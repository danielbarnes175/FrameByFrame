using System;
using FrameByFrame.src.Engine;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.UI.Components
{
    public sealed class CanvasViewControlsComponent : UIElement
    {
        private readonly CanvasView _view;
        private readonly UIActionButton _zoomOut;
        private readonly UIActionButton _zoomIn;
        private readonly UIActionButton _rotateLeft;
        private readonly UIActionButton _rotateRight;
        private readonly UIActionButton _reset;
        private Rectangle _statusBounds;

        public CanvasViewControlsComponent(CanvasView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _zoomOut = new UIActionButton("-", () => _view.ZoomBy(-CanvasView.ZoomStep)) { Tooltip = "Zoom out" };
            _zoomIn = new UIActionButton("+", () => _view.ZoomBy(CanvasView.ZoomStep)) { Tooltip = "Zoom in" };
            _rotateLeft = new UIActionButton("< 15", () => _view.RotateBy(-CanvasView.RotationStep)) { Tooltip = "Rotate left 15 degrees" };
            _rotateRight = new UIActionButton("15 >", () => _view.RotateBy(CanvasView.RotationStep)) { Tooltip = "Rotate right 15 degrees" };
            _reset = new UIActionButton("Reset", _view.Reset) { Tooltip = "Reset canvas view" };
        }

        public override void Arrange(Rectangle bounds)
        {
            base.Arrange(bounds);
            _statusBounds = new Rectangle(bounds.X + 6, bounds.Y + 4, bounds.Width - 12, 20);
            Rectangle actions = new(bounds.X + 6, bounds.Y + 26, bounds.Width - 12, Math.Max(1, bounds.Height - 32));
            var cells = UILayoutEngine.Stack(actions, UIAxis.Horizontal, 5, 4);
            _zoomOut.Arrange(cells[0]);
            _zoomIn.Arrange(cells[1]);
            _rotateLeft.Arrange(cells[2]);
            _rotateRight.Arrange(cells[3]);
            _reset.Arrange(cells[4]);
        }

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            _zoomOut.IsEnabled = _view.Zoom > CanvasView.MinimumZoom;
            _zoomIn.IsEnabled = _view.Zoom < CanvasView.MaximumZoom;
            _zoomOut.Update();
            _zoomIn.Update();
            _rotateLeft.Update();
            _rotateRight.Update();
            _reset.Update();
        }

        public override void Draw()
        {
            UIRenderer.Fill(Bounds, UITheme.Surface);
            UIRenderer.Border(Bounds, UITheme.Border, 2);
            new UITextContainer { Bounds = _statusBounds, Padding = 0, MaxLines = 1 }
                .Draw($"View {(int)Math.Round(_view.Zoom * 100)}% | {_view.RotationDegrees:0} deg", UITheme.TextMuted, .55f);
            _zoomOut.Draw(drawTooltip: false);
            _zoomIn.Draw(drawTooltip: false);
            _rotateLeft.Draw(drawTooltip: false);
            _rotateRight.Draw(drawTooltip: false);
            _reset.Draw(drawTooltip: false);
            _zoomOut.DrawTooltip();
            _zoomIn.DrawTooltip();
            _rotateLeft.DrawTooltip();
            _rotateRight.DrawTooltip();
            _reset.DrawTooltip();
        }
    }
}
