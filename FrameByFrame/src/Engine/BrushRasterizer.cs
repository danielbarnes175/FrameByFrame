using System;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine
{
    public static class BrushRasterizer
    {
        public static bool Contains(BrushShape shape, int x, int y, int radius) =>
            shape == BrushShape.Square || x * x + y * y <= radius * radius;

        public static Color Apply(Color existing, Color paint, int opacityPercent, bool erase)
        {
            float strength = Math.Clamp(opacityPercent, 1, 100) / 100f;
            if (erase)
            {
                byte alpha = (byte)Math.Round(existing.A * (1f - strength));
                return alpha == 0 ? Color.Transparent : new Color(existing.R, existing.G, existing.B, alpha);
            }

            float sourceAlpha = paint.A / 255f * strength;
            float destinationAlpha = existing.A / 255f;
            float outputAlpha = sourceAlpha + destinationAlpha * (1f - sourceAlpha);
            if (outputAlpha <= 0f) return Color.Transparent;
            byte Blend(byte source, byte destination) => (byte)Math.Clamp((int)Math.Round(
                (source * sourceAlpha + destination * destinationAlpha * (1f - sourceAlpha)) / outputAlpha), 0, 255);
            return new Color(Blend(paint.R, existing.R), Blend(paint.G, existing.G),
                Blend(paint.B, existing.B), (byte)Math.Clamp((int)Math.Round(outputAlpha * 255f), 0, 255));
        }
    }
}
