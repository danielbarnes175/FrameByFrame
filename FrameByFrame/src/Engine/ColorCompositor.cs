using System;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine
{
    internal static class ColorCompositor
    {
        /// <summary>Composites straight-alpha source over straight-alpha destination.</summary>
        public static Color SourceOver(Color source, Color destination)
        {
            if (source.A == 0) return destination;
            if (source.A == byte.MaxValue) return source;

            long inverseSourceAlpha = byte.MaxValue - source.A;
            long alphaNumerator = source.A * byte.MaxValue + destination.A * inverseSourceAlpha;
            if (alphaNumerator == 0) return Color.Transparent;

            byte alpha = (byte)((alphaNumerator + 127) / byte.MaxValue);
            byte red = Channel(source.R, source.A, destination.R, destination.A,
                inverseSourceAlpha, alphaNumerator);
            byte green = Channel(source.G, source.A, destination.G, destination.A,
                inverseSourceAlpha, alphaNumerator);
            byte blue = Channel(source.B, source.A, destination.B, destination.A,
                inverseSourceAlpha, alphaNumerator);
            return new Color(red, green, blue, alpha);
        }

        public static Color Premultiply(Color straight)
        {
            if (straight.A == byte.MaxValue) return straight;
            if (straight.A == 0) return Color.Transparent;
            return new Color(
                (byte)((straight.R * straight.A + 127) / byte.MaxValue),
                (byte)((straight.G * straight.A + 127) / byte.MaxValue),
                (byte)((straight.B * straight.A + 127) / byte.MaxValue),
                straight.A);
        }

        public static void SourceOver(byte[] rgba, int pixelIndex, Color source)
        {
            ArgumentNullException.ThrowIfNull(rgba);
            int offset = checked(pixelIndex * 4);
            if (offset < 0 || offset > rgba.Length - 4)
                throw new ArgumentOutOfRangeException(nameof(pixelIndex));
            Color destination = new(rgba[offset], rgba[offset + 1], rgba[offset + 2], rgba[offset + 3]);
            Color result = SourceOver(source, destination);
            rgba[offset] = result.R;
            rgba[offset + 1] = result.G;
            rgba[offset + 2] = result.B;
            rgba[offset + 3] = result.A;
        }

        private static byte Channel(byte source, byte sourceAlpha, byte destination,
            byte destinationAlpha, long inverseSourceAlpha, long alphaNumerator)
        {
            long numerator = source * (long)sourceAlpha * byte.MaxValue +
                destination * (long)destinationAlpha * inverseSourceAlpha;
            return (byte)Math.Clamp((numerator + alphaNumerator / 2) / alphaNumerator, 0, byte.MaxValue);
        }
    }
}
