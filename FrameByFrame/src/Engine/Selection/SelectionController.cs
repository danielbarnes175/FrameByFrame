using System;
using System.Collections.Generic;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Selection
{
    public sealed class SelectionController
    {
        private enum Gesture { None, Lasso, Move }

        private readonly Animation.Animation _animation;
        private readonly List<Vector2> _lassoPoints = new();
        private static PixelSelection? _clipboard;
        private PixelSelection? _selection;
        private PixelSelection? _underlay;
        private Frame? _selectedFrame;
        private Guid _selectedLayerId;
        private Gesture _gesture;
        private Vector2 _gestureStart;
        private Rectangle _previewBounds;

        public SelectionController(Animation.Animation animation) =>
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));

        public bool HasSelection { get { EnsureCurrentTarget(); return _selection != null; } }
        public bool CanPaste => _clipboard != null;
        public Rectangle Bounds => _gesture == Gesture.Move ? _previewBounds : _selection?.Bounds ?? Rectangle.Empty;

        public void Update(bool canEdit)
        {
            EnsureCurrentTarget();
            if (!ApplicationRuntime.Current.Mouse.LeftClickHold())
            {
                if (_gesture != Gesture.None) CompleteGesture();
                return;
            }
            if (!canEdit || UIPointerRouter.IsPointerBlocked() || _animation.CurrentFrame == null) return;

            Vector2 canvasPoint = _animation.ToCanvasPosition(ApplicationRuntime.Current.Mouse.newMousePos);
            if (ApplicationRuntime.Current.Mouse.LeftClick())
            {
                bool isOnCanvas = _animation.IsScreenPointOnCanvas(ApplicationRuntime.Current.Mouse.newMousePos);
                bool isOnSelection = _selection != null && _selection.Contains(canvasPoint);
                if (!isOnCanvas && !isOnSelection) return;
                _gestureStart = canvasPoint;
                if (isOnSelection)
                {
                    _gesture = Gesture.Move;
                    _previewBounds = _selection!.Bounds;
                }
                else
                {
                    _gesture = Gesture.Lasso;
                    _lassoPoints.Clear();
                    _lassoPoints.Add(ClampCanvasPoint(canvasPoint));
                }
            }
            else if (_gesture == Gesture.Lasso)
                AddLassoPoint(ClampCanvasPoint(canvasPoint));
            else if (_gesture == Gesture.Move)
            {
                Point delta = (canvasPoint - _gestureStart).ToPoint();
                _previewBounds = new Rectangle(_selection!.Bounds.X + delta.X,
                    _selection.Bounds.Y + delta.Y, _selection.Width, _selection.Height);
            }
        }

        public void Copy()
        {
            EnsureCurrentTarget();
            if (_selection != null) _clipboard = _selection.Clone();
        }

        public void Paste()
        {
            EnsureCurrentTarget();
            if (_clipboard == null || _animation.CurrentFrame == null) return;
            PixelSelection pasted = SelectionOperations.PrepareForPaste(_clipboard.Clone());
            var changes = new Dictionary<int, Color>();
            PixelSelection underlay = Capture(pasted, changes);
            Composite(changes, pasted);
            if (_animation.ApplyCurrentLayerPixelChanges(changes)) SetSelection(pasted, underlay);
        }

        public void Delete()
        {
            EnsureCurrentTarget();
            if (_selection == null) return;
            var changes = new Dictionary<int, Color>();
            RemoveSelectionFrom(changes);
            if (_animation.ApplyCurrentLayerPixelChanges(changes)) Clear();
        }

        public void Resize(float scale)
        {
            EnsureCurrentTarget();
            if (_selection == null) return;
            int width = Math.Clamp((int)Math.Round(_selection.Width * scale), 1, _animation.CurrentFrame.width);
            int height = Math.Clamp((int)Math.Round(_selection.Height * scale), 1, _animation.CurrentFrame.height);
            ApplyTransform(SelectionOperations.Resize(_selection, width, height));
        }

        public void RotateClockwise()
        {
            EnsureCurrentTarget();
            if (_selection != null) ApplyTransform(SelectionOperations.RotateClockwise(_selection));
        }

        public void Clear()
        {
            _selection = null;
            _underlay = null;
            _selectedFrame = null;
            _gesture = Gesture.None;
            _lassoPoints.Clear();
            _previewBounds = Rectangle.Empty;
        }

        public void DrawOverlay()
        {
            EnsureCurrentTarget();
            if (_gesture == Gesture.Lasso)
            {
                DrawLasso();
                return;
            }
            if (_selection == null) return;

            Rectangle bounds = _gesture == Gesture.Move ? _previewBounds : _selection.Bounds;
            foreach ((Vector2 start, Vector2 end) in _selection.Boundary)
            {
                Vector2 offset = bounds.Location.ToVector2();
                UIRenderer.Line(_animation.ToScreenPosition(offset + start),
                    _animation.ToScreenPosition(offset + end), UITheme.ToolSelected, 2f);
            }
        }

        private void CompleteGesture()
        {
            Gesture completed = _gesture;
            _gesture = Gesture.None;
            if (completed == Gesture.Lasso)
            {
                PixelSelection? selected = SelectionOperations.ExtractFreeform(
                    _animation.GetCurrentLayerPixel, _animation.CurrentFrame.width,
                    _animation.CurrentFrame.height, _lassoPoints);
                if (selected == null) Clear(); else SetSelection(selected);
            }
            else if (completed == Gesture.Move && _selection != null && _previewBounds != _selection.Bounds)
                ApplyTransform(SelectionOperations.Move(_selection, _previewBounds.X, _previewBounds.Y));
            _lassoPoints.Clear();
        }

        private void ApplyTransform(PixelSelection transformed)
        {
            var changes = new Dictionary<int, Color>();
            RemoveSelectionFrom(changes);
            PixelSelection underlay = Capture(transformed, changes);
            Composite(changes, transformed);
            if (_animation.ApplyCurrentLayerPixelChanges(changes)) SetSelection(transformed, underlay);
        }

        private void SetSelection(PixelSelection selection, PixelSelection? underlay = null)
        {
            _selection = selection;
            _underlay = underlay;
            _selectedFrame = _animation.CurrentFrame;
            _selectedLayerId = _animation.SelectedLayerId;
            _previewBounds = selection.Bounds;
            _lassoPoints.Clear();
        }

        private void RemoveSelectionFrom(Dictionary<int, Color> changes)
        {
            PixelSelection source = _underlay ?? _selection!;
            for (int y = 0; y < source.Height; y++)
            for (int x = 0; x < source.Width; x++)
            {
                int sourceIndex = x + y * source.Width;
                if (!source.Mask[sourceIndex]) continue;
                SetChange(changes, source.Bounds.X + x, source.Bounds.Y + y,
                    _underlay == null ? Color.Transparent : source.Pixels[sourceIndex]);
            }
        }

        private PixelSelection Capture(PixelSelection shape, IReadOnlyDictionary<int, Color> changes)
        {
            Color[] pixels = new Color[shape.Width * shape.Height];
            for (int y = 0; y < shape.Height; y++)
            for (int x = 0; x < shape.Width; x++)
            {
                int sourceIndex = x + y * shape.Width;
                if (!shape.Mask[sourceIndex]) continue;
                int canvasX = shape.Bounds.X + x;
                int canvasY = shape.Bounds.Y + y;
                if (!IsOnCanvas(canvasX, canvasY)) continue;
                int canvasIndex = canvasX + canvasY * _animation.CurrentFrame.width;
                pixels[sourceIndex] = changes.TryGetValue(canvasIndex, out Color changed)
                    ? changed : _animation.GetCurrentLayerPixel(canvasX, canvasY);
            }
            return new PixelSelection(shape.Bounds, pixels, shape.Mask);
        }

        private void Composite(Dictionary<int, Color> changes, PixelSelection selection)
        {
            for (int y = 0; y < selection.Height; y++)
            for (int x = 0; x < selection.Width; x++)
            {
                int sourceIndex = x + y * selection.Width;
                if (!selection.Mask[sourceIndex] || selection.Pixels[sourceIndex].A == 0) continue;
                SetChange(changes, selection.Bounds.X + x, selection.Bounds.Y + y,
                    selection.Pixels[sourceIndex]);
            }
        }

        private void SetChange(Dictionary<int, Color> changes, int x, int y, Color color)
        {
            if (IsOnCanvas(x, y)) changes[x + y * _animation.CurrentFrame.width] = color;
        }

        private bool IsOnCanvas(int x, int y) => x >= 0 && x < _animation.CurrentFrame.width &&
            y >= 0 && y < _animation.CurrentFrame.height;

        private void EnsureCurrentTarget()
        {
            if (_selection != null && (!ReferenceEquals(_selectedFrame, _animation.CurrentFrame) ||
                _selectedLayerId != _animation.SelectedLayerId)) Clear();
        }

        private void AddLassoPoint(Vector2 canvasPoint)
        {
            if (_lassoPoints.Count == 0 || Vector2.DistanceSquared(_lassoPoints[^1], canvasPoint) >= .25f)
                _lassoPoints.Add(canvasPoint);
        }

        private void DrawLasso()
        {
            if (_lassoPoints.Count < 2) return;
            for (int i = 1; i < _lassoPoints.Count; i++)
                UIRenderer.Line(_animation.ToScreenPosition(_lassoPoints[i - 1]),
                    _animation.ToScreenPosition(_lassoPoints[i]), UITheme.ToolSelected, 2f);
            if (_lassoPoints.Count > 2)
                UIRenderer.Line(_animation.ToScreenPosition(_lassoPoints[^1]),
                    _animation.ToScreenPosition(_lassoPoints[0]), UITheme.ToolSelected, 2f);
        }

        private Vector2 ClampCanvasPoint(Vector2 point) => new(
            Math.Clamp(point.X, 0f, _animation.CurrentFrame.width),
            Math.Clamp(point.Y, 0f, _animation.CurrentFrame.height));

    }
}
