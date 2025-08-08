using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FrameByFrame.src.Engine.Animation
{
    public class Frame : IDisposable
    {
        public Matrix transform;
        private Rectangle drawRectangle;

        // Compressed layer storage - only store non-transparent pixels
        private Dictionary<int, Color> _layer1Pixels = new Dictionary<int, Color>();
        private Dictionary<int, Color> _layer2Pixels = new Dictionary<int, Color>();
        private Dictionary<int, Color> _layer3Pixels = new Dictionary<int, Color>();
        
        // Lazy texture creation - only create when needed for drawing
        private Texture2D _layer1Texture;
        private Texture2D _layer2Texture;
        private Texture2D _layer3Texture;
        private bool _texturesNeedUpdate = true;

        public BasicTexture CombinedTexture;
        
        // Shared static background texture - all frames use the same one
        private static Texture2D _sharedBackgroundTexture;
        private bool _disposed = false;

        public static Vector2 position;
        public static int staticWidth { get; set; }
        public static int staticHeight { get; set; }

        public int width { get; set; }
        public int height { get; set; }

        public Frame(Vector2 givenPosition, Vector2 dimensions)
        {
            staticWidth = (int)dimensions.X;
            staticHeight = (int)dimensions.Y;

            width = (int)dimensions.X;
            height = (int)dimensions.Y;

            position = givenPosition;
            transform = Matrix.Identity;

            drawRectangle = new Rectangle((int)position.X, (int)position.Y, width, height);

            // Create shared background texture only once
            if (_sharedBackgroundTexture == null)
            {
                _sharedBackgroundTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, width, height, pixel => Color.White, Shapes.RECTANGLE);
            }
        }

        // Set a pixel in a layer - use compressed storage
        public void SetPixel(string layerName, int x, int y, Color color)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            
            int idx = y * width + x;
            var targetLayer = GetLayerDict(layerName);
            
            if (targetLayer != null)
            {
                if (color == Color.Transparent)
                {
                    targetLayer.Remove(idx); // Remove transparent pixels to save memory
                }
                else
                {
                    targetLayer[idx] = color;
                }
                _texturesNeedUpdate = true;
            }
        }

        // Get the pixel array for a layer - convert from compressed storage
        public Color[] GetLayerPixels(string layerName)
        {
            var layerDict = GetLayerDict(layerName);
            if (layerDict == null) return null;

            Color[] pixels = new Color[width * height];
            // Initialize all to transparent
            Array.Fill(pixels, Color.Transparent);
            
            // Fill in non-transparent pixels
            foreach (var kvp in layerDict)
            {
                pixels[kvp.Key] = kvp.Value;
            }
            
            return pixels;
        }

        private Dictionary<int, Color> GetLayerDict(string layerName)
        {
            return layerName switch
            {
                "_layer1" => _layer1Pixels,
                "_layer2" => _layer2Pixels,
                "_layer3" => _layer3Pixels,
                _ => null
            };
        }

        // Clear a layer
        public void ClearLayer(string layerName)
        {
            var targetLayer = GetLayerDict(layerName);
            if (targetLayer != null)
            {
                targetLayer.Clear();
                _texturesNeedUpdate = true;
            }
        }

        // Lazy texture creation and update
        public void UpdateTextures()
        {
            if (!_texturesNeedUpdate) return;

            // Only create textures when we need them
            EnsureTexturesCreated();

            // Update each layer texture from compressed data
            UpdateLayerTexture(_layer1Texture, _layer1Pixels);
            UpdateLayerTexture(_layer2Texture, _layer2Pixels);
            UpdateLayerTexture(_layer3Texture, _layer3Pixels);

            _texturesNeedUpdate = false;
        }

        private void EnsureTexturesCreated()
        {
            if (_layer1Texture == null)
                _layer1Texture = new Texture2D(GlobalParameters.GlobalGraphics, width, height);
            if (_layer2Texture == null)
                _layer2Texture = new Texture2D(GlobalParameters.GlobalGraphics, width, height);
            if (_layer3Texture == null)
                _layer3Texture = new Texture2D(GlobalParameters.GlobalGraphics, width, height);
        }

        private void UpdateLayerTexture(Texture2D texture, Dictionary<int, Color> layerData)
        {
            if (texture == null) return;

            // Create pixel array and fill from compressed data
            Color[] pixels = new Color[width * height];
            Array.Fill(pixels, Color.Transparent);
            
            foreach (var kvp in layerData)
            {
                pixels[kvp.Key] = kvp.Value;
            }
            
            texture.SetData(pixels);
        }

        public virtual void Draw(float opacity)
        {
            if (_sharedBackgroundTexture != null)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(_sharedBackgroundTexture,
                    drawRectangle,
                    null,
                    Color.White * opacity,
                    0.0f,
                    new Vector2(0, 0),
                    new SpriteEffects(),
                    0.2f);
            }
        }

        public virtual void DrawLayers(float opacity)
        {
            // Only update textures when we need to draw
            if (_texturesNeedUpdate)
                UpdateTextures();

            // Draw each layer texture (only if they have content)
            if (_layer3Pixels.Count > 0 && _layer3Texture != null)
                GlobalParameters.GlobalSpriteBatch.Draw(_layer3Texture, drawRectangle, Color.White * opacity);
            if (_layer2Pixels.Count > 0 && _layer2Texture != null)
                GlobalParameters.GlobalSpriteBatch.Draw(_layer2Texture, drawRectangle, Color.White * opacity);
            if (_layer1Pixels.Count > 0 && _layer1Texture != null)
                GlobalParameters.GlobalSpriteBatch.Draw(_layer1Texture, drawRectangle, Color.White * opacity);
        }

        public void DrawCombinedTexture(float opacity)
        {
            CombinedTexture?.Draw(Vector2.Zero, opacity);
        }

        // Get memory usage info for debugging
        public long GetMemoryUsage()
        {
            long memory = 0;
            memory += _layer1Pixels.Count * (sizeof(int) + 16); // Dictionary overhead + Color size
            memory += _layer2Pixels.Count * (sizeof(int) + 16);
            memory += _layer3Pixels.Count * (sizeof(int) + 16);
            
            // Add texture memory (if created)
            if (_layer1Texture != null) memory += width * height * 4; // 4 bytes per pixel
            if (_layer2Texture != null) memory += width * height * 4;
            if (_layer3Texture != null) memory += width * height * 4;
            
            return memory;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Dispose textures
                _layer1Texture?.Dispose();
                _layer2Texture?.Dispose();
                _layer3Texture?.Dispose();
                
                // Clear pixel data
                _layer1Pixels?.Clear();
                _layer2Pixels?.Clear();
                _layer3Pixels?.Clear();
                
                // Don't dispose shared background texture - it's shared!
                CombinedTexture = null;
            }

            _disposed = true;
        }

        ~Frame()
        {
            Dispose(false);
        }
    }
}
