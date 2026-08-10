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
        public int fps;
        public bool IsPlaying => _timeline.IsPlaying;

        public Vector2 frameSize;
        public Vector2 framePosition;

        // Tools
        public bool isOnionSkinEnabled;
        public int brushSize;
        private int previousOnionFrames = 3;
        private int nextOnionFrames;
        public int PreviousOnionFrames
        {
            get => previousOnionFrames;
            set => previousOnionFrames = Math.Clamp(value, 0, 10);
        }
        public int NextOnionFrames
        {
            get => nextOnionFrames;
            set => nextOnionFrames = Math.Clamp(value, 0, 10);
        }
        private float baseOpacity = 0.1f;
        public float OnionSkinOpacity
        {
            get => baseOpacity;
            set => baseOpacity = Math.Clamp(value, 0f, 1f);
        }
        private readonly List<AnimationLayer> _layers;
        public IReadOnlyList<AnimationLayer> Layers => _layers;
        public Guid SelectedLayerId { get; private set; }
        public AnimationLayer SelectedLayer => _layers.FirstOrDefault(layer => layer.Id == SelectedLayerId);

        // Project Settings
        public string projectName;

        private readonly Timeline _timeline;
        public IEnumerable<Frame> Frames => _timeline.Frames;
        private bool _disposed = false;
        private const int MaxHistoryEntries = 20;
        private readonly Stack<PixelEdit> _undoHistory = new();
        private readonly Stack<PixelEdit> _redoHistory = new();
        private PixelEdit _pendingPixelEdit;

        private sealed record PixelEdit(Frame Frame, Guid LayerId, Color[] Before, Color[] After);

        public int TotalFrames => _timeline.TotalFrames;
        public int CurrentFrameIndex => _timeline.CurrentFrameIndex;
        public Frame CurrentFrame => _timeline.CurrentFrame;
        public Rectangle DisplayBounds { get; private set; }
        public const int MinCanvasDimension = 64;
        public const int MaxCanvasDimension = 4096;
        public const long MaxStoredPixels = 268_435_456;
        public long StoredPixelCount => Frames.Sum(frame => frame.NonTransparentPixelCount);
        public float ResourceBudgetRemaining => Math.Clamp(1f -
            (float)(StoredPixelCount / (double)MaxStoredPixels), 0f, 1f);

        public Animation(string projectName, IEnumerable<AnimationLayer> layers = null)
        {
            this.projectName = projectName;
            fps = 12;
            _layers = layers?.ToList() ??
                [new AnimationLayer("Layer 1"), new AnimationLayer("Layer 2"), new AnimationLayer("Layer 3")];
            if (_layers.Count == 0) throw new ArgumentException("An animation must have at least one layer.", nameof(layers));
            if (_layers.Select(layer => layer.Id).Distinct().Count() != _layers.Count)
                throw new ArgumentException("Layer IDs must be unique.", nameof(layers));
            _timeline = new Timeline(() => framePosition, () => frameSize, () => _layers,
                CommitPixelEdit, ClearEditHistory);
            SelectedLayerId = _layers[0].Id;
            brushSize = 5;
            isOnionSkinEnabled = true;
        }

        public void InitializeFrames(int width = 1200, int height = 800)
        {
            width = Math.Clamp(width, MinCanvasDimension, MaxCanvasDimension);
            height = Math.Clamp(height, MinCanvasDimension, MaxCanvasDimension);
            frameSize = new Vector2(width, height);
            framePosition = new Vector2(
                GlobalParameters.screenWidth / 2 - (int)frameSize.X / 2,
                GlobalParameters.screenHeight / 2 - (int)frameSize.Y / 2);

            _timeline.Initialize();
        }

        public void LoadFrames(IEnumerable<Frame> loadedFrames, Vector2 loadedFramePosition, Vector2 loadedFrameSize)
        {
            ArgumentNullException.ThrowIfNull(loadedFrames);

            _timeline.Load(loadedFrames);
            framePosition = loadedFramePosition;
            frameSize = loadedFrameSize;
            ClearEditHistory();
        }

        public Frame GetFrameAtIndex(int index) => _timeline.GetFrameAtIndex(index);
        public void FirstFrame() => _timeline.FirstFrame();
        public void LastFrame() => _timeline.LastFrame();
        public void NextFrame() => _timeline.NextFrame();
        public void PreviousFrame() => _timeline.PreviousFrame();

        public void EraseCurrentLayer()
        {
            BeginPixelEdit();
            CurrentFrame.ClearLayer(SelectedLayerId);
            CommitPixelEdit();
        }

        public void BeginPixelEdit()
        {
            if (_pendingPixelEdit != null || CurrentFrame == null || SelectedLayer == null) return;
            _pendingPixelEdit = new PixelEdit(CurrentFrame, SelectedLayerId,
                CurrentFrame.GetLayerPixels(SelectedLayerId), null);
        }

        public void CommitPixelEdit()
        {
            if (_pendingPixelEdit == null) return;
            Frame frame = _timeline.Contains(_pendingPixelEdit.Frame) ? _pendingPixelEdit.Frame : null;
            Color[] after = frame?.GetLayerPixels(_pendingPixelEdit.LayerId);
            PixelEdit completed = _pendingPixelEdit with { After = after };
            _pendingPixelEdit = null;
            if (after == null || completed.Before.SequenceEqual(after)) return;
            _undoHistory.Push(completed);
            while (_undoHistory.Count > MaxHistoryEntries)
            {
                PixelEdit[] entries = _undoHistory.ToArray();
                _undoHistory.Clear();
                for (int i = entries.Length - 2; i >= 0; i--) _undoHistory.Push(entries[i]);
            }
            _redoHistory.Clear();
        }

        private void ClearEditHistory()
        {
            _pendingPixelEdit = null;
            _undoHistory.Clear();
            _redoHistory.Clear();
        }

        public bool Undo()
        {
            CommitPixelEdit();
            if (_undoHistory.Count == 0) return false;
            PixelEdit edit = _undoHistory.Peek();
            Frame frame = _timeline.Contains(edit.Frame) ? edit.Frame : null;
            if (frame == null || frame.Layers.All(layer => layer.Id != edit.LayerId)) return false;
            if (!CanReplaceLayer(frame, edit.LayerId, edit.Before)) return false;
            _undoHistory.Pop();
            frame.SetLayerPixels(edit.LayerId, edit.Before, true);
            SelectFrame(_timeline.IndexOf(frame));
            SelectLayer(edit.LayerId);
            _redoHistory.Push(edit);
            return true;
        }

        public bool Redo()
        {
            CommitPixelEdit();
            if (_redoHistory.Count == 0) return false;
            PixelEdit edit = _redoHistory.Peek();
            Frame frame = _timeline.Contains(edit.Frame) ? edit.Frame : null;
            if (frame == null || frame.Layers.All(layer => layer.Id != edit.LayerId)) return false;
            if (!CanReplaceLayer(frame, edit.LayerId, edit.After)) return false;
            _redoHistory.Pop();
            frame.SetLayerPixels(edit.LayerId, edit.After, true);
            SelectFrame(_timeline.IndexOf(frame));
            SelectLayer(edit.LayerId);
            _undoHistory.Push(edit);
            return true;
        }

        public void DeleteFrame() => _timeline.DeleteFrame();
        public void InsertFrame() => _timeline.InsertFrame();
        public void DuplicateCurrentFrame() => _timeline.DuplicateCurrentFrame();
        public void CopyCurrentFrame() => _timeline.CopyCurrentFrame();
        public bool PasteFrame() => _timeline.PasteFrame();
        public bool MoveFrame(int oldIndex, int newIndex) => _timeline.MoveFrame(oldIndex, newIndex);
        public void TogglePlaying() => _timeline.TogglePlaying();
        public void Animate(GameTime gameTime) => _timeline.Animate(gameTime, fps);
        public void Stop() => _timeline.Stop();
        public void Start() => _timeline.Start();

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
            CurrentFrame?.FloodFill(SelectedLayerId, (int)local.X, (int)local.Y, color,
                Math.Max(0, MaxStoredPixels - StoredPixelCount));
        }

        public Color SampleVisibleColorAt(Vector2 screenPosition)
        {
            Vector2 local = ToFramePosition(screenPosition);
            return CurrentFrame?.GetVisiblePixel((int)local.X, (int)local.Y) ?? Color.Transparent;
        }

        public void SelectFrame(int index)
        {
            _timeline.SelectFrame(index);
        }

        public void DrawCurrentFrame(Rectangle destination)
        {
            DisplayBounds = destination;
            CurrentFrame?.Draw(destination, 1f);
            if (!IsPlaying && isOnionSkinEnabled)
            {
                for (int i = 1; i <= previousOnionFrames; i++)
                {
                    Frame frame = GetFrameAtIndex(CurrentFrameIndex - i);
                    if (frame != null) frame.DrawLayers(destination, baseOpacity * (previousOnionFrames - i + 1));
                }
                for (int i = 1; i <= nextOnionFrames; i++)
                {
                    Frame frame = GetFrameAtIndex(CurrentFrameIndex + i);
                    if (frame != null) frame.DrawLayers(destination, baseOpacity * (nextOnionFrames - i + 1));
                }
            }
            CurrentFrame?.DrawLayers(destination, 1f);
        }

        private void DrawBrushAt(Vector2 localPos, Color color)
        {
            int centerX = (int)localPos.X;
            int centerY = (int)localPos.Y;
            
            // Pre-calculate brush bounds to avoid repeated boundary checks
            int minX = Math.Max(0, centerX - brushSize);
            int maxX = Math.Min(CurrentFrame.width - 1, centerX + brushSize);
            int minY = Math.Max(0, centerY - brushSize);
            int maxY = Math.Min(CurrentFrame.height - 1, centerY + brushSize);
            
            int brushSizeSquared = brushSize * brushSize;
            long availablePixels = Math.Max(0, MaxStoredPixels - StoredPixelCount);
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    
                    if (dx * dx + dy * dy <= brushSizeSquared)
                    {
                        bool canAddPixel = color == Color.Transparent || availablePixels > 0;
                        if (CurrentFrame.SetPixel(SelectedLayerId, x, y, color, canAddPixel))
                            availablePixels--;
                    }
                }
            }
        }

        public Color[] GetLayerPixels(Guid layerId)
        {
            return CurrentFrame.GetLayerPixels(layerId);
        }

        private bool CanReplaceLayer(Frame frame, Guid layerId, Color[] replacement)
        {
            long replacementCount = replacement.LongCount(pixel => pixel != Color.Transparent);
            return StoredPixelCount - frame.GetLayerPixelCount(layerId) + replacementCount <= MaxStoredPixels;
        }

        public AnimationLayer AddLayer(string name, int index = 0)
        {
            CommitPixelEdit();
            var layer = new AnimationLayer(name);
            index = Math.Clamp(index, 0, _layers.Count);
            _layers.Insert(index, layer);
            foreach (Frame frame in Frames) frame.AddLayer(layer, index);
            SelectedLayerId = layer.Id;
            return layer;
        }


        public bool RemoveLayer(Guid layerId)
        {
            if (_layers.Count <= 1) return false;
            int index = _layers.FindIndex(layer => layer.Id == layerId);
            if (index < 0) return false;
            CommitPixelEdit();
            _layers.RemoveAt(index);
            foreach (Frame frame in Frames) frame.RemoveLayer(layerId);
            if (SelectedLayerId == layerId) SelectedLayerId = _layers[Math.Min(index, _layers.Count - 1)].Id;
            return true;
        }

        public bool MoveLayer(Guid layerId, int newIndex)
        {
            int oldIndex = _layers.FindIndex(layer => layer.Id == layerId);
            if (oldIndex < 0 || newIndex < 0 || newIndex >= _layers.Count || oldIndex == newIndex) return false;
            CommitPixelEdit();
            AnimationLayer layer = _layers[oldIndex];
            _layers.RemoveAt(oldIndex);
            _layers.Insert(newIndex, layer);
            foreach (Frame frame in Frames) frame.ReorderLayers(_layers);
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
            CommitPixelEdit();
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
            CurrentFrame?.Draw(1.0f);

            if (!IsPlaying && isOnionSkinEnabled)
            {
                DrawOnionSkin();
            }

            DrawFrameWithOpacity(CurrentFrame, 1.0f);
        }

        public void DrawOnionSkin()
        {
            for (int i = 1; i <= previousOnionFrames; i++)
            {
                Frame frame = GetFrameAtIndex(CurrentFrameIndex - i);
                if (frame != null)
                {
                    float opacity = baseOpacity * (previousOnionFrames - i + 1);
                    frame.DrawLayers(opacity);
                }
            }
            for (int i = 1; i <= nextOnionFrames; i++)
            {
                Frame frame = GetFrameAtIndex(CurrentFrameIndex + i);
                if (frame != null)
                {
                    float opacity = baseOpacity * (nextOnionFrames - i + 1);
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
            foreach (Frame frame in Frames)
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
                _timeline.Dispose();
            }

            _disposed = true;
        }

        ~Animation()
        {
            Dispose(false);
        }
    }
}
