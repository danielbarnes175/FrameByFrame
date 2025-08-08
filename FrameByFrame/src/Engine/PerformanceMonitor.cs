using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine
{
    public static class PerformanceMonitor
    {
        private static readonly Stopwatch _stopwatch = new Stopwatch();
        private static int _frameCount = 0;
        private static double _totalFrameTime = 0;
        private static double _maxFrameTime = 0;
        private static double _minFrameTime = double.MaxValue;
        
        public static void StartFrame()
        {
            _stopwatch.Restart();
        }
        
        public static void EndFrame()
        {
            _stopwatch.Stop();
            double frameTime = _stopwatch.Elapsed.TotalMilliseconds;
            
            _frameCount++;
            _totalFrameTime += frameTime;
            _maxFrameTime = Math.Max(_maxFrameTime, frameTime);
            _minFrameTime = Math.Min(_minFrameTime, frameTime);
            
            // Reset stats every 60 frames to get recent performance
            if (_frameCount >= 60)
            {
                _frameCount = 0;
                _totalFrameTime = 0;
                _maxFrameTime = 0;
                _minFrameTime = double.MaxValue;
            }
        }
        
        public static string GetPerformanceInfo()
        {
            if (_frameCount == 0) return "Performance: Measuring...";
            
            double avgFrameTime = _totalFrameTime / _frameCount;
            double avgFps = 1000.0 / avgFrameTime;
            
            return $"FPS: {avgFps:F1} | Avg: {avgFrameTime:F2}ms | Max: {_maxFrameTime:F2}ms | Min: {_minFrameTime:F2}ms";
        }
        
        public static void DrawPerformanceOverlay(Vector2 position, Color color)
        {
            string perfInfo = GetPerformanceInfo();
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, perfInfo, position, color);
        }
    }
}