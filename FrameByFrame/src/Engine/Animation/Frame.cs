using FrameByFrame.src.Engine.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FrameByFrame.src.Engine.Animation
{
    public class Frame : IDisposable
    {
        public Matrix transform;
        private readonly Rectangle drawRectangle;
        private bool _texturesNeedUpdate = true;
        private bool _disposed;

        public IReadOnlyList<FrameLayer> Layers => _layers;
        public long NonTransparentPixelCount => _layers.Sum(layer => (long)layer.Pixels.Count);
        private readonly List<FrameLayer> _layers;

        public BasicTexture CombinedTexture;
        private static Texture2D _sharedBackgroundTexture;

        public int width { get; set; }
        public int height { get; set; }

        public Frame(Vector2 givenPosition, Vector2 dimensions, IEnumerable<AnimationLayer> layers = null)
        {
            width = (int)dimensions.X;
            height = (int)dimensions.Y;
            transform = Matrix.Identity;
            drawRectangle = new Rectangle((int)givenPosition.X, (int)givenPosition.Y, width, height);
            _layers = (layers ?? CreateDefaultLayers()).Select(layer => new FrameLayer(layer)).ToList();

            if (_sharedBackgroundTexture == null)
                _sharedBackgroundTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, width, height,
                    _ => Color.White, Shapes.RECTANGLE);
        }

        private static IEnumerable<AnimationLayer> CreateDefaultLayers() =>
            new[] { new AnimationLayer("Layer 1"), new AnimationLayer("Layer 2"), new AnimationLayer("Layer 3") };

        public void AddLayer(AnimationLayer definition, int index = -1)
        {
            if (_layers.Any(layer => layer.Id == definition.Id)) return;
            var layer = new FrameLayer(definition);
            if (index < 0 || index >= _layers.Count) _layers.Add(layer);
            else _layers.Insert(index, layer);
            _texturesNeedUpdate = true;
        }

        public void RemoveLayer(Guid layerId)
        {
            FrameLayer layer = FindLayer(layerId);
            if (layer == null) return;
            layer.Dispose();
            _layers.Remove(layer);
            _texturesNeedUpdate = true;
        }

        public void ReorderLayers(IReadOnlyList<AnimationLayer> order)
        {
            var byId = _layers.ToDictionary(layer => layer.Id);
            if (order.Count != _layers.Count || order.Any(layer => !byId.ContainsKey(layer.Id)))
                throw new ArgumentException("Layer order must contain every frame layer exactly once.", nameof(order));
            _layers.Clear();
            _layers.AddRange(order.Select(layer => byId[layer.Id]));
        }

        public bool SetPixel(Guid layerId, int x, int y, Color color, bool allowNewOpaquePixel = true)
        {
            FrameLayer layer = FindLayer(layerId);
            if (layer == null || layer.Definition.IsLocked || x < 0 || x >= width || y < 0 || y >= height) return false;
            int index = y * width + x;
            bool wasOpaque = layer.Pixels.ContainsKey(index);
            if (!wasOpaque && color != Color.Transparent && !allowNewOpaquePixel) return false;
            if (color == Color.Transparent) layer.Pixels.Remove(index);
            else layer.Pixels[index] = color;
            _texturesNeedUpdate = true;
            return !wasOpaque && color != Color.Transparent;
        }

        public int GetLayerPixelCount(Guid layerId) => FindLayer(layerId)?.Pixels.Count ?? 0;

        public Color[] GetLayerPixels(Guid layerId)
        {
            FrameLayer layer = FindLayer(layerId);
            if (layer == null) return null;
            Color[] pixels = new Color[width * height];
            Array.Fill(pixels, Color.Transparent);
            foreach (var pixel in layer.Pixels) pixels[pixel.Key] = pixel.Value;
            return pixels;
        }

        public IEnumerable<KeyValuePair<int, Color>> GetSparseLayerPixels(Guid layerId)
        {
            FrameLayer layer = FindLayer(layerId) ?? throw new ArgumentException("Unknown layer.", nameof(layerId));
            return layer.Pixels;
        }

        public void SetLayerPixels(Guid layerId, Color[] pixels, bool ignoreLock = false)
        {
            ArgumentNullException.ThrowIfNull(pixels);
            if (pixels.Length != width * height)
                throw new ArgumentException("Layer pixel count does not match the frame dimensions.", nameof(pixels));
            FrameLayer layer = FindLayer(layerId) ?? throw new ArgumentException("Unknown layer.", nameof(layerId));
            if (layer.Definition.IsLocked && !ignoreLock) return;
            layer.Pixels.Clear();
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i] != Color.Transparent) layer.Pixels[i] = pixels[i];
            _texturesNeedUpdate = true;
        }

        internal void SetSparseLayerPixels(Guid layerId, IEnumerable<KeyValuePair<int, uint>> pixels)
        {
            FrameLayer layer = FindLayer(layerId) ?? throw new ArgumentException("Unknown layer.", nameof(layerId));
            layer.Pixels.Clear();
            foreach (KeyValuePair<int, uint> pixel in pixels)
                if (pixel.Value != 0) layer.Pixels[pixel.Key] = new Color { PackedValue = pixel.Value };
            _texturesNeedUpdate = true;
        }

        public Color GetVisiblePixel(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return Color.Transparent;
            int index = y * width + x;
            foreach (FrameLayer layer in _layers)
                if (layer.Definition.IsVisible && layer.Pixels.TryGetValue(index, out Color color)) return color;
            return Color.White;
        }

        public bool FloodFill(Guid layerId, int startX, int startY, Color replacement, long availablePixels = long.MaxValue)
        {
            FrameLayer layer = FindLayer(layerId);
            if (layer == null || layer.Definition.IsLocked || startX < 0 || startX >= width || startY < 0 || startY >= height) return false;
            Color[] pixels = GetLayerPixels(layerId);
            FloodFillPixels(pixels, width, height, startX, startY, replacement);
            int newPixelCount = pixels.Count(pixel => pixel != Color.Transparent);
            if (newPixelCount - layer.Pixels.Count > availablePixels) return false;
            SetLayerPixels(layerId, pixels);
            return true;
        }

        public static void FloodFillPixels(Color[] pixels, int width, int height, int startX, int startY, Color replacement)
        {
            ArgumentNullException.ThrowIfNull(pixels);
            if (pixels.Length != width * height)
                throw new ArgumentException("Pixel count does not match the supplied dimensions.", nameof(pixels));
            if (startX < 0 || startX >= width || startY < 0 || startY >= height) return;
            int startIndex = startY * width + startX;
            Color target = pixels[startIndex];
            if (target == replacement) return;
            Queue<int> pending = new();
            pending.Enqueue(startIndex);
            pixels[startIndex] = replacement;
            while (pending.Count > 0)
            {
                int index = pending.Dequeue();
                int x = index % width;
                int y = index / width;
                TryQueue(x - 1, y); TryQueue(x + 1, y); TryQueue(x, y - 1); TryQueue(x, y + 1);
            }
            void TryQueue(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) return;
                int index = y * width + x;
                if (pixels[index] != target) return;
                pixels[index] = replacement;
                pending.Enqueue(index);
            }
        }

        public void ClearLayer(Guid layerId)
        {
            FrameLayer layer = FindLayer(layerId);
            if (layer == null || layer.Definition.IsLocked) return;
            layer.Pixels.Clear();
            _texturesNeedUpdate = true;
        }

        private FrameLayer FindLayer(Guid layerId) => _layers.FirstOrDefault(layer => layer.Id == layerId);

        public void UpdateTextures()
        {
            if (!_texturesNeedUpdate) return;
            foreach (FrameLayer layer in _layers)
            {
                layer.Texture ??= new Texture2D(GlobalParameters.GlobalGraphics, width, height);
                Color[] pixels = GetLayerPixels(layer.Id);
                layer.Texture.SetData(pixels);
            }
            _texturesNeedUpdate = false;
        }

        public virtual void Draw(float opacity)
        {
            if (_sharedBackgroundTexture != null)
                GlobalParameters.GlobalSpriteBatch.Draw(_sharedBackgroundTexture, drawRectangle, null, Color.White * opacity,
                    0f, Vector2.Zero, SpriteEffects.None, .2f);
        }

        public void Draw(Rectangle destination, float opacity)
        {
            if (_sharedBackgroundTexture != null)
                GlobalParameters.GlobalSpriteBatch.Draw(_sharedBackgroundTexture, destination, Color.White * opacity);
        }

        public void DrawLayers(Rectangle destination, float opacity) => DrawLayersCore(destination, opacity);
        public virtual void DrawLayers(float opacity) => DrawLayersCore(drawRectangle, opacity);

        private void DrawLayersCore(Rectangle destination, float opacity)
        {
            if (_texturesNeedUpdate) UpdateTextures();
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                FrameLayer layer = _layers[i];
                if (layer.Definition.IsVisible && layer.Pixels.Count > 0 && layer.Texture != null)
                    GlobalParameters.GlobalSpriteBatch.Draw(layer.Texture, destination, Color.White * opacity);
            }
        }

        public void DrawCombinedTexture(float opacity) => CombinedTexture?.Draw(Vector2.Zero, opacity);

        public void DrawPreview(Rectangle destination, float opacity)
        {
            if (CombinedTexture != null)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(CombinedTexture.texture, destination, Color.White * opacity);
                return;
            }
            GlobalParameters.GlobalSpriteBatch.Draw(_sharedBackgroundTexture, destination, Color.White * opacity);
            DrawLayersCore(destination, opacity);
        }

        public long GetMemoryUsage() => _layers.Sum(layer =>
            (long)layer.Pixels.Count * (sizeof(int) + 16) + (layer.Texture == null ? 0L : (long)width * height * 4));

        public void Dispose()
        {
            if (_disposed) return;
            foreach (FrameLayer layer in _layers) layer.Dispose();
            CombinedTexture?.texture?.Dispose();
            CombinedTexture = null;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
