using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace FrameByFrame.src.Engine.Animation
{
    public class Animation : IDisposable
    {
        // Animation
        private double playbackTimer;
        public int fps;
        public bool IsPlaying { get; private set; }

        public Vector2 frameSize;
        public Vector2 framePosition;

        // Tools
        public bool isOnionSkinEnabled;
        public int brushSize;
        private int maxOnionFrames = 3;
        private float baseOpacity = 0.1f;
        private readonly List<AnimationLayer> _layers;
        public IReadOnlyList<AnimationLayer> Layers => _layers;
        public Guid SelectedLayerId { get; private set; }
        public AnimationLayer SelectedLayer => _layers.FirstOrDefault(layer => layer.Id == SelectedLayerId);

        // Project Settings
        public string projectName;

        public LinkedList<Frame> frames;
        private LinkedListNode<Frame> currentFrame;
        private bool _disposed = false;

        public int TotalFrames => frames.Count;
        public int CurrentFrameIndex { get; private set; }
        public Frame CurrentFrame => currentFrame?.Value;
        public Rectangle DisplayBounds { get; private set; }

        public Animation(string projectName, IEnumerable<AnimationLayer> layers = null)
        {
            this.projectName = projectName;
            fps = 12;
            frames = new LinkedList<Frame>();
            playbackTimer = 0;
            IsPlaying = false;
            _layers = layers?.ToList() ??
                [new AnimationLayer("Layer 1"), new AnimationLayer("Layer 2"), new AnimationLayer("Layer 3")];
            if (_layers.Count == 0) throw new ArgumentException("An animation must have at least one layer.", nameof(layers));
            if (_layers.Select(layer => layer.Id).Distinct().Count() != _layers.Count)
                throw new ArgumentException("Layer IDs must be unique.", nameof(layers));
            SelectedLayerId = _layers[0].Id;
            CurrentFrameIndex = 0;
            brushSize = 5;
            isOnionSkinEnabled = true;
        }

        public void InitializeFrames()
        {
            frameSize = new Vector2(1200, 800);
            framePosition = new Vector2(
                GlobalParameters.screenWidth / 2 - (int)frameSize.X / 2,
                GlobalParameters.screenHeight / 2 - (int)frameSize.Y / 2);

            frames.AddLast(new Frame(framePosition, frameSize, _layers));
            currentFrame = frames.First;
        }

        public void LoadFrames(IEnumerable<Frame> loadedFrames, Vector2 loadedFramePosition, Vector2 loadedFrameSize)
        {
            ArgumentNullException.ThrowIfNull(loadedFrames);

            foreach (Frame frame in frames)
            {
                frame?.Dispose();
            }

            frames.Clear();
            foreach (Frame frame in loadedFrames)
            {
                if (frame != null)
                    frames.AddLast(frame);
            }

            if (frames.Count == 0)
            {
                throw new ArgumentException("A saved animation must contain at least one frame.", nameof(loadedFrames));
            }

            framePosition = loadedFramePosition;
            frameSize = loadedFrameSize;
            currentFrame = frames.First;
            CurrentFrameIndex = 0;
            playbackTimer = 0;
            IsPlaying = false;
            InvalidateFrameCache();
        }

        // Cache for faster frame access
        private List<Frame> _framesList = new List<Frame>();
        private bool _framesCacheValid = true;

        public Frame GetFrameAtIndex(int index)
        {
            if (index < 0 || index >= frames.Count) return null;

            // Use cached list for O(1) access
            if (!_framesCacheValid || _framesList.Count != frames.Count)
            {
                _framesList.Clear();
                _framesList.AddRange(frames);
                _framesCacheValid = true;
            }

            return _framesList[index];
        }

        private void InvalidateFrameCache()
        {
            _framesCacheValid = false;
        }

        public void FirstFrame()
        {
            currentFrame = frames.First;
            CurrentFrameIndex = 0;
        }

        public void LastFrame()
        {
            currentFrame = frames.Last;
            CurrentFrameIndex = TotalFrames - 1;
        }

        public void NextFrame()
        {
            CurrentFrameIndex += 1;
            if (CurrentFrameIndex > TotalFrames - 1)
            {
                frames.AddLast(new Frame(framePosition, frameSize, _layers));
            }
            currentFrame = currentFrame.Next;
        }

        public void PreviousFrame()
        {
            if (CurrentFrameIndex <= 0) return;
            if (CurrentFrameIndex > 0)
            {
                CurrentFrameIndex -= 1;
            }
            currentFrame = currentFrame.Previous;
        }

        public void EraseCurrentLayer()
        {
            currentFrame.Value.ClearLayer(SelectedLayerId);
        }

        public void DeleteFrame()
        {
            // Can't delete the only frame
            if (frames.Count <= 1) return;

            var toRemove = currentFrame;
            currentFrame = currentFrame.Previous ?? currentFrame.Next;
            
            // Dispose the frame to free memory
            toRemove.Value?.Dispose();
            
            frames.Remove(toRemove);
            CurrentFrameIndex = Math.Max(0, CurrentFrameIndex - 1);
            InvalidateFrameCache();
        }
        
        public void InsertFrame()
        {
            var newFrame = new Frame(framePosition, frameSize, _layers);
            frames.AddBefore(currentFrame, newFrame);
            currentFrame = currentFrame.Previous;
            InvalidateFrameCache();
        }

        public void TogglePlaying()
        {
            IsPlaying = !IsPlaying;
        }

        public void Animate(GameTime gameTime)
        {
            if (!IsPlaying || fps <= 0 || TotalFrames == 0) return;

            double frameDuration = 1.0 / fps; // Seconds per frame
            playbackTimer += gameTime.ElapsedGameTime.TotalSeconds;

            if (playbackTimer >= frameDuration)
            {
                long elapsedFrames = (long)(playbackTimer / frameDuration);
                playbackTimer -= elapsedFrames * frameDuration;

                // Full animation cycles end on the same frame, so only traverse
                // the remaining steps through the linked list.
                int framesToAdvance = (int)(elapsedFrames % TotalFrames);
                CurrentFrameIndex = (CurrentFrameIndex + framesToAdvance) % TotalFrames;

                for (int i = 0; i < framesToAdvance; i++)
                {
                    currentFrame = currentFrame.Next ?? frames.First;
                }
            }
        }

        public void Stop()
        {
            IsPlaying = false;
        }

        public void Start()
        {
            IsPlaying = true;
        }

        public void DrawOnCurrentLayer(Color selectedColor)
        {
            Vector2 mousePositionCur = GlobalParameters.GlobalMouse.newMousePos;
            Vector2 mousePositionOld = GlobalParameters.GlobalMouse.oldMousePos;

            float xChange = mousePositionCur.X - mousePositionOld.X;
            float yChange = mousePositionCur.Y - mousePositionOld.Y;
            float distance = (float)Math.Sqrt(xChange * xChange + yChange * yChange);

            // Only draw if there's movement or it's the first draw
            if (distance < 1f && GlobalParameters.GlobalMouse.LeftClickHold()) 
            {
                // Still draw a single point for initial click
                DrawBrushAt(ToFramePosition(mousePositionCur), selectedColor);
                return;
            }

            // Use more efficient line drawing
            int steps = Math.Max(1, (int)(distance / 2));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps > 0 ? i / (float)steps : 0;
                Vector2 interpolatedPos = ToFramePosition(Vector2.Lerp(mousePositionOld, mousePositionCur, t));
                DrawBrushAt(interpolatedPos, selectedColor);
            }
        }

        private Vector2 ToFramePosition(Vector2 screenPosition)
        {
            if (DisplayBounds.Width <= 0 || DisplayBounds.Height <= 0) return screenPosition - framePosition;
            return new Vector2(
                (screenPosition.X - DisplayBounds.X) * frameSize.X / DisplayBounds.Width,
                (screenPosition.Y - DisplayBounds.Y) * frameSize.Y / DisplayBounds.Height);
        }

        public void FillCurrentLayerAt(Vector2 screenPosition, Color color)
        {
            Vector2 local = ToFramePosition(screenPosition);
            currentFrame?.Value.FloodFill(SelectedLayerId, (int)local.X, (int)local.Y, color);
        }

        public Color SampleVisibleColorAt(Vector2 screenPosition)
        {
            Vector2 local = ToFramePosition(screenPosition);
            return currentFrame?.Value.GetVisiblePixel((int)local.X, (int)local.Y) ?? Color.Transparent;
        }

        public void SelectFrame(int index)
        {
            if (index < 0 || index >= TotalFrames) return;
            currentFrame = frames.First;
            for (int i = 0; i < index; i++) currentFrame = currentFrame.Next;
            CurrentFrameIndex = index;
        }

        public void DrawCurrentFrame(Rectangle destination)
        {
            DisplayBounds = destination;
            currentFrame?.Value.Draw(destination, 1f);
            if (!IsPlaying && isOnionSkinEnabled)
            {
                for (int i = 1; i <= maxOnionFrames; i++)
                {
                    Frame frame = frames.ElementAtOrDefault(CurrentFrameIndex - i);
                    if (frame != null) frame.DrawLayers(destination, baseOpacity * (maxOnionFrames - i + 1));
                }
            }
            currentFrame?.Value.DrawLayers(destination, 1f);
        }

        private void DrawBrushAt(Vector2 localPos, Color color)
        {
            int centerX = (int)localPos.X;
            int centerY = (int)localPos.Y;
            
            // Pre-calculate brush bounds to avoid repeated boundary checks
            int minX = Math.Max(0, centerX - brushSize);
            int maxX = Math.Min(Frame.staticWidth - 1, centerX + brushSize);
            int minY = Math.Max(0, centerY - brushSize);
            int maxY = Math.Min(Frame.staticHeight - 1, centerY + brushSize);
            
            int brushSizeSquared = brushSize * brushSize;
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    
                    if (dx * dx + dy * dy <= brushSizeSquared)
                    {
                        currentFrame.Value.SetPixel(SelectedLayerId, x, y, color);
                    }
                }
            }
        }

        public Color[] GetLayerPixels(Guid layerId)
        {
            return currentFrame.Value.GetLayerPixels(layerId);
        }

        public AnimationLayer AddLayer(string name, int index = 0)
        {
            var layer = new AnimationLayer(name);
            index = Math.Clamp(index, 0, _layers.Count);
            _layers.Insert(index, layer);
            foreach (Frame frame in frames) frame.AddLayer(layer, index);
            SelectedLayerId = layer.Id;
            return layer;
        }

        public bool RemoveLayer(Guid layerId)
        {
            if (_layers.Count <= 1) return false;
            int index = _layers.FindIndex(layer => layer.Id == layerId);
            if (index < 0) return false;
            _layers.RemoveAt(index);
            foreach (Frame frame in frames) frame.RemoveLayer(layerId);
            if (SelectedLayerId == layerId) SelectedLayerId = _layers[Math.Min(index, _layers.Count - 1)].Id;
            return true;
        }

        public bool MoveLayer(Guid layerId, int newIndex)
        {
            int oldIndex = _layers.FindIndex(layer => layer.Id == layerId);
            if (oldIndex < 0 || newIndex < 0 || newIndex >= _layers.Count || oldIndex == newIndex) return false;
            AnimationLayer layer = _layers[oldIndex];
            _layers.RemoveAt(oldIndex);
            _layers.Insert(newIndex, layer);
            foreach (Frame frame in frames) frame.ReorderLayers(_layers);
            return true;
        }

        public bool RenameLayer(Guid layerId, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            AnimationLayer layer = _layers.FirstOrDefault(candidate => candidate.Id == layerId);
            if (layer == null) return false;
            layer.Rename(name);
            return true;
        }

        public bool SelectLayer(Guid layerId)
        {
            if (_layers.All(layer => layer.Id != layerId)) return false;
            SelectedLayerId = layerId;
            return true;
        }

        public bool SetLayerVisibility(Guid layerId, bool isVisible)
        {
            AnimationLayer layer = _layers.FirstOrDefault(candidate => candidate.Id == layerId);
            if (layer == null) return false;
            layer.IsVisible = isVisible;
            return true;
        }

        public void DrawCurrentFrame()
        {
            currentFrame?.Value.Draw(1.0f);

            if (!IsPlaying && isOnionSkinEnabled)
            {
                DrawOnionSkin();
            }

            DrawFrameWithOpacity(currentFrame?.Value, 1.0f);
        }

        public void DrawOnionSkin()
        {
            for (int i = 1; i <= maxOnionFrames; i++)
            {
                var frame = frames.ElementAtOrDefault(CurrentFrameIndex - i);
                if (frame != null)
                {
                    float opacity = baseOpacity * (maxOnionFrames - i + 1);
                    frame.DrawLayers(opacity);
                }
            }
        }

        private void DrawFrameWithOpacity(Frame frame, float opacity)
        {
            frame?.DrawLayers(opacity);
        }

        // Get total memory usage of all frames
        public long GetTotalMemoryUsage()
        {
            long total = 0;
            foreach (var frame in frames)
            {
                total += frame.GetMemoryUsage();
            }
            return total;
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
                // Dispose all frames
                foreach (var frame in frames)
                {
                    frame?.Dispose();
                }
                frames.Clear();
                currentFrame = null;
            }

            _disposed = true;
        }

        ~Animation()
        {
            Dispose(false);
        }
    }
}
