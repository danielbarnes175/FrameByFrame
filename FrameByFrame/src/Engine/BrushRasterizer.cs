using System;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine
{
    public static class BrushRasterizer
    {
        public static byte Sample(BrushPreset brush, int x, int y, int radius,
            int canvasX, int canvasY)
        {
            ArgumentNullException.ThrowIfNull(brush);
            radius = Math.Max(1, radius);
            if (x < -radius || x > radius || y < -radius || y > radius) return 0;
            int diameter = radius * 2 + 1;
            if (brush.TileTip)
            {
                if (x * x + y * y > radius * radius) return 0;
                int patternX = ((canvasX % diameter) + diameter) % diameter;
                int patternY = ((canvasY % diameter) + diameter) % diameter;
                int tiledTipX = Math.Clamp(patternX * brush.TipWidth / diameter, 0, brush.TipWidth - 1);
                int tiledTipY = Math.Clamp(patternY * brush.TipHeight / diameter, 0, brush.TipHeight - 1);
                return brush.AlphaAt(tiledTipX, tiledTipY);
            }
            int tipX = Math.Clamp((x + radius) * brush.TipWidth / diameter, 0, brush.TipWidth - 1);
            int tipY = Math.Clamp((y + radius) * brush.TipHeight / diameter, 0, brush.TipHeight - 1);
            return brush.AlphaAt(tipX, tipY);
        }

        public static Color Apply(Color existing, Color paint, byte tipAlpha, bool erase)
        {
            float strength = tipAlpha / 255f;
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
