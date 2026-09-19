using System;
using System.IO;

namespace FrameByFrame.src.Engine.Animation
{
    public sealed class AudioTrack
    {
        public const int SampleRate = 44_100;
        public const int ChannelCount = 2;
        public const int BytesPerSample = 2;
        public const int BytesPerSecond = SampleRate * ChannelCount * BytesPerSample;

        public Guid Id { get; }
        public string SourceName { get; }
        public string SourceExtension { get; }
        public byte[] EncodedData { get; }
        public int StartFrame { get; internal set; }
        public float Volume { get; internal set; }
        public bool IsMuted { get; internal set; }
        public double DurationSeconds { get; }
        internal byte[] PcmData { get; private set; }

        public AudioTrack(string sourceName, string sourceExtension, byte[] encodedData,
            int startFrame, double durationSeconds, byte[] pcmData = null,
            Guid? id = null, float volume = 1f, bool isMuted = false)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                throw new ArgumentException("An audio track must have a source name.", nameof(sourceName));
            if (encodedData == null || encodedData.Length == 0)
                throw new ArgumentException("An audio track must contain encoded audio data.", nameof(encodedData));
            if (!double.IsFinite(durationSeconds) || durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));

            Id = id.GetValueOrDefault(Guid.NewGuid());
            if (Id == Guid.Empty) throw new ArgumentException("Audio track IDs cannot be empty.", nameof(id));
            SourceName = Path.GetFileName(sourceName);
            if (string.IsNullOrWhiteSpace(SourceName))
                throw new ArgumentException("The audio source name is invalid.", nameof(sourceName));
            SourceExtension = NormalizeExtension(sourceExtension);
            EncodedData = encodedData;
            StartFrame = Math.Max(0, startFrame);
            Volume = Math.Clamp(volume, 0f, 1f);
            IsMuted = isMuted;
            DurationSeconds = durationSeconds;
            SetDecodedPcm(pcmData);
        }

        internal void SetDecodedPcm(byte[] pcmData)
        {
            if (pcmData != null && pcmData.Length % (ChannelCount * BytesPerSample) != 0)
                throw new InvalidDataException("Decoded audio data is not sample-aligned.");
            PcmData = pcmData;
        }

        internal void ReleaseDecodedPcm() => PcmData = null;

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return ".audio";
            string normalized = extension.StartsWith('.') ? extension : "." + extension;
            if (normalized.Length > 16 || normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("The audio extension is invalid.", nameof(extension));
            return normalized.ToLowerInvariant();
        }
    }
}
