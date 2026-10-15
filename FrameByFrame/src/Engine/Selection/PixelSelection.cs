using System;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Selection
{
    public sealed class PixelSelection
    {
        public Rectangle Bounds { get; }
        public Color[] Pixels { get; }
        public int Width => Bounds.Width;
        public int Height => Bounds.Height;

        public PixelSelection(Rectangle bounds, Color[] pixels)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(bounds));
            ArgumentNullException.ThrowIfNull(pixels);
            if (pixels.Length != bounds.Width * bounds.Height)
                throw new ArgumentException("Selection pixels do not match its bounds.", nameof(pixels));
            Bounds = bounds;
            Pixels = (Color[])pixels.Clone();
        }

        public PixelSelection Clone(Rectangle? bounds = null) => new(bounds ?? Bounds, Pixels);
    }
}
