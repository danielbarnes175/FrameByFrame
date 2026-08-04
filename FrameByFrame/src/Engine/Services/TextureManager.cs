using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrameByFrame.src.Engine;

namespace FrameByFrame.src.Engine.Services
{
    public static class TextureManager
    {
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
        
        public static void ClearCache()
        {
            foreach (var texture in _colorTextureCache.Values)
                texture?.Dispose();
                
            _colorTextureCache.Clear();
        }
    }
}
