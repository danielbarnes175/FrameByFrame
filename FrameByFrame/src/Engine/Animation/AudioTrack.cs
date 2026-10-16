using System;
using System.IO;
using System.Text;

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
        private int _startFrame;
        private float _volume = 1f;
        public int StartFrame
        {
            get => _startFrame;
            internal set => _startFrame = value >= 0
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), "Audio start frames cannot be negative.");
        }
        public float Volume
        {
            get => _volume;
            internal set => _volume = float.IsFinite(value) && value is >= 0f and <= 1f
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), "Audio volume must be between zero and one.");
        }
        public bool IsMuted { get; internal set; }
        public double DurationSeconds { get; }
        internal byte[]? PcmData { get; private set; }

        public AudioTrack(string sourceName, string sourceExtension, byte[] encodedData,
            int startFrame, double durationSeconds, byte[]? pcmData = null,
            Guid? id = null, float volume = 1f, bool isMuted = false)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                throw new ArgumentException("An audio track must have a source name.", nameof(sourceName));
            if (encodedData == null || encodedData.Length == 0 || encodedData.Length > ProjectLimits.MaxAudioTrackBytes)
                throw new ArgumentException($"An audio track must contain between 1 byte and {ProjectLimits.MaxAudioTrackBytes:N0} bytes.", nameof(encodedData));
            if (!double.IsFinite(durationSeconds) || durationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));

            Id = id.GetValueOrDefault(Guid.NewGuid());
            if (Id == Guid.Empty) throw new ArgumentException("Audio track IDs cannot be empty.", nameof(id));
            SourceName = Path.GetFileName(sourceName);
            if (string.IsNullOrWhiteSpace(SourceName) || Encoding.UTF8.GetByteCount(SourceName) > ProjectLimits.MaxNameBytes)
                throw new ArgumentException("The audio source name is invalid.", nameof(sourceName));
            SourceExtension = NormalizeExtension(sourceExtension);
            EncodedData = encodedData;
            StartFrame = startFrame;
            Volume = volume;
            IsMuted = isMuted;
            DurationSeconds = durationSeconds;
            SetDecodedPcm(pcmData);
        }

        internal void SetDecodedPcm(byte[]? pcmData)
        {
            if (pcmData != null && (pcmData.Length > ProjectLimits.MaxDecodedAudioTrackBytes ||
                pcmData.Length % (ChannelCount * BytesPerSample) != 0))
                throw new InvalidDataException("Decoded audio data is too large or not sample-aligned.");
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
