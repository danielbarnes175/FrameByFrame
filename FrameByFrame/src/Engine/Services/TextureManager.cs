using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrameByFrame.src.Engine;

namespace FrameByFrame.src.Engine.Services
{
    public static class TextureManager
    {
        private static Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
        private static Dictionary<string, Texture2D> _colorTextureCache = new Dictionary<string, Texture2D>();
        
        public static Texture2D GetOrCreateColorTexture(GraphicsDevice device, Color color, int size = 32, Shapes shape = Shapes.RECTANGLE)
        {
            string key = $"{color.PackedValue}_{size}_{shape}";
            
            if (_colorTextureCache.ContainsKey(key))
                return _colorTextureCache[key];
                
            var texture = DrawingService.CreateTexture(device, size, size, pixel => color, shape);
            _colorTextureCache[key] = texture;
            return texture;
        }
        
        public static Texture2D GetOrCreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint, Shapes shape)
        {
            // For solid colors, use the color texture cache
            Color firstPixel = paint(0);
            bool isSolidColor = true;
            
            // Quick check if it's a solid color
            for (int i = 1; i < Math.Min(100, width * height); i++)
            {
                if (paint(i) != firstPixel)
                {
                    isSolidColor = false;
                    break;
                }
            }
            
            if (isSolidColor)
            {
                return GetOrCreateColorTexture(device, firstPixel, Math.Max(width, height), shape);
            }
            
            // For complex textures, create normally (could add more caching here if needed)
            return DrawingService.CreateTexture(device, width, height, paint, shape);
        }
        
        public static void ClearCache()
        {
            foreach (var texture in _textureCache.Values)
                texture?.Dispose();
            foreach (var texture in _colorTextureCache.Values)
                texture?.Dispose();
                
            _textureCache.Clear();
            _colorTextureCache.Clear();
        }
    }
}