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
        private int _fps = 12;
        public int fps
        {
            get => _fps;
            set => _fps = value is >= 1 and <= MaxFramesPerSecond
                ? value
                : throw new ArgumentOutOfRangeException(nameof(value), $"FPS must be between 1 and {MaxFramesPerSecond}.");
        }
        public bool IsPlaying => _timeline.IsPlaying;

        private Vector2 _frameSize;
        private Vector2 _framePosition;
        public Vector2 frameSize
        {
            get => _frameSize;
            private set => _frameSize = ProjectModelValidator.CanvasSize(value, nameof(frameSize));
        }
        public Vector2 framePosition
        {
            get => _framePosition;
            private set => _framePosition = ProjectModelValidator.Position(value, nameof(framePosition));
        }

        // Tools
        public bool isOnionSkinEnabled;
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
        public AnimationLayer? SelectedLayer => _layers.FirstOrDefault(layer => layer.Id == SelectedLayerId);

        // Project Settings
        private string _projectName = null!;
        public string projectName
        {
            get => _projectName;
            set => _projectName = ProjectModelValidator.ProjectName(value, nameof(projectName));
        }
        public bool IsCanvasBackgroundTransparent { get; private set; }
        public Color CanvasBackgroundColor { get; private set; } = Color.White;

        private readonly Timeline _timeline;
        private Frame? _thumbnailFrame;
        private readonly List<AudioTrack> _audioTracks = new();
        public IEnumerable<Frame> Frames => _timeline.Frames;
        public IReadOnlyList<AudioTrack> AudioTracks => _audioTracks;
        private bool _disposed = false;
        private const int MaxHistoryEntries = 20;
        private readonly Stack<PixelEdit> _undoHistory = new();
        private readonly Stack<PixelEdit> _redoHistory = new();
        private PixelEdit? _pendingPixelEdit;
        private readonly Dictionary<int, byte> _strokeCoverage = new();
        private Point _strokePatternOrigin;
        private bool _hasStrokePatternOrigin;
        private CanvasView? _displayView;

        private sealed record PixelEdit(Frame Frame, Guid LayerId, Color[] Before, Color[]? After);

        public int TotalFrames => _timeline.TotalFrames;
        public int CurrentFrameIndex => _timeline.CurrentFrameIndex;
        public double PlaybackPositionSeconds => _timeline.PlaybackPositionSeconds(fps);
        public long PlaybackRevision => _timeline.PlaybackRevision;
        public long AudioRevision { get; private set; }
        public int ThumbnailFrameIndex
        {
            get
            {
                int index = _timeline.IndexOf(_thumbnailFrame);
                return index >= 0 ? index : 0;
            }
        }
        public Frame CurrentFrame => _timeline.CurrentFrame;
        public Rectangle DisplayBounds { get; private set; }
        public const int MinCanvasDimension = ProjectLimits.MinCanvasDimension;
        public const int MaxCanvasDimension = ProjectLimits.MaxCanvasDimension;
        public const int MaxFramesPerSecond = ProjectLimits.MaxFramesPerSecond;
        public const long MaxStoredPixels = ProjectLimits.MaxStoredPixels;
        public long StoredPixelCount => Frames.Sum(frame => frame.NonTransparentPixelCount);
        public bool CanAddFrame => CanUseShape(_layers.Count, TotalFrames + 1);
        public bool CanDuplicateFrame => TotalFrames > 0 && CanAddFrame &&
            StoredPixelCount + CurrentFrame.NonTransparentPixelCount <= MaxStoredPixels;
        public bool CanPasteFrame => CanAddFrame && _timeline.HasClipboard &&
            StoredPixelCount + _timeline.ClipboardPixelCount <= MaxStoredPixels;
        public bool CanAddLayer => CanUseShape(_layers.Count + 1, Math.Max(1, TotalFrames));
        public string FrameLimitMessage => $"Project limit reached ({ProjectLimits.MaxFrameCount:N0} frames or combined memory budget).";
        public string LayerLimitMessage => $"Project limit reached ({ProjectLimits.MaxLayerCount} layers or combined memory budget).";
        public float ResourceBudgetRemaining => Math.Clamp(1f -
            (float)(StoredPixelCount / (double)MaxStoredPixels), 0f, 1f);

        public Animation(string projectName, IEnumerable<AnimationLayer>? layers = null)
        {
            this.projectName = projectName;
            _layers = layers?.ToList() ??
                [new AnimationLayer("Layer 1"), new AnimationLayer("Layer 2"), new AnimationLayer("Layer 3")];
            if (_layers.Count == 0) throw new ArgumentException("An animation must have at least one layer.", nameof(layers));
            if (_layers.Count > ProjectLimits.MaxLayerCount)
                throw new ArgumentException($"An animation cannot contain more than {ProjectLimits.MaxLayerCount} layers.", nameof(layers));
            if (_layers.Select(layer => layer.Id).Distinct().Count() != _layers.Count)
                throw new ArgumentException("Layer IDs must be unique.", nameof(layers));
            _timeline = new Timeline(() => framePosition, () => frameSize, () => _layers,
                CommitPixelEdit, ClearEditHistory);
            SelectedLayerId = _layers[0].Id;
            isOnionSkinEnabled = true;
        }

        public void InitializeFrames(int width = 1200, int height = 800)
        {
            frameSize = new Vector2(width, height);
            framePosition = new Vector2(
                GlobalParameters.screenWidth / 2 - (int)frameSize.X / 2,
                GlobalParameters.screenHeight / 2 - (int)frameSize.Y / 2);

            _timeline.Initialize();
        }

        public void LoadFrames(IEnumerable<Frame> loadedFrames, Vector2 loadedFramePosition, Vector2 loadedFrameSize)
        {
            ArgumentNullException.ThrowIfNull(loadedFrames);
            List<Frame> frames = loadedFrames.ToList();
            ProjectLimits.ValidateProjectShape((int)loadedFrameSize.X, (int)loadedFrameSize.Y,
                _layers.Count, frames.Count);
            if (frames.Any(frame => frame.width != (int)loadedFrameSize.X || frame.height != (int)loadedFrameSize.Y))
                throw new ArgumentException("Every frame must match the project canvas dimensions.", nameof(loadedFrames));
            if (frames.Sum(frame => frame.NonTransparentPixelCount) > MaxStoredPixels)
                throw new ArgumentException("The project exceeds the supported stored pixel budget.", nameof(loadedFrames));
            framePosition = loadedFramePosition;
            frameSize = loadedFrameSize;
            _timeline.Load(frames);
            ClearEditHistory();
        }

        public void CaptureThumbnailFrame() => _thumbnailFrame = CurrentFrame;

        public void RestoreThumbnailFrame(int index) =>
            _thumbnailFrame = GetFrameAtIndex(index) ?? GetFrameAtIndex(0);

        public bool AddAudioTrack(AudioTrack track)
        {
            ArgumentNullException.ThrowIfNull(track);
            if (!Audio.AudioService.IsSupportedExtension(track.SourceExtension))
                throw new InvalidOperationException("The audio track uses an unsupported source format.");
            if (_audioTracks.Any(candidate => candidate.Id == track.Id)) return false;
            if (_audioTracks.Count >= Audio.AudioService.MaxAudioTrackCount)
                throw new InvalidOperationException("The project contains too many audio tracks.");
            if (_audioTracks.Sum(candidate => (long)candidate.EncodedData.Length) + track.EncodedData.Length >
                Audio.AudioService.MaxProjectAudioBytes)
                throw new InvalidOperationException("The project exceeds the supported embedded audio limit.");
            if (_audioTracks.Sum(candidate => (long)(candidate.PcmData?.Length ?? 0)) +
                (track.PcmData?.Length ?? 0) > Audio.AudioService.MaxDecodedProjectBytes)
                throw new InvalidOperationException("The project exceeds the supported decoded audio limit.");
            track.StartFrame = Math.Clamp(track.StartFrame, 0, Math.Max(0, TotalFrames - 1));
            _audioTracks.Add(track);
            AudioRevision++;
            return true;
        }

        public bool RemoveAudioTrack(Guid id)
        {
            AudioTrack? track = _audioTracks.FirstOrDefault(candidate => candidate.Id == id);
            if (track == null) return false;
            _audioTracks.Remove(track);
            AudioRevision++;
            return true;
        }

        public bool SetAudioTrackStart(Guid id, int frame)
        {
            AudioTrack? track = _audioTracks.FirstOrDefault(candidate => candidate.Id == id);
            if (track == null) return false;
            int lastFrame = Math.Max(0, TotalFrames - 1);
            if (frame < 0 || frame > lastFrame)
                throw new ArgumentOutOfRangeException(nameof(frame), $"Audio must start between frame 0 and {lastFrame}.");
            if (track.StartFrame == frame) return true;
            track.StartFrame = frame;
            AudioRevision++;
            return true;
        }

        public bool SetAudioTrackVolume(Guid id, float volume)
        {
            AudioTrack? track = _audioTracks.FirstOrDefault(candidate => candidate.Id == id);
            if (track == null) return false;
            if (!float.IsFinite(volume) || volume is < 0f or > 1f)
                throw new ArgumentOutOfRangeException(nameof(volume), "Audio volume must be between zero and one.");
            if (Math.Abs(track.Volume - volume) < .0001f) return true;
            track.Volume = volume;
            AudioRevision++;
            return true;
        }

        public bool SetAudioTrackMuted(Guid id, bool isMuted)
        {
            AudioTrack? track = _audioTracks.FirstOrDefault(candidate => candidate.Id == id);
            if (track == null) return false;
            if (track.IsMuted == isMuted) return true;
            track.IsMuted = isMuted;
            AudioRevision++;
            return true;
        }

        public Frame? GetFrameAtIndex(int index) => _timeline.GetFrameAtIndex(index);
        public void FirstFrame() => _timeline.FirstFrame();
        public void LastFrame() => _timeline.LastFrame();
        public void NextFrame()
        {
            if (CurrentFrameIndex == TotalFrames - 1 && !CanAddFrame) return;
            _timeline.NextFrame();
        }
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
            _strokeCoverage.Clear();
            _hasStrokePatternOrigin = false;
        }

        public void CommitPixelEdit()
        {
            if (_pendingPixelEdit == null) return;
            Frame? frame = _timeline.Contains(_pendingPixelEdit.Frame) ? _pendingPixelEdit.Frame : null;
            Color[]? after = frame?.GetLayerPixels(_pendingPixelEdit.LayerId);
            PixelEdit completed = _pendingPixelEdit with { After = after };
            _pendingPixelEdit = null;
            _strokeCoverage.Clear();
            _hasStrokePatternOrigin = false;
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
            _strokeCoverage.Clear();
            _hasStrokePatternOrigin = false;
            _undoHistory.Clear();
            _redoHistory.Clear();
        }

        public bool Undo()
        {
            CommitPixelEdit();
            if (_undoHistory.Count == 0) return false;
            PixelEdit edit = _undoHistory.Peek();
            Frame? frame = _timeline.Contains(edit.Frame) ? edit.Frame : null;
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
            Frame? frame = _timeline.Contains(edit.Frame) ? edit.Frame : null;
            if (frame == null || frame.Layers.All(layer => layer.Id != edit.LayerId)) return false;
            if (edit.After == null || !CanReplaceLayer(frame, edit.LayerId, edit.After)) return false;
            _redoHistory.Pop();
            frame.SetLayerPixels(edit.LayerId, edit.After, true);
            SelectFrame(_timeline.IndexOf(frame));
            SelectLayer(edit.LayerId);
            _undoHistory.Push(edit);
            return true;
        }

        public void DeleteFrame()
        {
            Frame deletedFrame = CurrentFrame;
            _timeline.DeleteFrame();
            if (ReferenceEquals(_thumbnailFrame, deletedFrame) && !_timeline.Contains(deletedFrame))
                _thumbnailFrame = null;
            ClampAudioTrackStarts();
        }
        public void InsertFrame()
        {
            if (CanAddFrame) _timeline.InsertFrame();
        }
        public void DuplicateCurrentFrame()
        {
            if (CanDuplicateFrame) _timeline.DuplicateCurrentFrame();
        }
        public void CopyCurrentFrame() => _timeline.CopyCurrentFrame();
        public bool PasteFrame() => CanPasteFrame && _timeline.PasteFrame();
        public bool MoveFrame(int oldIndex, int newIndex) => _timeline.MoveFrame(oldIndex, newIndex);
        public void TogglePlaying() => _timeline.TogglePlaying();
        public void Animate(GameTime gameTime) => _timeline.Animate(gameTime, fps);
        public void Stop() => _timeline.Stop();
        public void Start() => _timeline.Start();

        public void DrawOnCurrentLayer(Color selectedColor, BrushPreset brush, int brushSize, bool erase)
        {
            ArgumentNullException.ThrowIfNull(brush);
            if (_pendingPixelEdit == null ||
                !ReferenceEquals(_pendingPixelEdit.Frame, CurrentFrame) ||
                _pendingPixelEdit.LayerId != SelectedLayerId) return;
            Vector2 mousePositionCur = GlobalParameters.GlobalMouse.newMousePos;
            Vector2 mousePositionOld = GlobalParameters.GlobalMouse.oldMousePos;
            Vector2 current = ToFramePosition(mousePositionCur);
            Vector2 previous = ToFramePosition(mousePositionOld);
            float distance = Vector2.Distance(current, previous);
            if (!_hasStrokePatternOrigin)
            {
                _strokePatternOrigin = new Point((int)current.X - brushSize, (int)current.Y - brushSize);
                _hasStrokePatternOrigin = true;
            }

            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                DrawBrushAt(current, selectedColor, brush, brushSize, erase);
                return;
            }

            if (distance < .01f) return;

            float spacing = Math.Max(1f, brushSize * .5f);
            int steps = Math.Max(1, (int)Math.Ceiling(distance / spacing));
            for (int i = 0; i <= steps; i++)
            {
                float t = steps > 0 ? i / (float)steps : 0;
                DrawBrushAt(Vector2.Lerp(previous, current, t), selectedColor, brush, brushSize, erase);
            }
        }

        private Vector2 ToFramePosition(Vector2 screenPosition)
        {
            if (_displayView != null) return _displayView.ScreenToCanvas(screenPosition);
            if (DisplayBounds.Width <= 0 || DisplayBounds.Height <= 0) return screenPosition - framePosition;
            return new Vector2(
                (screenPosition.X - DisplayBounds.X) * frameSize.X / DisplayBounds.Width,
                (screenPosition.Y - DisplayBounds.Y) * frameSize.Y / DisplayBounds.Height);
        }

        public Vector2 ToCanvasPosition(Vector2 screenPosition) => ToFramePosition(screenPosition);

        public Vector2 ToScreenPosition(Vector2 canvasPosition)
        {
            if (_displayView != null) return _displayView.CanvasToScreen(canvasPosition);
            if (DisplayBounds.Width <= 0 || DisplayBounds.Height <= 0) return canvasPosition + framePosition;
            return new Vector2(
                DisplayBounds.X + canvasPosition.X * DisplayBounds.Width / frameSize.X,
                DisplayBounds.Y + canvasPosition.Y * DisplayBounds.Height / frameSize.Y);
        }

        public bool IsScreenPointOnCanvas(Vector2 screenPosition) =>
            _displayView?.ContainsScreenPoint(screenPosition) ?? DisplayBounds.Contains(screenPosition);

        public void FillCurrentLayerAt(Vector2 screenPosition, Color color)
        {
            Vector2 local = ToFramePosition(screenPosition);
            CurrentFrame?.FloodFill(SelectedLayerId, (int)local.X, (int)local.Y, color,
                Math.Max(0, MaxStoredPixels - StoredPixelCount));
        }

        public Color SampleVisibleColorAt(Vector2 screenPosition)
        {
            Vector2 local = ToFramePosition(screenPosition);
            Color background = IsCanvasBackgroundTransparent ? Color.Transparent : CanvasBackgroundColor;
            return CurrentFrame?.GetVisiblePixel((int)local.X, (int)local.Y, background) ?? background;
        }

        public void SetCanvasBackgroundTransparent(bool isTransparent) =>
            IsCanvasBackgroundTransparent = isTransparent;

        public void SetCanvasBackgroundColor(Color color)
        {
            CanvasBackgroundColor = new Color(color.R, color.G, color.B, byte.MaxValue);
            IsCanvasBackgroundTransparent = false;
        }

        public void SelectFrame(int index)
        {
            _timeline.SelectFrame(index);
        }

        public void DrawCurrentFrame(Rectangle destination)
        {
            CanvasView view = new();
            view.Arrange(destination, (int)frameSize.X, (int)frameSize.Y);
            DrawCurrentFrame(view);
        }

        public void DrawCurrentFrame(CanvasView view)
        {
            ArgumentNullException.ThrowIfNull(view);
            _displayView = view;
            DisplayBounds = new Rectangle(
                (int)Math.Round(view.Center.X - frameSize.X * view.Scale / 2f),
                (int)Math.Round(view.Center.Y - frameSize.Y * view.Scale / 2f),
                Math.Max(1, (int)Math.Round(frameSize.X * view.Scale)),
                Math.Max(1, (int)Math.Round(frameSize.Y * view.Scale)));
            DrawingService.DrawCanvasBackground(view, IsCanvasBackgroundTransparent, CanvasBackgroundColor);
            if (!IsPlaying && isOnionSkinEnabled)
            {
                for (int i = 1; i <= previousOnionFrames; i++)
                {
                    Frame? frame = GetFrameAtIndex(CurrentFrameIndex - i);
                    if (frame != null) frame.DrawLayers(view,
                        CalculateOnionOpacity(baseOpacity, previousOnionFrames, i));
                }
                for (int i = 1; i <= nextOnionFrames; i++)
                {
                    Frame? frame = GetFrameAtIndex(CurrentFrameIndex + i);
                    if (frame != null) frame.DrawLayers(view,
                        CalculateOnionOpacity(baseOpacity, nextOnionFrames, i));
                }
            }
            CurrentFrame.DrawLayers(view, 1f);
            DrawingService.DrawCanvasOutline(view, UI.UITheme.Border);
        }

        private void DrawBrushAt(Vector2 localPos, Color color, BrushPreset brush, int brushSize, bool erase)
        {
            int centerX = (int)localPos.X;
            int centerY = (int)localPos.Y;
            // Pre-calculate brush bounds to avoid repeated boundary checks
            int minX = Math.Max(0, centerX - brushSize);
            int maxX = Math.Min(CurrentFrame.width - 1, centerX + brushSize);
            int minY = Math.Max(0, centerY - brushSize);
            int maxY = Math.Min(CurrentFrame.height - 1, centerY + brushSize);
            
            long availablePixels = Math.Max(0, MaxStoredPixels - StoredPixelCount);
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    
                    byte tipAlpha = BrushRasterizer.Sample(brush, dx, dy, brushSize,
                        x - _strokePatternOrigin.X, y - _strokePatternOrigin.Y);
                    if (tipAlpha > 0)
                    {
                        int pixelIndex = x + y * CurrentFrame.width;
                        if (_strokeCoverage.TryGetValue(pixelIndex, out byte appliedAlpha) &&
                            appliedAlpha >= tipAlpha) continue;
                        Color existing = CurrentFrame.GetLayerPixel(SelectedLayerId, x, y);
                        Color baseline = _pendingPixelEdit?.Before?[pixelIndex] ?? existing;
                        Color result = BrushRasterizer.Apply(baseline, color, tipAlpha, erase);
                        bool canAddPixel = result == Color.Transparent || existing != Color.Transparent || availablePixels > 0;
                        if (CurrentFrame.SetPixel(SelectedLayerId, x, y, result, canAddPixel))
                        {
                            _strokeCoverage[pixelIndex] = tipAlpha;
                            availablePixels--;
                        }
                    }
                }
            }
        }

        public Color[] GetLayerPixels(Guid layerId)
        {
            return CurrentFrame.GetLayerPixels(layerId);
        }

        public Color[] GetCurrentLayerPixels() => CurrentFrame.GetLayerPixels(SelectedLayerId);

        public bool ReplaceCurrentLayerPixels(Color[] pixels)
        {
            ArgumentNullException.ThrowIfNull(pixels);
            if (CurrentFrame == null || SelectedLayer == null || SelectedLayer.IsLocked ||
                pixels.Length != CurrentFrame.width * CurrentFrame.height ||
                !CanReplaceLayer(CurrentFrame, SelectedLayerId, pixels)) return false;
            CommitPixelEdit();
            BeginPixelEdit();
            CurrentFrame.SetLayerPixels(SelectedLayerId, pixels);
            CommitPixelEdit();
            return true;
        }

        private bool CanReplaceLayer(Frame frame, Guid layerId, Color[] replacement)
        {
            long replacementCount = replacement.LongCount(pixel => pixel != Color.Transparent);
            return StoredPixelCount - frame.GetLayerPixelCount(layerId) + replacementCount <= MaxStoredPixels;
        }

        public AnimationLayer AddLayer(string name, int index = 0)
        {
            if (!CanAddLayer) throw new InvalidOperationException(LayerLimitMessage);
            CommitPixelEdit();
            var layer = new AnimationLayer(name);
            index = Math.Clamp(index, 0, _layers.Count);
            _layers.Insert(index, layer);
            foreach (Frame frame in Frames) frame.AddLayer(layer, index);
            SelectedLayerId = layer.Id;
            return layer;
        }

        private static bool CanUseShape(int layerCount, int frameCount)
        {
            try
            {
                ProjectLimits.ValidateResourceCounts(layerCount, frameCount);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
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
            AnimationLayer? layer = _layers.FirstOrDefault(candidate => candidate.Id == layerId);
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
            AnimationLayer? layer = _layers.FirstOrDefault(candidate => candidate.Id == layerId);
            if (layer == null) return false;
            layer.IsVisible = isVisible;
            return true;
        }

        internal static float CalculateOnionOpacity(float baseOpacity, int frameCount, int distance)
        {
            if (frameCount <= 0 || distance <= 0 || distance > frameCount) return 0f;
            float normalizedBase = Math.Clamp(baseOpacity, 0f, 1f);
            float falloff = (frameCount - distance + 1) / (float)frameCount;
            return Math.Clamp(normalizedBase * falloff, 0f, 1f);
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
                _audioTracks.Clear();
            }

            _disposed = true;
        }

        ~Animation()
        {
            Dispose(false);
        }

        private void ClampAudioTrackStarts()
        {
            int lastFrame = Math.Max(0, TotalFrames - 1);
            bool changed = false;
            foreach (AudioTrack track in _audioTracks)
            {
                int clamped = Math.Clamp(track.StartFrame, 0, lastFrame);
                if (clamped == track.StartFrame) continue;
                track.StartFrame = clamped;
                changed = true;
            }
            if (changed) AudioRevision++;
        }
    }
}
