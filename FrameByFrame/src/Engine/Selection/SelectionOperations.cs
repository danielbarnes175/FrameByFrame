using System;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Selection
{
    public static class SelectionOperations
    {
        public static PixelSelection Extract(Color[] source, int canvasWidth, int canvasHeight, Rectangle bounds)
        {
            ValidateCanvas(source, canvasWidth, canvasHeight);
            Rectangle clipped = Rectangle.Intersect(bounds, new Rectangle(0, 0, canvasWidth, canvasHeight));
            if (clipped.Width <= 0 || clipped.Height <= 0) return null;
            Color[] pixels = new Color[clipped.Width * clipped.Height];
            for (int y = 0; y < clipped.Height; y++)
            for (int x = 0; x < clipped.Width; x++)
                pixels[x + y * clipped.Width] = source[clipped.X + x + (clipped.Y + y) * canvasWidth];
            return new PixelSelection(clipped, pixels);
        }

        public static void Clear(Color[] destination, int canvasWidth, int canvasHeight, Rectangle bounds)
        {
            ValidateCanvas(destination, canvasWidth, canvasHeight);
            Rectangle clipped = Rectangle.Intersect(bounds, new Rectangle(0, 0, canvasWidth, canvasHeight));
            for (int y = clipped.Top; y < clipped.Bottom; y++)
            for (int x = clipped.Left; x < clipped.Right; x++)
                destination[x + y * canvasWidth] = Color.Transparent;
        }

        public static void Composite(Color[] destination, int canvasWidth, int canvasHeight,
            PixelSelection selection)
        {
            ValidateCanvas(destination, canvasWidth, canvasHeight);
            ArgumentNullException.ThrowIfNull(selection);
            for (int y = 0; y < selection.Height; y++)
            for (int x = 0; x < selection.Width; x++)
            {
                int targetX = selection.Bounds.X + x;
                int targetY = selection.Bounds.Y + y;
                if (targetX < 0 || targetX >= canvasWidth || targetY < 0 || targetY >= canvasHeight) continue;
                Color color = selection.Pixels[x + y * selection.Width];
                if (color != Color.Transparent) destination[targetX + targetY * canvasWidth] = color;
            }
        }

        public static PixelSelection Move(PixelSelection selection, int x, int y) =>
            selection.Clone(new Rectangle(x, y, selection.Width, selection.Height));

        public static PixelSelection Resize(PixelSelection selection, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(selection);
            width = Math.Max(1, width);
            height = Math.Max(1, height);
            Color[] resized = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int sourceX = Math.Min(selection.Width - 1, x * selection.Width / width);
                int sourceY = Math.Min(selection.Height - 1, y * selection.Height / height);
                resized[x + y * width] = selection.Pixels[sourceX + sourceY * selection.Width];
            }
            Point center = selection.Bounds.Center;
            return new PixelSelection(new Rectangle(center.X - width / 2, center.Y - height / 2, width, height), resized);
        }

        public static PixelSelection RotateClockwise(PixelSelection selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            int width = selection.Height;
            int height = selection.Width;
            Color[] rotated = new Color[width * height];
            for (int y = 0; y < selection.Height; y++)
            for (int x = 0; x < selection.Width; x++)
            {
                int targetX = selection.Height - 1 - y;
                int targetY = x;
                rotated[targetX + targetY * width] = selection.Pixels[x + y * selection.Width];
            }
            Point center = selection.Bounds.Center;
            return new PixelSelection(new Rectangle(center.X - width / 2, center.Y - height / 2, width, height), rotated);
        }

        private static void ValidateCanvas(Color[] pixels, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(pixels);
            if (width <= 0 || height <= 0 || pixels.Length != width * height)
                throw new ArgumentException("Pixel count does not match the canvas dimensions.", nameof(pixels));
        }
    }
}
