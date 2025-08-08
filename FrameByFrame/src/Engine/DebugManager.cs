using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using FrameByFrame.src.Engine.Services;

namespace FrameByFrame.src.Engine
{
    public static class DebugManager
    {
        private static bool _isDebugMode = false;
        private static bool _showPerformanceMonitor = false;
        private static bool _showMemoryMonitor = false;
        private static KeyboardState _previousKeyboardState;
        
        // Debug toggle keys
        private static Keys _debugModeToggleKey = Keys.F1;
        private static Keys _performanceToggleKey = Keys.F2;
        private static Keys _memoryToggleKey = Keys.F3;
        
        public static bool IsDebugMode => _isDebugMode;
        public static bool ShowPerformanceMonitor => _isDebugMode && _showPerformanceMonitor;
        public static bool ShowMemoryMonitor => _isDebugMode && _showMemoryMonitor;
        
        public static void Update()
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();
            
            // Toggle debug mode with F1
            if (currentKeyboardState.IsKeyDown(_debugModeToggleKey) && !_previousKeyboardState.IsKeyDown(_debugModeToggleKey))
            {
                _isDebugMode = !_isDebugMode;
                
                // If debug mode is turned off, also turn off all monitors
                if (!_isDebugMode)
                {
                    _showPerformanceMonitor = false;
                    _showMemoryMonitor = false;
                }
            }
            
            // Only allow monitor toggles when debug mode is on
            if (_isDebugMode)
            {
                // Toggle performance monitor with F2
                if (currentKeyboardState.IsKeyDown(_performanceToggleKey) && !_previousKeyboardState.IsKeyDown(_performanceToggleKey))
                {
                    _showPerformanceMonitor = !_showPerformanceMonitor;
                }
                
                // Toggle memory monitor with F3
                if (currentKeyboardState.IsKeyDown(_memoryToggleKey) && !_previousKeyboardState.IsKeyDown(_memoryToggleKey))
                {
                    _showMemoryMonitor = !_showMemoryMonitor;
                }
            }
            
            _previousKeyboardState = currentKeyboardState;
        }
        
        public static void DrawDebugHelp(Vector2 position)
        {
            if (!_isDebugMode) return;
            
            string debugInfo = $"Debug Mode: ON\n" +
                              $"F1: Toggle Debug Mode\n" +
                              $"F2: Performance Monitor ({(_showPerformanceMonitor ? "ON" : "OFF")})\n" +
                              $"F3: Memory Monitor ({(_showMemoryMonitor ? "ON" : "OFF")})";
            
            // Draw semi-transparent background
            var helpLines = debugInfo.Split('\n');
            var lineHeight = GlobalParameters.font.LineSpacing;
            var maxWidth = 0f;
            
            foreach (var line in helpLines)
            {
                var lineWidth = GlobalParameters.font.MeasureString(line).X;
                if (lineWidth > maxWidth) maxWidth = lineWidth;
            }
            
            var backgroundRect = new Rectangle(
                (int)position.X - 5,
                (int)position.Y - 5,
                (int)maxWidth + 10,
                helpLines.Length * lineHeight + 10
            );
            
            // Create a small texture for background (reuse existing texture creation)
            var backgroundTexture = Services.TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                Color.Black, 
                1, 
                Shapes.RECTANGLE
            );
            
            GlobalParameters.GlobalSpriteBatch.Draw(backgroundTexture, backgroundRect, Color.Black * 0.8f);
            
            // Draw each line with appropriate colors
            for (int i = 0; i < helpLines.Length; i++)
            {
                var linePos = position + new Vector2(0, i * lineHeight);
                Color textColor = i == 0 ? UIConstants.DEBUG_PERFORMANCE : UIConstants.DEBUG_TEXT;
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, helpLines[i], linePos, textColor);
            }
        }
        
        // Compile-time debug mode (can be overridden by runtime toggle)
        public static bool IsCompileTimeDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
        
        // Initialize debug mode based on compile-time flag
        public static void Initialize()
        {
            _isDebugMode = IsCompileTimeDebug();
            _showPerformanceMonitor = _isDebugMode;
            _showMemoryMonitor = _isDebugMode;
        }
    }
}