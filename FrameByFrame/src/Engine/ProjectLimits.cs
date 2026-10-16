using System;

namespace FrameByFrame.src.Engine
{
    /// <summary>Single source of truth for editable-project and FBF resource limits.</summary>
    internal static class ProjectLimits
    {
        public const int MinCanvasDimension = 64;
        public const int MaxCanvasDimension = 4_096;
        public const int MaxFramesPerSecond = 240;
        public const int MaxFrameCount = 100_000;
        public const int MaxLayerCount = 256;
        public const long MaxFrameLayerInstances = 250_000;
        public const long MaxEstimatedModelBytes = 256L * 1024 * 1024;
        public const long MaxStoredPixels = 32L * 1024 * 1024;
        public const int MaxStringBytes = 1024 * 1024;
        public const int MaxNameBytes = 4 * 1024;
        public const int MaxFramePayloadBytes = 512 * 1024 * 1024;

        public const int MaxAudioTrackCount = 64;
        public const int MaxAudioTrackBytes = 100 * 1024 * 1024;
        public const long MaxProjectAudioBytes = 256L * 1024 * 1024;
        public const int MaxDecodedAudioTrackBytes = 128 * 1024 * 1024;
        public const long MaxDecodedProjectAudioBytes = 256L * 1024 * 1024;

        private const long EstimatedFrameBytes = 256;
        private const long EstimatedFrameLayerBytes = 192;

        public static long EstimateModelBytes(int layerCount, int frameCount)
        {
            return checked(frameCount * EstimatedFrameBytes +
                checked((long)layerCount * frameCount) * EstimatedFrameLayerBytes);
        }

        public static void ValidateProjectShape(int width, int height, int layerCount, int frameCount)
        {
            if (width < MinCanvasDimension || height < MinCanvasDimension ||
                width > MaxCanvasDimension || height > MaxCanvasDimension)
                throw new ArgumentOutOfRangeException(nameof(width),
                    $"Canvas dimensions must be between {MinCanvasDimension} and {MaxCanvasDimension}.");
            _ = checked(width * height);
            ValidateResourceCounts(layerCount, frameCount);
        }

        public static void ValidateResourceCounts(int layerCount, int frameCount)
        {
            if (layerCount <= 0 || layerCount > MaxLayerCount)
                throw new ArgumentOutOfRangeException(nameof(layerCount),
                    $"Layer count must be between 1 and {MaxLayerCount}.");
            if (frameCount <= 0 || frameCount > MaxFrameCount)
                throw new ArgumentOutOfRangeException(nameof(frameCount),
                    $"Frame count must be between 1 and {MaxFrameCount:N0}.");

            long frameLayers = checked((long)layerCount * frameCount);
            if (frameLayers > MaxFrameLayerInstances || EstimateModelBytes(layerCount, frameCount) > MaxEstimatedModelBytes)
                throw new ArgumentOutOfRangeException(nameof(frameCount),
                    "The combined frame and layer count exceeds the project memory budget.");
        }

        public static int MaximumFramePayloadBytes(int pixelCount, int layerCount)
        {
            if (pixelCount <= 0) throw new ArgumentOutOfRangeException(nameof(pixelCount));
            if (layerCount <= 0 || layerCount > MaxLayerCount) throw new ArgumentOutOfRangeException(nameof(layerCount));
            long maximumChanges = Math.Min(MaxStoredPixels, checked((long)pixelCount * layerCount));
            long maximumPayload = checked(layerCount * sizeof(int) + maximumChanges * 9L);
            return (int)Math.Min(MaxFramePayloadBytes, maximumPayload);
        }
    }
}
