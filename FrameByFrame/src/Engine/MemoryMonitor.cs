using System;
using Microsoft.Xna.Framework;
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
            if (!DebugManager.ShowMemoryMonitor) return string.Empty;
            if (animation == null) return "No animation loaded";
            
            long totalMemory = animation.GetTotalMemoryUsage();
            long systemMemory = GC.GetTotalMemory(false);
            
            return $"Frames: {animation.TotalFrames} | " +
                   $"Animation Memory: {FormatBytes(totalMemory)} | " +
                   $"System Memory: {FormatBytes(systemMemory)}";
        }
        
        public static void DrawMemoryOverlay(Vector2 position, Color color, Animation.Animation animation)
        {
            if (!DebugManager.ShowMemoryMonitor) return;
            
            string memoryInfo = GetAnimationMemoryInfo(animation);
            if (!string.IsNullOrEmpty(memoryInfo))
            {
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, memoryInfo, position, color);
            }
        }
    }
}