using System;
using FrameByFrame.src.Engine.Animation;

namespace FrameByFrame.src.Engine
{
    public static class MemoryMonitor
    {
        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        public static string GetAnimationMemoryInfo(Animation.Animation animation)
        {
            if (animation == null) return "No animation loaded";
            
            long totalMemory = animation.GetTotalMemoryUsage();
            long systemMemory = GC.GetTotalMemory(false);
            
            return $"Frames: {animation.TotalFrames} | " +
                   $"Animation Memory: {FormatBytes(totalMemory)} | " +
                   $"System Memory: {FormatBytes(systemMemory)}";
        }
    }
}