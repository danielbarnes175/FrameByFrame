using System;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Selection
{
    public sealed class SelectionController
    {
        private enum Gesture { None, Marquee, Move }

        private readonly Animation.Animation _animation;
        private static PixelSelection _clipboard;
        private PixelSelection _selection;
        private Frame _selectedFrame;
        private Guid _selectedLayerId;
        private Gesture _gesture;
        private Vector2 _gestureStart;
        private Rectangle _previewBounds;

        public SelectionController(Animation.Animation animation) =>
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));

        public bool HasSelection { get { EnsureCurrentTarget(); return _selection != null; } }
        public bool CanPaste => _clipboard != null;
        public Rectangle Bounds => _gesture == Gesture.None ? _selection?.Bounds ?? Rectangle.Empty : _previewBounds;

        public void Update(bool canEdit)
        {
            EnsureCurrentTarget();
            if (!GlobalParameters.GlobalMouse.LeftClickHold())
            {
                if (_gesture != Gesture.None) CompleteGesture();
                return;
            }
            if (!canEdit || UIPointerRouter.IsPointerBlocked()) return;

            Vector2 canvasPoint = _animation.ToCanvasPosition(GlobalParameters.GlobalMouse.newMousePos);
            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                if (!_animation.IsScreenPointOnCanvas(GlobalParameters.GlobalMouse.newMousePos)) return;
                _gestureStart = canvasPoint;
                _gesture = _selection != null && _selection.Bounds.Contains(canvasPoint)
                    ? Gesture.Move
                    : Gesture.Marquee;
                _previewBounds = _gesture == Gesture.Move ? _selection.Bounds : RectangleFrom(_gestureStart, canvasPoint);
            }
            else if (_gesture == Gesture.Marquee)
                _previewBounds = RectangleFrom(_gestureStart, canvasPoint);
            else if (_gesture == Gesture.Move)
            {
                Point delta = (canvasPoint - _gestureStart).ToPoint();
                _previewBounds = ClampBounds(new Rectangle(_selection.Bounds.X + delta.X,
                    _selection.Bounds.Y + delta.Y, _selection.Width, _selection.Height));
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
            PixelSelection pasted = FitWithinCanvas(_clipboard.Clone(new Rectangle(_clipboard.Bounds.X + 10,
                _clipboard.Bounds.Y + 10, _clipboard.Width, _clipboard.Height)));
            pasted = pasted.Clone(ClampBounds(pasted.Bounds));
            Color[] pixels = _animation.GetLayerPixels(_animation.SelectedLayerId);
            SelectionOperations.Composite(pixels, _animation.CurrentFrame.width, _animation.CurrentFrame.height, pasted);
            if (_animation.ReplaceCurrentLayerPixels(pixels)) SetSelection(pasted);
        }

        public void Delete()
        {
            EnsureCurrentTarget();
            if (_selection == null) return;
            Color[] pixels = _animation.GetLayerPixels(_animation.SelectedLayerId);
            SelectionOperations.Clear(pixels, _animation.CurrentFrame.width, _animation.CurrentFrame.height,
                _selection.Bounds);
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
            _selectedFrame = null;
            _gesture = Gesture.None;
            _previewBounds = Rectangle.Empty;
        }

        public void DrawOverlay()
        {
            EnsureCurrentTarget();
            Rectangle bounds = Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            Rectangle screen = _animation.ToScreenRectangle(bounds);
            DrawDashedBorder(screen, UITheme.ToolSelected);
        }

        private void CompleteGesture()
        {
            Gesture completed = _gesture;
            _gesture = Gesture.None;
            if (completed == Gesture.Marquee)
            {
                Rectangle clipped = ClampBounds(_previewBounds);
                PixelSelection selected = SelectionOperations.Extract(
                    _animation.GetLayerPixels(_animation.SelectedLayerId), _animation.CurrentFrame.width,
                    _animation.CurrentFrame.height, clipped);
                if (selected == null) Clear(); else SetSelection(selected);
            }
            else if (completed == Gesture.Move && _selection != null && _previewBounds != _selection.Bounds)
                ApplyTransform(SelectionOperations.Move(_selection, _previewBounds.X, _previewBounds.Y));
        }

        private void ApplyTransform(PixelSelection transformed)
        {
            transformed = FitWithinCanvas(transformed);
            transformed = transformed.Clone(ClampBounds(transformed.Bounds));
            Color[] pixels = _animation.GetLayerPixels(_animation.SelectedLayerId);
            SelectionOperations.Clear(pixels, _animation.CurrentFrame.width, _animation.CurrentFrame.height,
                _selection.Bounds);
            SelectionOperations.Composite(pixels, _animation.CurrentFrame.width, _animation.CurrentFrame.height,
                transformed);
            if (_animation.ReplaceCurrentLayerPixels(pixels)) SetSelection(transformed);
        }

        private void SetSelection(PixelSelection selection)
        {
            _selection = selection;
            _selectedFrame = _animation.CurrentFrame;
            _selectedLayerId = _animation.SelectedLayerId;
            _previewBounds = selection.Bounds;
        }

        private void EnsureCurrentTarget()
        {
            if (_selection != null && (!ReferenceEquals(_selectedFrame, _animation.CurrentFrame) ||
                _selectedLayerId != _animation.SelectedLayerId)) Clear();
        }

        private Rectangle ClampBounds(Rectangle bounds)
        {
            int width = Math.Min(Math.Max(1, bounds.Width), _animation.CurrentFrame.width);
            int height = Math.Min(Math.Max(1, bounds.Height), _animation.CurrentFrame.height);
            return new Rectangle(Math.Clamp(bounds.X, 0, _animation.CurrentFrame.width - width),
                Math.Clamp(bounds.Y, 0, _animation.CurrentFrame.height - height), width, height);
        }

        private PixelSelection FitWithinCanvas(PixelSelection selection)
        {
            if (selection.Width <= _animation.CurrentFrame.width &&
                selection.Height <= _animation.CurrentFrame.height) return selection;
            float fit = Math.Min(_animation.CurrentFrame.width / (float)selection.Width,
                _animation.CurrentFrame.height / (float)selection.Height);
            return SelectionOperations.Resize(selection,
                Math.Max(1, (int)Math.Floor(selection.Width * fit)),
                Math.Max(1, (int)Math.Floor(selection.Height * fit)));
        }

        private static Rectangle RectangleFrom(Vector2 first, Vector2 second)
        {
            int left = (int)Math.Floor(Math.Min(first.X, second.X));
            int top = (int)Math.Floor(Math.Min(first.Y, second.Y));
            int right = (int)Math.Ceiling(Math.Max(first.X, second.X)) + 1;
            int bottom = (int)Math.Ceiling(Math.Max(first.Y, second.Y)) + 1;
            return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }

        private static void DrawDashedBorder(Rectangle bounds, Color color)
        {
            const int dash = 7;
            for (int x = bounds.Left; x < bounds.Right; x += dash * 2)
            {
                UIRenderer.Fill(new Rectangle(x, bounds.Top, Math.Min(dash, bounds.Right - x), 2), color);
                UIRenderer.Fill(new Rectangle(x, bounds.Bottom - 2, Math.Min(dash, bounds.Right - x), 2), color);
            }
            for (int y = bounds.Top; y < bounds.Bottom; y += dash * 2)
            {
                UIRenderer.Fill(new Rectangle(bounds.Left, y, 2, Math.Min(dash, bounds.Bottom - y)), color);
                UIRenderer.Fill(new Rectangle(bounds.Right - 2, y, 2, Math.Min(dash, bounds.Bottom - y)), color);
            }
        }
    }
}
