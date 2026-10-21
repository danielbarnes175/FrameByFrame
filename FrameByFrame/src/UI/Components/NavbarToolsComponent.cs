using System;
using System.Collections.Generic;
using FrameByFrame.src.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.UI.Components
{
    internal sealed class NavbarToolsComponent : IDisposable
    {
        private const int IconSize = 32;
        private readonly DrawingTools _drawingTools;
        private readonly List<UIIconButton> _buttons = new();
        private readonly List<DrawingTool> _kinds = new();
        private readonly Texture2D _selectionIcon;

        public IReadOnlyList<UIIconButton> Buttons => _buttons;

        public NavbarToolsComponent(DrawingTools drawingTools, Func<string, Texture2D> loadIcon)
        {
            _drawingTools = drawingTools ?? throw new ArgumentNullException(nameof(drawingTools));
            ArgumentNullException.ThrowIfNull(loadIcon);
            Add(DrawingTool.Draw, loadIcon("brush"), "Paintbrush");
            Add(DrawingTool.Eraser, loadIcon("eraser"), "Eraser");
            Add(DrawingTool.Fill, loadIcon("bucket_tool"), "Bucket fill");
            Add(DrawingTool.ColorPicker, loadIcon("eyedropper_tool"), "Color picker");
            _selectionIcon = CreateSelectionIcon();
            Add(DrawingTool.Selection, _selectionIcon, "Selection tool");
        }

        private void Add(DrawingTool tool, Texture2D icon, string tooltip)
        {
            UIIconButton button = new(icon, () => _drawingTools.SelectedTool = tool)
            {
                Tooltip = tooltip,
                UseToolSelectionStyle = true
            };
            _buttons.Add(button);
            _kinds.Add(tool);
        }

        public void Update()
        {
            foreach (UIIconButton button in _buttons) button.Update();
        }

        public void Draw()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].IsSelected = _drawingTools.SelectedTool == _kinds[i];
                _buttons[i].Draw(drawTooltip: false);
            }
        }

        public void DrawTooltips()
        {
            foreach (UIIconButton button in _buttons) button.DrawTooltip();
        }

        private static Texture2D CreateSelectionIcon()
        {
            Texture2D texture = new(GlobalParameters.GlobalGraphics, IconSize, IconSize);
            Color[] pixels = new Color[IconSize * IconSize];
            for (int i = 6; i <= 25; i++)
            {
                bool dash = ((i - 6) / 3) % 2 == 0;
                if (!dash) continue;
                pixels[i + 6 * IconSize] = UITheme.Text;
                pixels[i + 25 * IconSize] = UITheme.Text;
                pixels[6 + i * IconSize] = UITheme.Text;
                pixels[25 + i * IconSize] = UITheme.Text;
            }
            texture.SetData(pixels);
            return texture;
        }

        public void Dispose() => _selectionIcon.Dispose();
    }
}
