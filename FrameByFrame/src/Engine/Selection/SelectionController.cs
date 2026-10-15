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
            if (!GlobalParameters.GlobalMouse.LeftClickHold())
            {
                if (_gesture != Gesture.None) CompleteGesture();
                return;
            }
            if (!canEdit || UIPointerRouter.IsPointerBlocked() || _animation.CurrentFrame == null) return;

            Vector2 canvasPoint = _animation.ToCanvasPosition(GlobalParameters.GlobalMouse.newMousePos);
            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                bool isOnCanvas = _animation.IsScreenPointOnCanvas(GlobalParameters.GlobalMouse.newMousePos);
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
            Color[] pixels = _animation.GetCurrentLayerPixels();
            PixelSelection underlay = SelectionOperations.Capture(pixels, _animation.CurrentFrame.width,
                _animation.CurrentFrame.height, pasted);
            SelectionOperations.Composite(pixels, _animation.CurrentFrame.width, _animation.CurrentFrame.height, pasted);
            if (_animation.ReplaceCurrentLayerPixels(pixels)) SetSelection(pasted, underlay);
        }

        public void Delete()
        {
            EnsureCurrentTarget();
            if (_selection == null) return;
            Color[] pixels = _animation.GetCurrentLayerPixels();
            RemoveSelectionFrom(pixels);
            if (_animation.ReplaceCurrentLayerPixels(pixels)) Clear();
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
                    _animation.GetCurrentLayerPixels(), _animation.CurrentFrame.width,
                    _animation.CurrentFrame.height, _lassoPoints);
                if (selected == null) Clear(); else SetSelection(selected);
            }
            else if (completed == Gesture.Move && _selection != null && _previewBounds != _selection.Bounds)
                ApplyTransform(SelectionOperations.Move(_selection, _previewBounds.X, _previewBounds.Y));
            _lassoPoints.Clear();
        }

        private void ApplyTransform(PixelSelection transformed)
        {
            Color[] pixels = _animation.GetCurrentLayerPixels();
            RemoveSelectionFrom(pixels);
            PixelSelection underlay = SelectionOperations.Capture(pixels, _animation.CurrentFrame.width,
                _animation.CurrentFrame.height, transformed);
            SelectionOperations.Composite(pixels, _animation.CurrentFrame.width, _animation.CurrentFrame.height,
                transformed);
            if (_animation.ReplaceCurrentLayerPixels(pixels)) SetSelection(transformed, underlay);
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

        private void RemoveSelectionFrom(Color[] pixels)
        {
            if (_underlay != null)
                SelectionOperations.Restore(pixels, _animation.CurrentFrame.width,
                    _animation.CurrentFrame.height, _underlay);
            else
                SelectionOperations.Clear(pixels, _animation.CurrentFrame.width,
                    _animation.CurrentFrame.height, _selection!);
        }

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
