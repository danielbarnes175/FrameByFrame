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
        private bool _previewNeedsUpdate = true;
        private bool _disposed;
        private Texture2D? _previewTexture;
        private int _previewVisibilityHash;

        public IReadOnlyList<FrameLayer> Layers => _layers;
        public long NonTransparentPixelCount => _layers.Sum(layer => (long)layer.Pixels.Count);
        private readonly List<FrameLayer> _layers;

        private static Texture2D? _sharedBackgroundTexture;

        public int width { get; set; }
        public int height { get; set; }

        public Frame(Vector2 givenPosition, Vector2 dimensions, IEnumerable<AnimationLayer>? layers = null)
        {
            width = (int)dimensions.X;
            height = (int)dimensions.Y;
            transform = Matrix.Identity;
            drawRectangle = new Rectangle((int)givenPosition.X, (int)givenPosition.Y, width, height);
            _layers = (layers ?? CreateDefaultLayers()).Select(layer => new FrameLayer(layer)).ToList();

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
            _previewNeedsUpdate = true;
        }

        public void RemoveLayer(Guid layerId)
        {
            FrameLayer? layer = FindLayer(layerId);
            if (layer == null) return;
            layer.Dispose();
            _layers.Remove(layer);
            _texturesNeedUpdate = true;
            _previewNeedsUpdate = true;
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
            FrameLayer? layer = FindLayer(layerId);
            if (layer == null || layer.Definition.IsLocked || x < 0 || x >= width || y < 0 || y >= height) return false;
            int index = y * width + x;
            bool wasOpaque = layer.Pixels.ContainsKey(index);
            if (!wasOpaque && color != Color.Transparent && !allowNewOpaquePixel) return false;
            if (color == Color.Transparent) layer.Pixels.Remove(index);
            else layer.Pixels[index] = color;
            _texturesNeedUpdate = true;
            _previewNeedsUpdate = true;
            return !wasOpaque && color != Color.Transparent;
        }

        public int GetLayerPixelCount(Guid layerId) => FindLayer(layerId)?.Pixels.Count ?? 0;

        public Color GetLayerPixel(Guid layerId, int x, int y)
        {
            FrameLayer? layer = FindLayer(layerId);
            if (layer == null || x < 0 || x >= width || y < 0 || y >= height) return Color.Transparent;
            return layer.Pixels.TryGetValue(y * width + x, out Color color) ? color : Color.Transparent;
        }

        internal Color GetLayerPixel(Guid layerId, int index)
        {
            FrameLayer layer = FindLayer(layerId) ?? throw new ArgumentException("Unknown layer.", nameof(layerId));
            return layer.Pixels.TryGetValue(index, out Color color) ? color : Color.Transparent;
        }

        internal void ApplyPixelChanges(Guid layerId, IEnumerable<KeyValuePair<int, Color>> changes)
        {
            FrameLayer layer = FindLayer(layerId) ?? throw new ArgumentException("Unknown layer.", nameof(layerId));
            foreach (KeyValuePair<int, Color> change in changes)
            {
                if (change.Key < 0 || change.Key >= width * height)
                    throw new ArgumentOutOfRangeException(nameof(changes), "A pixel index is outside the frame.");
                if (change.Value == Color.Transparent) layer.Pixels.Remove(change.Key);
                else layer.Pixels[change.Key] = change.Value;
            }
            _texturesNeedUpdate = true;
            _previewNeedsUpdate = true;
        }

        public Color[] GetLayerPixels(Guid layerId)
        {
            FrameLayer layer = FindLayer(layerId) ?? throw new ArgumentException("Unknown layer.", nameof(layerId));
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
            _previewNeedsUpdate = true;
        }

        internal void SetSparseLayerPixels(Guid layerId, IEnumerable<KeyValuePair<int, uint>> pixels)
        {
            FrameLayer layer = FindLayer(layerId) ?? throw new ArgumentException("Unknown layer.", nameof(layerId));
            layer.Pixels.Clear();
            foreach (KeyValuePair<int, uint> pixel in pixels)
                if (pixel.Value != 0) layer.Pixels[pixel.Key] = new Color { PackedValue = pixel.Value };
            _texturesNeedUpdate = true;
            _previewNeedsUpdate = true;
        }

        public Color GetVisiblePixel(int x, int y, Color background = default)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return Color.Transparent;
            int index = y * width + x;
            return CompositeVisiblePixel(_layers, index, background);
        }

        internal static Color CompositeVisiblePixel(IReadOnlyList<FrameLayer> layers, int pixelIndex,
            Color background)
        {
            ArgumentNullException.ThrowIfNull(layers);
            Color result = background;
            for (int layerIndex = layers.Count - 1; layerIndex >= 0; layerIndex--)
            {
                FrameLayer layer = layers[layerIndex];
                if (layer.Definition.IsVisible && layer.Pixels.TryGetValue(pixelIndex, out Color color))
                    result = ColorCompositor.SourceOver(color, result);
            }
            return result;
        }

        public bool FloodFill(Guid layerId, int startX, int startY, Color replacement,
            long availablePixels = long.MaxValue, long maxChangedPixels = long.MaxValue,
            Action<int, Color>? beforeChange = null)
        {
            FrameLayer? layer = FindLayer(layerId);
            if (layer == null || layer.Definition.IsLocked || startX < 0 || startX >= width || startY < 0 || startY >= height) return false;
            int startIndex = startY * width + startX;
            Color target = layer.Pixels.TryGetValue(startIndex, out Color startColor)
                ? startColor : Color.Transparent;
            if (target == replacement) return false;

            var changed = new List<int>();
            var visited = new HashSet<int> { startIndex };
            var pending = new Queue<int>();
            pending.Enqueue(startIndex);
            while (pending.Count > 0)
            {
                int index = pending.Dequeue();
                changed.Add(index);
                if (changed.Count > maxChangedPixels) return false;
                int x = index % width;
                int y = index / width;
                TryQueue(x - 1, y); TryQueue(x + 1, y); TryQueue(x, y - 1); TryQueue(x, y + 1);
            }

            long addedPixels = target == Color.Transparent && replacement != Color.Transparent ? changed.Count : 0;
            if (addedPixels > availablePixels) return false;
            foreach (int index in changed)
            {
                beforeChange?.Invoke(index, target);
                if (replacement == Color.Transparent) layer.Pixels.Remove(index);
                else layer.Pixels[index] = replacement;
            }
            _texturesNeedUpdate = true;
            _previewNeedsUpdate = true;
            return true;

            void TryQueue(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) return;
                int index = y * width + x;
                if (!visited.Add(index)) return;
                Color color = layer.Pixels.TryGetValue(index, out Color value) ? value : Color.Transparent;
                if (color == target) pending.Enqueue(index);
            }
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
            FrameLayer? layer = FindLayer(layerId);
            if (layer == null || layer.Definition.IsLocked) return;
            layer.Pixels.Clear();
            _texturesNeedUpdate = true;
            _previewNeedsUpdate = true;
        }

        private FrameLayer? FindLayer(Guid layerId) => _layers.FirstOrDefault(layer => layer.Id == layerId);

        public void UpdateTextures()
        {
            if (!_texturesNeedUpdate) return;
            foreach (FrameLayer layer in _layers)
            {
                layer.Texture ??= new Texture2D(ApplicationRuntime.Current.Graphics, width, height);
                Color[] pixels = GetLayerPixels(layer.Id);
                for (int i = 0; i < pixels.Length; i++) pixels[i] = ColorCompositor.Premultiply(pixels[i]);
                layer.Texture.SetData(pixels);
            }
            _texturesNeedUpdate = false;
        }

        public virtual void Draw(float opacity)
        {
            EnsureBackgroundTexture();
            if (_sharedBackgroundTexture != null)
                ApplicationRuntime.Current.SpriteBatch.Draw(_sharedBackgroundTexture, drawRectangle, null, Color.White * opacity,
                    0f, Vector2.Zero, SpriteEffects.None, .2f);
        }

        public void Draw(Rectangle destination, float opacity)
        {
            EnsureBackgroundTexture();
            if (_sharedBackgroundTexture != null)
                ApplicationRuntime.Current.SpriteBatch.Draw(_sharedBackgroundTexture, destination, Color.White * opacity);
        }

        public void DrawLayers(Rectangle destination, float opacity) => DrawLayersCore(destination, opacity);

        private static void EnsureBackgroundTexture()
        {
            _sharedBackgroundTexture ??= DrawingService.CreateTexture(ApplicationRuntime.Current.Graphics, 1, 1,
                _ => Color.White, Shapes.RECTANGLE);
        }
        public void DrawLayers(CanvasView view, float opacity)
        {
            ArgumentNullException.ThrowIfNull(view);
            if (_texturesNeedUpdate) UpdateTextures();
            Vector2 origin = new(width / 2f, height / 2f);
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                FrameLayer layer = _layers[i];
                if (layer.Definition.IsVisible && layer.Pixels.Count > 0 && layer.Texture != null)
                    ApplicationRuntime.Current.SpriteBatch.Draw(layer.Texture, view.Center, null, Color.White * opacity,
                        view.RotationRadians, origin, view.Scale, SpriteEffects.None, 0f);
            }
        }
        public virtual void DrawLayers(float opacity) => DrawLayersCore(drawRectangle, opacity);

        private void DrawLayersCore(Rectangle destination, float opacity)
        {
            if (_texturesNeedUpdate) UpdateTextures();
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                FrameLayer layer = _layers[i];
                if (layer.Definition.IsVisible && layer.Pixels.Count > 0 && layer.Texture != null)
                    ApplicationRuntime.Current.SpriteBatch.Draw(layer.Texture, destination, Color.White * opacity);
            }
        }

        public void DrawPreview(Rectangle destination, float opacity, bool transparentBackground = false,
            Color backgroundColor = default)
        {
            int previewWidth = Math.Max(1, destination.Width);
            int previewHeight = Math.Max(1, destination.Height);
            int visibilityHash = 17;
            foreach (FrameLayer layer in _layers)
                visibilityHash = unchecked(visibilityHash * 31 + (layer.Definition.IsVisible ? layer.Id.GetHashCode() : 0));
            visibilityHash = unchecked(visibilityHash * 31 + transparentBackground.GetHashCode());
            visibilityHash = unchecked(visibilityHash * 31 + backgroundColor.GetHashCode());

            if (_previewTexture == null || _previewTexture.Width != previewWidth || _previewTexture.Height != previewHeight)
            {
                _previewTexture?.Dispose();
                _previewTexture = new Texture2D(ApplicationRuntime.Current.Graphics, previewWidth, previewHeight);
                _previewNeedsUpdate = true;
            }

            if (_previewNeedsUpdate || visibilityHash != _previewVisibilityHash)
            {
                Color[] previewPixels = new Color[previewWidth * previewHeight];
                for (int y = 0; y < previewHeight; y++)
                {
                    int sourceY = Math.Min(height - 1, y * height / previewHeight);
                    for (int x = 0; x < previewWidth; x++)
                    {
                        int sourceX = Math.Min(width - 1, x * width / previewWidth);
                        Color background = transparentBackground
                            ? DrawingService.CheckerboardColor(x, y)
                            : backgroundColor;
                        previewPixels[x + y * previewWidth] = GetVisiblePixel(sourceX, sourceY, background);
                    }
                }
                for (int i = 0; i < previewPixels.Length; i++)
                    previewPixels[i] = ColorCompositor.Premultiply(previewPixels[i]);
                _previewTexture.SetData(previewPixels);
                _previewVisibilityHash = visibilityHash;
                _previewNeedsUpdate = false;
            }

            ApplicationRuntime.Current.SpriteBatch.Draw(_previewTexture, destination, Color.White * opacity);
        }

        public long GetMemoryUsage() => _layers.Sum(layer =>
            (long)layer.Pixels.Count * (sizeof(int) + 16) + (layer.Texture == null ? 0L : (long)width * height * 4));

        public void ReleasePreviewTexture()
        {
            _previewTexture?.Dispose();
            _previewTexture = null;
            _previewNeedsUpdate = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            foreach (FrameLayer layer in _layers) layer.Dispose();
            ReleasePreviewTexture();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
