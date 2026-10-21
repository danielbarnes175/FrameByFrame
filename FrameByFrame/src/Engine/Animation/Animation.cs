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
using System.Threading;

namespace FrameByFrame.src.Engine.Animation
{
    public class Animation : IDisposable
    {
        // Animation
        private int _fps = 12;
        public int fps
        {
            get => _fps;
            set
            {
                int validated = value is >= 1 and <= MaxFramesPerSecond
                    ? value
                    : throw new ArgumentOutOfRangeException(nameof(value), $"FPS must be between 1 and {MaxFramesPerSecond}.");
                if (_fps == validated) return;
                _fps = validated;
                MarkChanged();
            }
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
            set
            {
                string validated = ProjectModelValidator.ProjectName(value, nameof(projectName));
                if (_projectName == validated) return;
                _projectName = validated;
                MarkChanged();
            }
        }
        public bool IsCanvasBackgroundTransparent { get; private set; }
        public Color CanvasBackgroundColor { get; private set; } = Color.White;

        private readonly Timeline _timeline;
        private Frame? _thumbnailFrame;
        private readonly List<AudioTrack> _audioTracks = new();
        public IEnumerable<Frame> Frames => _timeline.Frames;
        public IReadOnlyList<AudioTrack> AudioTracks => _audioTracks;
        private bool _disposed = false;
        internal const long MaxHistoryBytes = 64L * 1024 * 1024;
        private const long EstimatedDeltaBytes = 24;
        internal static long EstimateHistoryBytes(long changedPixelCount) =>
            checked(Math.Max(0, changedPixelCount) * EstimatedDeltaBytes);
        internal long HistoryByteBudget { get; set; } = MaxHistoryBytes;
        private readonly LinkedList<PixelEdit> _undoHistory = new();
        private readonly LinkedList<PixelEdit> _redoHistory = new();
        private PendingPixelEdit? _pendingPixelEdit;
        private long _historyBytes;
        private readonly Dictionary<int, byte> _strokeCoverage = new();
        private Point _strokePatternOrigin;
        private bool _hasStrokePatternOrigin;
        private CanvasView? _displayView;
        private long _contentRevision;
        private long _savedRevision;

        private readonly record struct PixelDelta(int Index, Color Before, Color After);
        private sealed record PixelEdit(Frame Frame, Guid LayerId, PixelDelta[] Changes, long EstimatedBytes);
        private sealed record PendingPixelEdit(Frame Frame, Guid LayerId, Dictionary<int, Color> Before);

        public int TotalFrames => _timeline.TotalFrames;
        public int CurrentFrameIndex => _timeline.CurrentFrameIndex;
        public double PlaybackPositionSeconds => _timeline.PlaybackPositionSeconds(fps);
        public long PlaybackRevision => _timeline.PlaybackRevision;
        public long AudioRevision { get; private set; }
        public long ContentRevision => Interlocked.Read(ref _contentRevision);
        public bool IsDirty => ContentRevision != Interlocked.Read(ref _savedRevision);
        internal long HistoryBytes => _historyBytes;
        public string LastHistoryError { get; private set; } = string.Empty;
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
            // CanvasView owns screen placement; the persisted model stays viewport-independent.
            framePosition = Vector2.Zero;

            _timeline.Initialize();
            MarkChanged();
        }

        internal void MarkChanged() => Interlocked.Increment(ref _contentRevision);

        internal void MarkSaved(long revision)
        {
            long saved;
            do
            {
                saved = Interlocked.Read(ref _savedRevision);
                if (saved >= revision) return;
            } while (Interlocked.CompareExchange(ref _savedRevision, revision, saved) != saved);
        }

