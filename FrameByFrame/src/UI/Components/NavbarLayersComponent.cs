using System;
using System.Collections.Generic;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.UI.Components
{
    internal sealed class NavbarLayersComponent
    {
        public const int ToolbarHeight = 44;
        public const int RowHeight = 42;
        private readonly Animation _animation;
        private readonly IApplicationContext _app;

        public UIIconButton Button { get; }
        public UIPopover Popover { get; } = new();

        public NavbarLayersComponent(IApplicationContext app, Animation animation, UIIconButton button)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));
            Button = button ?? throw new ArgumentNullException(nameof(button));
        }

        public void UpdateContent()
        {
            Rectangle panel = Popover.Bounds;
            Rectangle toolbar = new(panel.X + 8, panel.Bottom - ToolbarHeight, panel.Width - 16, 36);
            IReadOnlyList<Rectangle> actions = UILayoutEngine.Stack(toolbar, UIAxis.Horizontal, 4, 6);
            if (_animation.CanAddLayer && UIPointerRouter.Clicked(actions[0]))
                _animation.AddLayer($"Layer {_animation.Layers.Count + 1}");
            else if (UIPointerRouter.Clicked(actions[1]))
                _animation.RemoveLayer(_animation.SelectedLayerId);
            else if (UIPointerRouter.Clicked(actions[2]))
            {
                int selected = SelectedIndex();
                if (selected > 0) _animation.MoveLayer(_animation.SelectedLayerId, selected - 1);
            }
            else if (UIPointerRouter.Clicked(actions[3]))
            {
                int selected = SelectedIndex();
                if (selected >= 0 && selected < _animation.Layers.Count - 1)
                    _animation.MoveLayer(_animation.SelectedLayerId, selected + 1);
            }

            int visibleRows = Math.Max(1, (panel.Height - ToolbarHeight) / RowHeight);
            int start = Math.Max(0, SelectedIndex() - visibleRows + 1);
            for (int i = start; i < Math.Min(_animation.Layers.Count, start + visibleRows); i++)
            {
                Rectangle row = new(panel.X + 4, panel.Y + (i - start) * RowHeight,
                    panel.Width - 8, RowHeight);
                if (!UIPointerRouter.Clicked(row)) continue;
                Rectangle visibility = new(row.X, row.Y, 44, row.Height);
                if (visibility.Contains(_app.Mouse.newMousePos))
                    _animation.SetLayerVisibility(_animation.Layers[i].Id, !_animation.Layers[i].IsVisible);
                else
                    _animation.SelectLayer(_animation.Layers[i].Id);
                break;
            }
        }

        public void DrawContent()
        {
            Popover.Draw();
            Rectangle panel = Popover.Bounds;
            int visibleRows = Math.Max(1, (panel.Height - ToolbarHeight) / RowHeight);
            int start = Math.Max(0, SelectedIndex() - visibleRows + 1);
            for (int i = start; i < Math.Min(_animation.Layers.Count, start + visibleRows); i++)
            {
                AnimationLayer layer = _animation.Layers[i];
                Rectangle row = new(panel.X + 4, panel.Y + (i - start) * RowHeight,
                    panel.Width - 8, RowHeight);
                bool selected = _animation.SelectedLayerId == layer.Id;
                UIRenderer.Fill(row, selected ? UITheme.Primary : UITheme.SurfaceRaised);
                Rectangle visibility = new(row.X, row.Y, 44, row.Height);
                Rectangle visibilityBox = new(visibility.Center.X - 9, visibility.Center.Y - 9, 18, 18);
                UIRenderer.Fill(visibilityBox, layer.IsVisible ? Color.White : Color.Black);
                UIRenderer.Border(visibilityBox, layer.IsVisible ? Color.Black : Color.White, 2);
                Rectangle nameBounds = new(row.X + 44, row.Y, row.Width - 44, row.Height);
                string status = $"{(layer.IsLocked ? "Locked - " : "")}{layer.Name}";
                new UITextContainer { Bounds = nameBounds, HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                    .Draw(status, selected ? Color.White : UITheme.Text, UITextRole.Label);
            }
            Rectangle toolbar = new(panel.X + 8, panel.Bottom - ToolbarHeight, panel.Width - 16, 36);
            IReadOnlyList<Rectangle> actions = UILayoutEngine.Stack(toolbar, UIAxis.Horizontal, 4, 6);
            string[] labels = { "+", "-", "^", "v" };
            for (int i = 0; i < actions.Count; i++)
            {
                bool enabled = i != 0 || _animation.CanAddLayer;
                UIRenderer.Fill(actions[i], enabled ? UITheme.SurfaceRaised : UITheme.Surface);
                UIRenderer.Border(actions[i], UITheme.Border, 2);
                UIRenderer.CenteredText(labels[i], actions[i],
                    !enabled ? UITheme.TextMuted : i == 1 ? UITheme.Danger : UITheme.Text, UITextRole.Button);
            }
            if (!_animation.CanAddLayer && UIPointerRouter.ContainsPointer(actions[0]))
                UIRenderer.Tooltip(_animation.LayerLimitMessage, actions[0], preferBelow: false, border: UITheme.Border);
            UIRenderer.Border(panel, UITheme.Primary, 3);
        }

        private int SelectedIndex()
        {
            for (int i = 0; i < _animation.Layers.Count; i++)
                if (_animation.Layers[i].Id == _animation.SelectedLayerId) return i;
            return -1;
        }
    }
}
