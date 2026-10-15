using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Selection
{
    public sealed class PixelSelection
    {
        public Rectangle Bounds { get; }
        public Color[] Pixels { get; }
        public bool[] Mask { get; }
        public IReadOnlyList<(Vector2 Start, Vector2 End)> Boundary { get; }
        public int Width => Bounds.Width;
        public int Height => Bounds.Height;

        public PixelSelection(Rectangle bounds, Color[] pixels, bool[]? mask = null)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(bounds));
            ArgumentNullException.ThrowIfNull(pixels);
            if (pixels.Length != bounds.Width * bounds.Height)
                throw new ArgumentException("Selection pixels do not match its bounds.", nameof(pixels));
            if (mask != null && mask.Length != pixels.Length)
                throw new ArgumentException("Selection mask does not match its bounds.", nameof(mask));

            Bounds = bounds;
            Pixels = (Color[])pixels.Clone();
            Mask = mask == null ? CreateFilledMask(pixels.Length) : (bool[])mask.Clone();
            Boundary = CreateBoundary(Mask, bounds.Width, bounds.Height);
        }

        public bool Contains(Vector2 canvasPoint)
        {
            int x = (int)Math.Floor(canvasPoint.X) - Bounds.X;
            int y = (int)Math.Floor(canvasPoint.Y) - Bounds.Y;
            return x >= 0 && x < Width && y >= 0 && y < Height && Mask[x + y * Width];
        }

        public PixelSelection Clone(Rectangle? bounds = null) => new(bounds ?? Bounds, Pixels, Mask);

        private static bool[] CreateFilledMask(int length)
        {
            bool[] mask = new bool[length];
            Array.Fill(mask, true);
            return mask;
        }

        private static IReadOnlyList<(Vector2 Start, Vector2 End)> CreateBoundary(bool[] mask, int width, int height)
        {
            var boundary = new List<(Vector2, Vector2)>();
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!mask[x + y * width]) continue;
                if (y == 0 || !mask[x + (y - 1) * width])
                    boundary.Add((new Vector2(x, y), new Vector2(x + 1, y)));
                if (x == width - 1 || !mask[x + 1 + y * width])
                    boundary.Add((new Vector2(x + 1, y), new Vector2(x + 1, y + 1)));
                if (y == height - 1 || !mask[x + (y + 1) * width])
                    boundary.Add((new Vector2(x + 1, y + 1), new Vector2(x, y + 1)));
                if (x == 0 || !mask[x - 1 + y * width])
                    boundary.Add((new Vector2(x, y + 1), new Vector2(x, y)));
            }
            return boundary;
        }
    }
}
