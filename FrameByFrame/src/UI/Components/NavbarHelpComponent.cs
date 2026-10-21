using System;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.UI.Components
{
    internal sealed class NavbarHelpComponent
    {
        private const int ContentHeight = 470;
        private int _scroll;

        private static readonly string[] Shortcuts =
        {
            "Ctrl+Z / Ctrl+Y - Undo or redo",
            "Ctrl+D - Duplicate selected frame",
            "Ctrl+C / Ctrl+V - Copy or paste frame/selection",
            "Drag timeline frames - Reorder frames",
            "P - Play or pause",
            "N / M - Previous or next frame",
            "B - Insert a frame",
            "Delete - Remove current frame",
            "O - Toggle onion skin",
            "[ / ] - Change brush size",
            "Middle mouse drag - Pan canvas view",
            "Right mouse drag - Rotate canvas view",
            "Mouse wheel - Zoom canvas view",
            "Backspace - Clear selected layer",
            "L - Save project",
            "Esc - Return home"
        };

        public UIIconButton Button { get; }
        public UIPopover Popover { get; } = new();

        public NavbarHelpComponent(UIIconButton button) =>
            Button = button ?? throw new ArgumentNullException(nameof(button));

        public void ArrangePopover(Rectangle bounds)
        {
            Popover.Arrange(bounds);
            _scroll = Math.Clamp(_scroll, 0, Math.Max(0, ContentHeight - bounds.Height));
        }

        public void UpdateContent()
        {
            Rectangle panel = Popover.Bounds;
            if (panel.Contains(GlobalParameters.GlobalMouse.newMousePos) && GlobalParameters.GlobalMouse.ScrollDelta != 0)
            {
                int maxScroll = Math.Max(0, ContentHeight - panel.Height);
                _scroll = Math.Clamp(_scroll - Math.Sign(GlobalParameters.GlobalMouse.ScrollDelta) * 36, 0, maxScroll);
            }
        }

        public void DrawContent()
        {
            Popover.Draw();
            Rectangle panel = Popover.Bounds;
            Rectangle viewport = new(panel.X + 4, panel.Y + 64,
                Math.Max(1, panel.Width - 8), Math.Max(1, panel.Height - 68));
            new UITextContainer
            {
                Bounds = new Rectangle(panel.X + 22, panel.Y + 18, panel.Width - 44, 48),
                HorizontalAlignment = UIAlign.Start,
                MaxLines = 1
            }.Draw("Keyboard shortcuts", UITheme.Primary, UITextRole.Title);
            for (int i = 0; i < Shortcuts.Length; i++)
            {
                Rectangle row = new(panel.X + 22, panel.Y + 72 + i * 24 - _scroll, panel.Width - 44, 24);
                if (row.X < viewport.X || row.Right > viewport.Right ||
                    row.Y < viewport.Y || row.Bottom > viewport.Bottom) continue;
                new UITextContainer
                {
                    Bounds = row,
                    HorizontalAlignment = UIAlign.Start,
                    Padding = 4,
                    MaxLines = 1
                }.Draw(Shortcuts[i], UITheme.Text, UITextRole.Body);
            }
        }
    }
}
