using System;
using FrameByFrame.src.Engine;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.UI.Components
{
    public sealed class SelectionControlsComponent : UIElement
    {
        private readonly DrawingTools _tools;
        private readonly UIActionButton _copy;
        private readonly UIActionButton _paste;
        private readonly UIActionButton _delete;
        private readonly UIActionButton _shrink;
        private readonly UIActionButton _grow;
        private readonly UIActionButton _rotate;

        public SelectionControlsComponent(DrawingTools tools)
        {
            _tools = tools ?? throw new ArgumentNullException(nameof(tools));
            _copy = new UIActionButton("Copy", _tools.Selection.Copy) { Tooltip = "Copy selection - Ctrl+C" };
            _paste = new UIActionButton("Paste", _tools.Selection.Paste) { Tooltip = "Paste selection - Ctrl+V" };
            _delete = new UIActionButton("Delete", _tools.Selection.Delete) { Tooltip = "Delete selection - Delete" };
            _shrink = new UIActionButton("Size -", () => _tools.Selection.Resize(.8f)) { Tooltip = "Shrink selection" };
            _grow = new UIActionButton("Size +", () => _tools.Selection.Resize(1.25f)) { Tooltip = "Grow selection" };
            _rotate = new UIActionButton("Rotate", _tools.Selection.RotateClockwise) { Tooltip = "Rotate selection clockwise" };
        }

        public bool ShouldShow => _tools.SelectedTool == DrawingTool.Selection;

        public override void Arrange(Rectangle bounds)
        {
            base.Arrange(bounds);
            var cells = UILayoutEngine.Stack(new Rectangle(bounds.X + 6, bounds.Y + 26,
                Math.Max(1, bounds.Width - 12), Math.Max(1, bounds.Height - 32)), UIAxis.Horizontal, 6, 4);
            _copy.Arrange(cells[0]);
            _paste.Arrange(cells[1]);
            _delete.Arrange(cells[2]);
            _shrink.Arrange(cells[3]);
            _grow.Arrange(cells[4]);
            _rotate.Arrange(cells[5]);
        }

        public override void Update()
        {
            if (!ShouldShow) return;
            UIPointerRouter.Block(Bounds);
            bool selected = _tools.Selection.HasSelection;
            _copy.IsEnabled = _delete.IsEnabled = _shrink.IsEnabled = _grow.IsEnabled = _rotate.IsEnabled = selected;
            _paste.IsEnabled = _tools.Selection.CanPaste;
            _copy.Update();
            _paste.Update();
            _delete.Update();
            _shrink.Update();
            _grow.Update();
            _rotate.Update();
        }

        public override void Draw()
        {
            if (!ShouldShow) return;
            UIRenderer.Fill(Bounds, UITheme.Surface);
            UIRenderer.Border(Bounds, UITheme.Border, 2);
            new UITextContainer { Bounds = new Rectangle(Bounds.X + 6, Bounds.Y + 3, Bounds.Width - 12, 20),
                Padding = 0, MaxLines = 1 }.Draw(
                _tools.Selection.HasSelection ? "Selection actions" : "Draw around artwork to select it",
                UITheme.TextMuted, .55f);
            _copy.Draw(drawTooltip: false);
            _paste.Draw(drawTooltip: false);
            _delete.Draw(drawTooltip: false);
            _shrink.Draw(drawTooltip: false);
            _grow.Draw(drawTooltip: false);
            _rotate.Draw(drawTooltip: false);
            _copy.DrawTooltip();
            _paste.DrawTooltip();
            _delete.DrawTooltip();
            _shrink.DrawTooltip();
            _grow.DrawTooltip();
            _rotate.DrawTooltip();
        }
    }
}
