using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;

namespace FrameByFrame.src.Engine.Audio
{
    public static class AudioService
    {
        public const int MaxTrackBytes = 100 * 1024 * 1024;
        public const long MaxProjectAudioBytes = 256L * 1024 * 1024;
        public const int MaxAudioTrackCount = 64;
        public const int MaxDecodedTrackBytes = 128 * 1024 * 1024;
        public const long MaxDecodedProjectBytes = 256L * 1024 * 1024;
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".wav", ".mp3", ".m4a", ".aac", ".ogg", ".opus", ".flac"
        };

        public static bool IsSupportedExtension(string extension) =>
            !string.IsNullOrWhiteSpace(extension) && SupportedExtensions.Contains(
                extension.StartsWith('.') ? extension : "." + extension);

        public static Task<AudioTrack> ImportAsync(string filename, int startFrame) => Task.Run(() =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filename);
            string extension = Path.GetExtension(filename);
            if (!IsSupportedExtension(extension))
                throw new InvalidDataException("Supported audio types are WAV, MP3, M4A/AAC, OGG/Opus, and FLAC.");
            byte[] encoded = ReadBoundedFile(filename);
            byte[] pcm = DecodeFile(filename);
            double duration = pcm.Length / (double)AudioTrack.BytesPerSecond;
            return new AudioTrack(Path.GetFileName(filename), extension, encoded, startFrame, duration, pcm);
        });

        public static Task DecodeAsync(AudioTrack track) => Task.Run(() =>
        {
            ArgumentNullException.ThrowIfNull(track);
            if (track.PcmData != null) return;
            string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"framebyframe-audio-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectory);
            try
            {
                string input = Path.Combine(temporaryDirectory, "source" + track.SourceExtension);
                File.WriteAllBytes(input, track.EncodedData);
                track.SetDecodedPcm(DecodeFile(input));
            }
            finally
            {
                try { Directory.Delete(temporaryDirectory, recursive: true); }
                catch { }
            }
        });

        private static byte[] ReadBoundedFile(string filename)
        {
            var info = new FileInfo(filename);
            if (!info.Exists) throw new FileNotFoundException("The selected audio file no longer exists.", filename);
            if (info.Length <= 0 || info.Length > MaxTrackBytes)
                throw new InvalidDataException($"Audio files must be between 1 byte and {MaxTrackBytes / (1024 * 1024)} MB.");
            byte[] data = File.ReadAllBytes(filename);
            if (data.Length == 0 || data.Length > MaxTrackBytes)
                throw new InvalidDataException($"Audio files must be between 1 byte and {MaxTrackBytes / (1024 * 1024)} MB.");
            return data;
        }

        private static byte[] DecodeFile(string input)
        {
            string output = Path.Combine(Path.GetTempPath(), $"framebyframe-{Guid.NewGuid():N}.pcm");
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = SaveService.ResolveFfmpegPath(),
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                foreach (string argument in new[]
                {
                    "-v", "error", "-y", "-i", input, "-vn", "-f", "s16le", "-acodec", "pcm_s16le",
                    "-ar", AudioTrack.SampleRate.ToString(CultureInfo.InvariantCulture), "-ac",
                    AudioTrack.ChannelCount.ToString(CultureInfo.InvariantCulture), output
                }) startInfo.ArgumentList.Add(argument);

                using Process process = Process.Start(startInfo) ??
                    throw new InvalidOperationException("FFmpeg could not be started.");
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0 || !File.Exists(output))
                    throw new InvalidDataException($"The selected file could not be decoded: {LastLine(error)}");
                var outputInfo = new FileInfo(output);
                if (outputInfo.Length > MaxDecodedTrackBytes)
                    throw new InvalidDataException("The decoded audio exceeds the supported per-track limit.");
                byte[] pcm = File.ReadAllBytes(output);
                if (pcm.Length == 0) throw new InvalidDataException("The selected file contains no decodable audio.");
                return pcm;
            }
            finally
            {
                try { if (File.Exists(output)) File.Delete(output); }
                catch { }
            }
        }

        private static string LastLine(string text)
        {
            string[] lines = (text ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return lines.Length == 0 ? "No diagnostic was provided." : lines[^1];
        }
    }
}
