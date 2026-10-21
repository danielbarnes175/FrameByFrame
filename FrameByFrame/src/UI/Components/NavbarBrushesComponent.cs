using System;
using System.Collections.Generic;
using FrameByFrame.src.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.UI.Components
{
    internal sealed class NavbarBrushesComponent : IDisposable
    {
        private const int IconSize = 32;
        private readonly DrawingTools _drawingTools;
        private readonly IApplicationContext _app;
        private readonly Texture2D _icon;
        private readonly UIActionButton _previous;
        private readonly UIActionButton _next;
        private readonly UIActionButton _create;
        private readonly UIActionButton _delete;
        private readonly UIActionButton _clearTip;
        private readonly UIActionButton _fillTip;
        private readonly UISlider _tipAlpha;
        private int _tipAlphaPercent = 100;
        private Rectangle _tipBounds;

        public UIIconButton Button { get; }
        public UIPopover Popover { get; } = new();

        public NavbarBrushesComponent(IApplicationContext app, DrawingTools drawingTools, Action toggle)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _drawingTools = drawingTools ?? throw new ArgumentNullException(nameof(drawingTools));
            ArgumentNullException.ThrowIfNull(toggle);
            _icon = new Texture2D(_app.Graphics, IconSize, IconSize);
            UpdateIcon();
            Button = new UIIconButton(_icon, toggle)
                { Tooltip = "Brush presets", UseToolSelectionStyle = true };
            _previous = new UIActionButton("<", () => Select(-1));
            _next = new UIActionButton(">", () => Select(1));
            _create = new UIActionButton("New", _drawingTools.CreateBrush);
            _delete = new UIActionButton("Delete", _drawingTools.DeleteSelectedBrush);
            _clearTip = new UIActionButton("Clear tip", () => _drawingTools.FillBrushTip(0));
            _fillTip = new UIActionButton("Fill tip", () => _drawingTools.FillBrushTip(byte.MaxValue));
            _tipAlpha = new UISlider(0, 100, _tipAlphaPercent, value => _tipAlphaPercent = value);
        }

        public void ArrangePopover(Rectangle bounds)
        {
            Popover.Arrange(bounds);
            int left = bounds.X + 20;
            int width = bounds.Width - 40;
            _previous.Arrange(new Rectangle(left, bounds.Y + 34, 40, 28));
            _next.Arrange(new Rectangle(bounds.Right - 60, bounds.Y + 34, 40, 28));
            IReadOnlyList<Rectangle> actions = UILayoutEngine.Stack(
                new Rectangle(left, bounds.Y + 182, width, 30), UIAxis.Horizontal, 2, 8);
            _create.Arrange(actions[0]);
            _delete.Arrange(actions[1]);
            int editorSize = Math.Min(220, Math.Max(64, bounds.Height - 250));
            Rectangle editorArea = new(left, bounds.Y + 220, editorSize, editorSize);
            _tipBounds = FitTip(editorArea, _drawingTools.SelectedBrush);
            int controlX = editorArea.Right + 16;
            int controlWidth = Math.Max(80, bounds.Right - 20 - controlX);
            _tipAlpha.Arrange(new Rectangle(controlX, bounds.Y + 248, controlWidth, 34));
            _fillTip.Arrange(new Rectangle(controlX, bounds.Y + 306, controlWidth, 34));
            _clearTip.Arrange(new Rectangle(controlX, bounds.Y + 348, controlWidth, 34));
        }

        public void UpdateContent()
        {
            BrushPreset brush = _drawingTools.SelectedBrush;
            Vector2 pointer = _app.Mouse.newMousePos;
            if (_app.Mouse.LeftClick())
            {
                foreach ((int index, Rectangle bounds) in VisiblePresetBounds())
                    if (bounds.Contains(pointer))
                    {
                        _drawingTools.SelectBrush(index);
                        brush = _drawingTools.SelectedBrush;
                        break;
                    }
            }
            bool canEdit = _drawingTools.CanEditSelectedBrush;
            _delete.IsEnabled = canEdit && _drawingTools.Brushes.Count > 1;
            _tipAlpha.IsEnabled = canEdit;
            _fillTip.IsEnabled = canEdit;
            _clearTip.IsEnabled = canEdit;
            _previous.Update(); _next.Update(); _create.Update(); _delete.Update();
            _tipAlpha.SetValue(_tipAlphaPercent); _tipAlpha.Update();
            _fillTip.Update(); _clearTip.Update();
            int editorSize = Math.Min(220, Math.Max(64, Popover.Bounds.Height - 250));
            _tipBounds = FitTip(new Rectangle(Popover.Bounds.X + 20, Popover.Bounds.Y + 220,
                editorSize, editorSize), brush);
            UpdateIcon();
            if (!canEdit || !_tipBounds.Contains(pointer) ||
                (!_app.Mouse.LeftClickHold() && !_app.Mouse.RightClickHold())) return;
            int x = Math.Clamp((int)((pointer.X - _tipBounds.X) * brush.TipWidth / _tipBounds.Width), 0, brush.TipWidth - 1);
            int y = Math.Clamp((int)((pointer.Y - _tipBounds.Y) * brush.TipHeight / _tipBounds.Height), 0, brush.TipHeight - 1);
            byte alpha = _app.Mouse.RightClickHold() ? (byte)0
                : (byte)Math.Clamp((int)Math.Round(_tipAlphaPercent / 100f * 255f), 0, 255);
            _drawingTools.SetBrushTipAlpha(x, y, alpha);
            UpdateIcon();
        }

        public void DrawContent()
        {
            Popover.Draw();
            Rectangle panel = Popover.Bounds;
            BrushPreset brush = _drawingTools.SelectedBrush;
            new UITextContainer { Bounds = new Rectangle(panel.X + 20, panel.Y + 4, panel.Width - 40, 28), MaxLines = 1 }
                .Draw("Custom brush editor", UITheme.Primary, UITextRole.Heading);
            new UITextContainer { Bounds = new Rectangle(_previous.Bounds.Right, panel.Y + 34,
                Math.Max(1, _next.Bounds.X - _previous.Bounds.Right), 28), MaxLines = 1 }
                .Draw($"{brush.Name} ({_drawingTools.SelectedBrushIndex + 1}/{_drawingTools.Brushes.Count})",
                    UITheme.Text, UITextRole.Label);
            _previous.Draw(); _next.Draw();
            foreach ((int index, Rectangle bounds) in VisiblePresetBounds())
                DrawPreset(_drawingTools.Brushes[index], bounds, index == _drawingTools.SelectedBrushIndex);
            _create.Draw(); _delete.Draw();
            DrawTip(brush);
            new UITextContainer { Bounds = new Rectangle(_tipAlpha.Bounds.X, _tipAlpha.Bounds.Y - 20,
                _tipAlpha.Bounds.Width, 20), Padding = 0, HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw($"Paint alpha: {_tipAlphaPercent}%", UITheme.TextMuted, UITextRole.Caption);
            _tipAlpha.Draw(); _fillTip.Draw(); _clearTip.Draw();
            new UITextContainer { Bounds = new Rectangle(_tipAlpha.Bounds.X, _clearTip.Bounds.Bottom + 8,
                _tipAlpha.Bounds.Width, 58), Padding = 0, HorizontalAlignment = UIAlign.Start, MaxLines = 3 }
                .Draw(brush.IsBuiltIn ? "Preset is read-only\nChoose New to edit a copy"
                    : "Left drag paints\nRight drag erases", UITheme.TextMuted, UITextRole.Caption);
            if (!string.IsNullOrWhiteSpace(_drawingTools.BrushStoreError))
                new UITextContainer { Bounds = new Rectangle(panel.X + 16, panel.Bottom - 22, panel.Width - 32, 18), MaxLines = 1 }
                    .Draw(_drawingTools.BrushStoreError, UITheme.Danger, UITextRole.Caption);
        }

        private void Select(int delta)
        {
            int count = _drawingTools.Brushes.Count;
            _drawingTools.SelectBrush((_drawingTools.SelectedBrushIndex + delta + count) % count);
        }

        private void UpdateIcon()
        {
            BrushPreset brush = _drawingTools.SelectedBrush;
            Color[] pixels = new Color[IconSize * IconSize];
            const int padding = 4;
            int size = IconSize - padding * 2;
            for (int y = padding; y < IconSize - padding; y++)
            for (int x = padding; x < IconSize - padding; x++)
            {
                byte alpha = brush.AlphaAt((x - padding) * brush.TipWidth / size,
                    (y - padding) * brush.TipHeight / size);
                if (alpha > 0) pixels[x + y * IconSize] = UITheme.Text * (alpha / 255f);
            }
            _icon.SetData(pixels);
        }

        private IEnumerable<(int Index, Rectangle Bounds)> VisiblePresetBounds()
        {
            const int columns = 5, visibleCount = 10, gap = 6;
            Rectangle panel = Popover.Bounds;
            int start = _drawingTools.SelectedBrushIndex / visibleCount * visibleCount;
            int width = (panel.Width - 40 - gap * (columns - 1)) / columns;
            for (int index = start; index < Math.Min(_drawingTools.Brushes.Count, start + visibleCount); index++)
            {
                int slot = index - start;
                yield return (index, new Rectangle(panel.X + 20 + slot % columns * (width + gap),
                    panel.Y + 66 + slot / columns * 56, width, 50));
            }
        }

        private static Rectangle FitTip(Rectangle area, BrushPreset brush)
        {
            int cell = Math.Max(1, Math.Min(area.Width / brush.TipWidth, area.Height / brush.TipHeight));
            int width = cell * brush.TipWidth, height = cell * brush.TipHeight;
            return new Rectangle(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height);
        }

        private static void DrawPreset(BrushPreset brush, Rectangle bounds, bool selected)
        {
            UIRenderer.Fill(bounds, selected ? UITheme.SurfaceRaised : UITheme.Surface);
            UIRenderer.Border(bounds, selected ? UITheme.Primary : UITheme.Border, selected ? 2 : 1);
            Rectangle preview = new(bounds.X + 6, bounds.Y + 5, 40, 40);
            UIRenderer.Fill(preview, UITheme.CanvasStage);
            DrawPreview(brush, preview, selected ? UITheme.Primary : UITheme.Text);
            new UITextContainer { Bounds = new Rectangle(preview.Right + 5, bounds.Y + 2,
                Math.Max(1, bounds.Right - preview.Right - 8), bounds.Height - 4), Padding = 0,
                HorizontalAlignment = UIAlign.Start, MaxLines = 2 }
                .Draw(brush.Name, selected ? UITheme.Primary : UITheme.Text, UITextRole.Caption);
        }

        private static void DrawPreview(BrushPreset brush, Rectangle bounds, Color color)
        {
            for (int y = 0; y < brush.TipHeight; y++)
            for (int x = 0; x < brush.TipWidth; x++)
            {
                byte alpha = brush.AlphaAt(x, y);
                if (alpha == 0) continue;
                int left = bounds.X + x * bounds.Width / brush.TipWidth;
                int top = bounds.Y + y * bounds.Height / brush.TipHeight;
                int right = bounds.X + (x + 1) * bounds.Width / brush.TipWidth;
                int bottom = bounds.Y + (y + 1) * bounds.Height / brush.TipHeight;
                UIRenderer.Fill(new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top)), color * (alpha / 255f));
            }
        }

        private void DrawTip(BrushPreset brush)
        {
            UIRenderer.Fill(_tipBounds, UITheme.CanvasStage);
            for (int y = 0; y < brush.TipHeight; y++)
            for (int x = 0; x < brush.TipWidth; x++)
            {
                int left = _tipBounds.X + x * _tipBounds.Width / brush.TipWidth;
                int top = _tipBounds.Y + y * _tipBounds.Height / brush.TipHeight;
                int right = _tipBounds.X + (x + 1) * _tipBounds.Width / brush.TipWidth;
                int bottom = _tipBounds.Y + (y + 1) * _tipBounds.Height / brush.TipHeight;
                Rectangle cell = new(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
                UIRenderer.Fill(cell, (x + y) % 2 == 0 ? UITheme.Surface : UITheme.SurfaceRaised);
                byte alpha = brush.AlphaAt(x, y);
                if (alpha > 0) UIRenderer.Fill(cell, UITheme.Text * (alpha / 255f));
                if (_tipBounds.Width / brush.TipWidth >= 5) UIRenderer.Border(cell, UITheme.Border * .45f);
            }
            UIRenderer.Border(_tipBounds, UITheme.Primary, 2);
        }

        public void Dispose() => _icon.Dispose();
    }
}
