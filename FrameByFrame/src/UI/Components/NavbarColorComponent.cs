using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.UI.Components
{
    internal sealed class NavbarColorComponent : IDisposable
    {
        public const int Width = 236;
        public const int Height = 200;
        private const int SwatchSize = 32;
        private const int WheelDiameter = 200;
        private const int ValueStripX = 208;
        private const int ValueStripWidth = 28;
        private readonly Texture2D _wheelTexture;
        private readonly Texture2D _swatchTexture;
        private readonly Color[] _swatchPixels = new Color[SwatchSize * SwatchSize];
        private float _hue;
        private float _saturation;
        private float _value;

        public UIIconButton Button { get; }
        public UIPopover Popover { get; } = new() { ShowOutline = false };
        public Color SelectedColor { get; private set; } = Color.Black;

        public NavbarColorComponent(Action toggle)
        {
            ArgumentNullException.ThrowIfNull(toggle);
            _swatchTexture = new Texture2D(GlobalParameters.GlobalGraphics, SwatchSize, SwatchSize);
            UpdateSwatch();
            Button = new UIIconButton(_swatchTexture, toggle) { Tooltip = "Brush color" };
            _wheelTexture = GeneratePickerTexture(Width, Height);
        }

        public void UpdateContent()
        {
            Rectangle bounds = Popover.Bounds;
            if (!UIPointerRouter.Held(Popover, bounds)) return;
            Vector2 local = GlobalParameters.GlobalMouse.newMousePos - new Vector2(bounds.X, bounds.Y);
            float radius = (WheelDiameter - 1) / 2f;
            Vector2 offset = local - new Vector2(radius, radius);
            if (local.X >= 0 && local.Y >= 0 && local.X < WheelDiameter &&
                local.Y < WheelDiameter && offset.Length() <= radius)
            {
                _hue = MathHelper.ToDegrees((float)Math.Atan2(offset.Y, offset.X) + MathHelper.TwoPi) % 360;
                _saturation = Math.Clamp(offset.Length() / radius, 0f, 1f);
                _value = 1f;
                CommitColor();
            }
            else if (local.X >= ValueStripX && local.X < ValueStripX + ValueStripWidth &&
                local.Y >= 0 && local.Y < bounds.Height)
            {
                _value = Math.Clamp(1f - local.Y / Math.Max(1f, bounds.Height - 1f), 0f, 1f);
                CommitColor();
            }
        }

        public void SetSelectedColor(Color color)
        {
            SelectedColor = color;
            RgbToHsv(color, out _hue, out _saturation, out _value);
            UpdateSwatch();
        }

        public void DrawContent()
        {
            Popover.Draw();
            GlobalParameters.GlobalSpriteBatch.Draw(_wheelTexture, Popover.Bounds, Color.White);
        }

        private void CommitColor()
        {
            SelectedColor = HsvToRgb(_hue, _saturation, _value);
            UpdateSwatch();
        }

        private void UpdateSwatch()
        {
            const int outer = 15 * 15;
            const int inner = 13 * 13;
            int center = SwatchSize / 2;
            for (int y = 0; y < SwatchSize; y++)
            for (int x = 0; x < SwatchSize; x++)
            {
                int dx = x - center;
                int dy = y - center;
                int distance = dx * dx + dy * dy;
                _swatchPixels[x + y * SwatchSize] = distance > outer
                    ? Color.Transparent : distance >= inner ? Color.Black : SelectedColor;
            }
            _swatchTexture.SetData(_swatchPixels);
        }

        private static Texture2D GeneratePickerTexture(int width, int height)
        {
            Texture2D texture = new(GlobalParameters.GlobalGraphics, width, height);
            Color[] colors = new Color[width * height];
            float radius = (WheelDiameter - 1) / 2f;
            Vector2 center = new(radius, radius);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = x + y * width;
                if (x < WheelDiameter)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    if (offset.Length() <= radius)
                    {
                        float hue = MathHelper.ToDegrees((float)Math.Atan2(offset.Y, offset.X) + MathHelper.TwoPi) % 360;
                        colors[index] = HsvToRgb(hue, Math.Clamp(offset.Length() / radius, 0f, 1f), 1f);
                    }
                }
                if (x >= ValueStripX && x < ValueStripX + ValueStripWidth)
                {
                    float value = 1f - y / (float)Math.Max(1, height - 1);
                    colors[index] = new Color(value, value, value);
                }
            }
            texture.SetData(colors);
            return texture;
        }

        internal static void RgbToHsv(Color color, out float hue, out float saturation, out float value)
        {
            float r = color.R / 255f, g = color.G / 255f, b = color.B / 255f;
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;
            hue = 0f;
            if (delta > 0f)
            {
                if (max == r) hue = 60f * (((g - b) / delta) % 6f);
                else if (max == g) hue = 60f * (((b - r) / delta) + 2f);
                else hue = 60f * (((r - g) / delta) + 4f);
                if (hue < 0f) hue += 360f;
            }
            saturation = max <= 0f ? 0f : delta / max;
            value = max;
        }

        internal static Color HsvToRgb(float hue, float saturation, float value)
        {
            float c = value * saturation;
            float x = c * (1 - Math.Abs((hue / 60f) % 2 - 1));
            float m = value - c;
            float r = 0, g = 0, b = 0;
            if (hue < 60) { r = c; g = x; }
            else if (hue < 120) { r = x; g = c; }
            else if (hue < 180) { g = c; b = x; }
            else if (hue < 240) { g = x; b = c; }
            else if (hue < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return new Color(r + m, g + m, b + m);
        }

        public void Dispose()
        {
            _wheelTexture.Dispose();
            _swatchTexture.Dispose();
        }
    }
}
