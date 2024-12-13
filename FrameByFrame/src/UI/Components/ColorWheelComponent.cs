using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;

namespace FrameByFrame.src.Engine
{
    public class ColorWheelComponent : Overlay
    {
        public Action<Color> OnColorSelected;
        public Color SelectedColor { get; private set; }

        public ColorWheelComponent(Vector2 position, Vector2 dimensions): base((Texture2D)null, position, dimensions)
        {
            texture = GenerateColorWheel((int)dimensions.X);
            this.SetColorData();
            container = new Container(texture, position, dimensions);
            SelectedColor = Color.Black;
        }

        private Texture2D GenerateColorWheel(int diameter)
        {
            int radius = diameter / 2;
            Texture2D colorWheel = new Texture2D(GlobalParameters.GlobalGraphics, diameter, diameter);

            Color[] colors = new Color[diameter * diameter];
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    Vector2 position = new Vector2(x, y);
                    Vector2 offset = position - center;

                    float distance = offset.Length(); // Distance from center
                    if (distance > radius)
                    {
                        // Outside the circle, set transparent
                        colors[x + y * diameter] = Color.Transparent;
                        continue;
                    }

                    // Angle (hue) and normalized distance (saturation)
                    float angle = (float)Math.Atan2(offset.Y, offset.X);
                    float hue = MathHelper.ToDegrees(angle + MathHelper.TwoPi) % 360; // Hue (0-360)
                    float saturation = distance / radius; // Saturation (0-1)
                    float value = 1.0f; // Max brightness

                    // Convert HSV to RGB
                    colors[x + y * diameter] = HSVToRGB(hue, saturation, value);
                }
            }

            colorWheel.SetData(colors);
            return colorWheel;
        }

        private Color HSVToRGB(float h, float s, float v)
        {
            float c = v * s; // Chroma
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1)); // Secondary component
            float m = v - c;

            float r = 0, g = 0, b = 0;

            if (h >= 0 && h < 60) { r = c; g = x; b = 0; }
            else if (h >= 60 && h < 120) { r = x; g = c; b = 0; }
            else if (h >= 120 && h < 180) { r = 0; g = c; b = x; }
            else if (h >= 180 && h < 240) { r = 0; g = x; b = c; }
            else if (h >= 240 && h < 300) { r = x; g = 0; b = c; }
            else if (h >= 300 && h < 360) { r = c; g = 0; b = x; }

            return new Color(r + m, g + m, b + m);
        }

        public override void Update()
        {
            base.Update();

            // Handle mouse click to select a color
            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                Vector2 mousePosition = GlobalParameters.GlobalMouse.newMousePos;

                // Check if the click is within the bounds of the color wheel
                if (mousePosition.X >= position.X && mousePosition.X <= position.X + dimensions.X &&
                    mousePosition.Y >= position.Y && mousePosition.Y <= position.Y + dimensions.Y)
                {
                    // Map mouse position to texture coordinates
                    int relativeX = (int)((mousePosition.X - position.X) / dimensions.X * texture.Width);
                    int relativeY = (int)((mousePosition.Y - position.Y) / dimensions.Y * texture.Height);

                    // Get the color at the clicked position
                    if (relativeX >= 0 && relativeX < texture.Width &&
                        relativeY >= 0 && relativeY < texture.Height)
                    {
                        SelectedColor = colorData[relativeX, relativeY];
                        OnColorSelected?.Invoke(SelectedColor);
                        Debug.WriteLine(SelectedColor.ToString());

                        // Ignore transparent areas (outside the wheel)
                        if (SelectedColor.A == 0)
                            SelectedColor = Color.Transparent;
                    }
                }
            }
        }

        public override void Draw(Vector2 OFFSET)
        {
            base.Draw(OFFSET);

            // Optional: Visualize the selected color
            if (SelectedColor != Color.Transparent)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(
                    texture,
                    new Rectangle((int)(position.X + OFFSET.X + dimensions.X + 10), (int)(position.Y + OFFSET.Y), 50, 50),
                    SelectedColor
                );
            }
        }
    }
}
