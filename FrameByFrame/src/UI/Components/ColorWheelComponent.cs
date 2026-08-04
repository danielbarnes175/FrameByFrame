using System;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine
{
    public class ColorWheelComponent : Overlay
    {
        private const int ValueStripGap = 8;
        private const int ValueStripWidth = 28;

        private readonly int _wheelDiameter;
        private readonly int _valueStripX;
        private float _selectedHue;
        private float _selectedSaturation;
        private float _selectedValue = 1f;

        public Action<Color> OnColorSelected;
        public Color SelectedColor { get; private set; } = Color.Black;

        public ColorWheelComponent(Vector2 position, Vector2 dimensions) : base((Texture2D)null, position, dimensions)
        {
            _wheelDiameter = Math.Min((int)dimensions.Y, (int)dimensions.X - ValueStripGap - ValueStripWidth);
            _valueStripX = _wheelDiameter + ValueStripGap;
            texture = GeneratePickerTexture((int)dimensions.X, (int)dimensions.Y);
            SetColorData();
            ShowOutline = false;
        }

        private Texture2D GeneratePickerTexture(int width, int height)
        {
            Texture2D picker = new(GlobalParameters.GlobalGraphics, width, height);
            Color[] colors = new Color[width * height];
            float radius = (_wheelDiameter - 1) / 2f;
            Vector2 center = new(radius, radius);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    if (x < _wheelDiameter && y < _wheelDiameter)
                    {
                        Vector2 offset = new Vector2(x, y) - center;
                        float distance = offset.Length();
                        if (distance <= radius)
                        {
                            float hue = MathHelper.ToDegrees((float)Math.Atan2(offset.Y, offset.X) + MathHelper.TwoPi) % 360;
                            float saturation = Math.Clamp(distance / radius, 0f, 1f);
                            colors[index] = HSVToRGB(hue, saturation, 1f);
                        }
                    }

                    if (x >= _valueStripX && x < _valueStripX + ValueStripWidth)
                    {
                        float value = 1f - y / (float)Math.Max(1, height - 1);
                        colors[index] = new Color(value, value, value);
                    }
                }
            }

            picker.SetData(colors);
            return picker;
        }

        public override void Update()
        {
            base.Update();
            if (!GlobalParameters.GlobalMouse.LeftClickHold()) return;

            Vector2 local = GlobalParameters.GlobalMouse.newMousePos - position;
            if (local.X < 0 || local.Y < 0 || local.X >= dimensions.X || local.Y >= dimensions.Y) return;

            float radius = (_wheelDiameter - 1) / 2f;
            Vector2 offset = local - new Vector2(radius, radius);
            if (local.X < _wheelDiameter && local.Y < _wheelDiameter && offset.Length() <= radius)
            {
                _selectedHue = MathHelper.ToDegrees((float)Math.Atan2(offset.Y, offset.X) + MathHelper.TwoPi) % 360;
                _selectedSaturation = Math.Clamp(offset.Length() / radius, 0f, 1f);
                _selectedValue = 1f;
                CommitSelection();
            }
            else if (local.X >= _valueStripX && local.X < _valueStripX + ValueStripWidth)
            {
                _selectedValue = Math.Clamp(1f - local.Y / Math.Max(1f, dimensions.Y - 1f), 0f, 1f);
                CommitSelection();
            }
        }

        private void CommitSelection()
        {
            SelectedColor = HSVToRGB(_selectedHue, _selectedSaturation, _selectedValue);
            OnColorSelected?.Invoke(SelectedColor);
        }

        public void SetSelectedColor(Color color)
        {
            SelectedColor = color;
            RGBToHSV(color, out _selectedHue, out _selectedSaturation, out _selectedValue);
            OnColorSelected?.Invoke(SelectedColor);
        }

        private static void RGBToHSV(Color color, out float hue, out float saturation, out float value)
        {
            float r = color.R / 255f;
            float g = color.G / 255f;
            float b = color.B / 255f;
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

        private static Color HSVToRGB(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;
            float r = 0, g = 0, b = 0;

            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }

            return new Color(r + m, g + m, b + m);
        }
    }
}