        internal void MarkSaved() => MarkSaved(ContentRevision);

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
            MarkChanged();
            return true;
        }

        public bool RemoveAudioTrack(Guid id)
        {
            AudioTrack? track = _audioTracks.FirstOrDefault(candidate => candidate.Id == id);
            if (track == null) return false;
            _audioTracks.Remove(track);
            AudioRevision++;
            MarkChanged();
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
            MarkChanged();
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
            MarkChanged();
            return true;
        }

        public bool SetAudioTrackMuted(Guid id, bool isMuted)
        {
            AudioTrack? track = _audioTracks.FirstOrDefault(candidate => candidate.Id == id);
            if (track == null) return false;
            if (track.IsMuted == isMuted) return true;
            track.IsMuted = isMuted;
            AudioRevision++;
            MarkChanged();
            return true;
        }

        public Frame? GetFrameAtIndex(int index) => _timeline.GetFrameAtIndex(index);
        public void FirstFrame() => _timeline.FirstFrame();
        public void LastFrame() => _timeline.LastFrame();
        public void NextFrame()
        {
            if (CurrentFrameIndex == TotalFrames - 1 && !CanAddFrame) return;
            int count = TotalFrames;
            _timeline.NextFrame();
            if (TotalFrames != count) MarkChanged();
        }
        public void PreviousFrame() => _timeline.PreviousFrame();

        public void EraseCurrentLayer()
        {
            if (CurrentFrame.GetLayerPixelCount(SelectedLayerId) > HistoryByteBudget / EstimatedDeltaBytes)
            {
                LastHistoryError = "The edit was not applied because it exceeds the undo memory budget.";
                return;
            }
            BeginPixelEdit();
            foreach (KeyValuePair<int, Color> pixel in CurrentFrame.GetSparseLayerPixels(SelectedLayerId))
                TrackPixelBefore(pixel.Key, pixel.Value);
            CurrentFrame.ClearLayer(SelectedLayerId);
            CommitPixelEdit();
        }

        public void BeginPixelEdit()
        {
            if (_pendingPixelEdit != null || CurrentFrame == null || SelectedLayer == null) return;
            _pendingPixelEdit = new PendingPixelEdit(CurrentFrame, SelectedLayerId, new Dictionary<int, Color>());
            LastHistoryError = string.Empty;
            _strokeCoverage.Clear();
            _hasStrokePatternOrigin = false;
        }

        public void CommitPixelEdit()
        {
            if (_pendingPixelEdit == null) return;
            PendingPixelEdit pending = _pendingPixelEdit;
            Frame? frame = _timeline.Contains(pending.Frame) ? pending.Frame : null;
            PixelDelta[] changes = frame == null ? [] : pending.Before
                .Select(pixel => new PixelDelta(pixel.Key, pixel.Value,
                    frame.GetLayerPixel(pending.LayerId, pixel.Key)))
                .Where(change => change.Before != change.After)
                .ToArray();
            _pendingPixelEdit = null;
            _strokeCoverage.Clear();
            _hasStrokePatternOrigin = false;
            if (changes.Length == 0) return;
            long bytes = EstimateHistoryBytes(changes.LongLength);
            if (bytes > HistoryByteBudget)
            {
                frame!.ApplyPixelChanges(pending.LayerId, changes.Select(change =>
                    new KeyValuePair<int, Color>(change.Index, change.Before)));
                LastHistoryError = "The edit was reverted because it exceeds the undo memory budget.";
                return;
            }
            ClearRedoHistory();
            _undoHistory.AddLast(new PixelEdit(frame!, pending.LayerId, changes, bytes));
            _historyBytes += bytes;
            TrimHistoryToBudget();
            MarkChanged();
        }

        private void ClearEditHistory()
        {
            _pendingPixelEdit = null;
            _strokeCoverage.Clear();
            _hasStrokePatternOrigin = false;
            _undoHistory.Clear();
            _redoHistory.Clear();
            _historyBytes = 0;
            LastHistoryError = string.Empty;
        }

        public bool Undo()
        {
            CommitPixelEdit();
            if (_undoHistory.Count == 0) return false;
            PixelEdit edit = _undoHistory.Last!.Value;
            Frame? frame = _timeline.Contains(edit.Frame) ? edit.Frame : null;
            if (frame == null || frame.Layers.All(layer => layer.Id != edit.LayerId)) return false;
            if (!CanApplyDeltas(frame, edit, useAfter: false)) return false;
            _undoHistory.RemoveLast();
            frame.ApplyPixelChanges(edit.LayerId, edit.Changes.Select(change =>
                new KeyValuePair<int, Color>(change.Index, change.Before)));
            SelectFrame(_timeline.IndexOf(frame));
            SelectLayer(edit.LayerId);
            _redoHistory.AddLast(edit);
            MarkChanged();
            return true;
        }

        public bool Redo()
        {
            CommitPixelEdit();
            if (_redoHistory.Count == 0) return false;
            PixelEdit edit = _redoHistory.Last!.Value;
            Frame? frame = _timeline.Contains(edit.Frame) ? edit.Frame : null;
            if (frame == null || frame.Layers.All(layer => layer.Id != edit.LayerId)) return false;
            if (!CanApplyDeltas(frame, edit, useAfter: true)) return false;
            _redoHistory.RemoveLast();
            frame.ApplyPixelChanges(edit.LayerId, edit.Changes.Select(change =>
                new KeyValuePair<int, Color>(change.Index, change.After)));
            SelectFrame(_timeline.IndexOf(frame));
            SelectLayer(edit.LayerId);
            _undoHistory.AddLast(edit);
            MarkChanged();
            return true;
        }

        public void DeleteFrame()
        {
            Frame deletedFrame = CurrentFrame;
            _timeline.DeleteFrame();
            if (ReferenceEquals(_thumbnailFrame, deletedFrame) && !_timeline.Contains(deletedFrame))
                _thumbnailFrame = null;
            ClampAudioTrackStarts();
            MarkChanged();
        }
        public void InsertFrame()
        {
            if (!CanAddFrame) return;
            _timeline.InsertFrame();
            MarkChanged();
        }
        public void DuplicateCurrentFrame()
        {
            if (!CanDuplicateFrame) return;
            _timeline.DuplicateCurrentFrame();
            MarkChanged();
        }
        public void CopyCurrentFrame() => _timeline.CopyCurrentFrame();
        public bool PasteFrame()
        {
            bool changed = CanPasteFrame && _timeline.PasteFrame();
            if (changed) MarkChanged();
            return changed;
        }
        public bool MoveFrame(int oldIndex, int newIndex) { bool changed = _timeline.MoveFrame(oldIndex, newIndex); if (changed) MarkChanged(); return changed; }
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
            Vector2 mousePositionCur = ApplicationRuntime.Current.Mouse.newMousePos;
            Vector2 mousePositionOld = ApplicationRuntime.Current.Mouse.oldMousePos;
            Vector2 current = ToFramePosition(mousePositionCur);
            Vector2 previous = ToFramePosition(mousePositionOld);
            float distance = Vector2.Distance(current, previous);
            if (!_hasStrokePatternOrigin)
            {
                _strokePatternOrigin = new Point((int)current.X - brushSize, (int)current.Y - brushSize);
                _hasStrokePatternOrigin = true;
            }

            if (ApplicationRuntime.Current.Mouse.LeftClick())
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
            int x = (int)local.X;
            int y = (int)local.Y;
            Color previous = CurrentFrame.GetLayerPixel(SelectedLayerId, x, y);
            bool changed = CurrentFrame.FloodFill(SelectedLayerId, x, y, color,
                Math.Max(0, MaxStoredPixels - StoredPixelCount),
                HistoryByteBudget / EstimatedDeltaBytes, (index, before) => TrackPixelBefore(index, before));
            if (!changed && previous != color)
                LastHistoryError = "The fill was not applied because it exceeds a project or undo memory budget.";
        }

        public Color SampleVisibleColorAt(Vector2 screenPosition)
        {
            Vector2 local = ToFramePosition(screenPosition);
            Color background = IsCanvasBackgroundTransparent ? Color.Transparent : CanvasBackgroundColor;
            return CurrentFrame?.GetVisiblePixel((int)local.X, (int)local.Y, background) ?? background;
        }

        public void SetCanvasBackgroundTransparent(bool isTransparent)
        {
            if (IsCanvasBackgroundTransparent == isTransparent) return;
            IsCanvasBackgroundTransparent = isTransparent;
            MarkChanged();
        }

        public void SetCanvasBackgroundColor(Color color)
        {
            Color opaque = new Color(color.R, color.G, color.B, byte.MaxValue);
            if (CanvasBackgroundColor == opaque && !IsCanvasBackgroundTransparent) return;
            CanvasBackgroundColor = opaque;
            IsCanvasBackgroundTransparent = false;
            MarkChanged();
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
                        if (!TrackPixelBefore(pixelIndex, existing)) continue;
                        Color baseline = _pendingPixelEdit != null &&
                            _pendingPixelEdit.Before.TryGetValue(pixelIndex, out Color before) ? before : existing;
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
            for (int i = 0; i < pixels.Length; i++)
            {
                Color existing = CurrentFrame.GetLayerPixel(SelectedLayerId, i);
                if (existing != pixels[i] && !TrackPixelBefore(i, existing))
                {
                    CancelPixelEdit();
                    return false;
                }
            }
            CurrentFrame.SetLayerPixels(SelectedLayerId, pixels);
            CommitPixelEdit();
            return string.IsNullOrEmpty(LastHistoryError);
        }

        public Color GetCurrentLayerPixel(int x, int y) =>
            CurrentFrame.GetLayerPixel(SelectedLayerId, x, y);

        public bool ApplyCurrentLayerPixelChanges(IReadOnlyDictionary<int, Color> changes)
        {
            ArgumentNullException.ThrowIfNull(changes);
            if (SelectedLayer?.IsLocked != false || changes.Count == 0) return false;
            if (changes.Count > HistoryByteBudget / EstimatedDeltaBytes)
            {
                LastHistoryError = "The edit was not applied because it exceeds the undo memory budget.";
                return false;
            }
            long opaqueDelta = 0;
            foreach (KeyValuePair<int, Color> change in changes)
            {
                Color existing = CurrentFrame.GetLayerPixel(SelectedLayerId, change.Key);
                if (existing == Color.Transparent && change.Value != Color.Transparent) opaqueDelta++;
                else if (existing != Color.Transparent && change.Value == Color.Transparent) opaqueDelta--;
            }
            if (StoredPixelCount + opaqueDelta > MaxStoredPixels) return false;
            CommitPixelEdit();
            BeginPixelEdit();
            foreach (KeyValuePair<int, Color> change in changes)
                TrackPixelBefore(change.Key, CurrentFrame.GetLayerPixel(SelectedLayerId, change.Key));
            CurrentFrame.ApplyPixelChanges(SelectedLayerId, changes);
            CommitPixelEdit();
            return string.IsNullOrEmpty(LastHistoryError);
        }

        private bool CanReplaceLayer(Frame frame, Guid layerId, Color[] replacement)
        {
            long replacementCount = replacement.LongCount(pixel => pixel != Color.Transparent);
            return StoredPixelCount - frame.GetLayerPixelCount(layerId) + replacementCount <= MaxStoredPixels;
        }

        private bool TrackPixelBefore(int index, Color color)
        {
            if (_pendingPixelEdit == null) return false;
            if (_pendingPixelEdit.Before.ContainsKey(index)) return true;
            if (_pendingPixelEdit.Before.Count >= HistoryByteBudget / EstimatedDeltaBytes)
            {
                LastHistoryError = "The edit was not applied because it exceeds the undo memory budget.";
                return false;
            }
            _pendingPixelEdit.Before.Add(index, color);
            return true;
        }

        private void CancelPixelEdit()
        {
            _pendingPixelEdit = null;
            _strokeCoverage.Clear();
            _hasStrokePatternOrigin = false;
        }

        private bool CanApplyDeltas(Frame frame, PixelEdit edit, bool useAfter)
        {
            long opaqueDelta = 0;
            foreach (PixelDelta change in edit.Changes)
            {
                Color current = frame.GetLayerPixel(edit.LayerId, change.Index);
                Color replacement = useAfter ? change.After : change.Before;
                if (current == Color.Transparent && replacement != Color.Transparent) opaqueDelta++;
                else if (current != Color.Transparent && replacement == Color.Transparent) opaqueDelta--;
            }
            return StoredPixelCount + opaqueDelta <= MaxStoredPixels;
        }

        private void ClearRedoHistory()
        {
            foreach (PixelEdit edit in _redoHistory) _historyBytes -= edit.EstimatedBytes;
            _redoHistory.Clear();
        }

        private void TrimHistoryToBudget()
        {
            while (_historyBytes > HistoryByteBudget)
            {
                if (_undoHistory.First != null)
                {
                    _historyBytes -= _undoHistory.First.Value.EstimatedBytes;
                    _undoHistory.RemoveFirst();
                }
                else if (_redoHistory.First != null)
                {
                    _historyBytes -= _redoHistory.First.Value.EstimatedBytes;
                    _redoHistory.RemoveFirst();
                }
                else break;
            }
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
            MarkChanged();
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
            MarkChanged();
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
            MarkChanged();
            return true;
        }

        public bool RenameLayer(Guid layerId, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            AnimationLayer? layer = _layers.FirstOrDefault(candidate => candidate.Id == layerId);
            if (layer == null) return false;
            if (layer.Name == name.Trim()) return true;
            layer.Rename(name);
            MarkChanged();
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
            if (layer.IsVisible == isVisible) return true;
            layer.IsVisible = isVisible;
            MarkChanged();
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
