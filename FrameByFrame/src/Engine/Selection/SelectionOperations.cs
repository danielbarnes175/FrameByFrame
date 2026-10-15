using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Selection
{
    public static class SelectionOperations
    {
        public static PixelSelection ExtractFreeform(Color[] source, int canvasWidth, int canvasHeight,
            IReadOnlyList<Vector2> polygon)
        {
            ValidateCanvas(source, canvasWidth, canvasHeight);
            ArgumentNullException.ThrowIfNull(polygon);
            if (polygon.Count < 3) return null;

            float minX = polygon[0].X;
            float minY = polygon[0].Y;
            float maxX = polygon[0].X;
            float maxY = polygon[0].Y;
            for (int i = 1; i < polygon.Count; i++)
            {
                minX = Math.Min(minX, polygon[i].X);
                minY = Math.Min(minY, polygon[i].Y);
                maxX = Math.Max(maxX, polygon[i].X);
                maxY = Math.Max(maxY, polygon[i].Y);
            }

            int left = (int)Math.Floor(minX);
            int top = (int)Math.Floor(minY);
            Rectangle bounds = Rectangle.Intersect(
                new Rectangle(left, top, Math.Max(1, (int)Math.Ceiling(maxX) - left),
                    Math.Max(1, (int)Math.Ceiling(maxY) - top)),
                new Rectangle(0, 0, canvasWidth, canvasHeight));
            if (bounds.Width <= 0 || bounds.Height <= 0) return null;

            Color[] pixels = new Color[bounds.Width * bounds.Height];
            bool[] mask = new bool[pixels.Length];
            bool hasSelectedPixel = false;
            for (int y = 0; y < bounds.Height; y++)
            for (int x = 0; x < bounds.Width; x++)
            {
                int index = x + y * bounds.Width;
                Vector2 pixelCenter = new(bounds.X + x + .5f, bounds.Y + y + .5f);
                if (!ContainsPoint(polygon, pixelCenter)) continue;
                mask[index] = true;
                pixels[index] = source[bounds.X + x + (bounds.Y + y) * canvasWidth];
                hasSelectedPixel = true;
            }
            return hasSelectedPixel ? new PixelSelection(bounds, pixels, mask) : null;
        }

        public static void Clear(Color[] destination, int canvasWidth, int canvasHeight, PixelSelection selection)
        {
            ValidateCanvas(destination, canvasWidth, canvasHeight);
            ArgumentNullException.ThrowIfNull(selection);
            for (int y = 0; y < selection.Height; y++)
            for (int x = 0; x < selection.Width; x++)
            {
                if (!selection.Mask[x + y * selection.Width]) continue;
                int targetX = selection.Bounds.X + x;
                int targetY = selection.Bounds.Y + y;
                if (targetX >= 0 && targetX < canvasWidth && targetY >= 0 && targetY < canvasHeight)
                    destination[targetX + targetY * canvasWidth] = Color.Transparent;
            }
        }

        public static void Composite(Color[] destination, int canvasWidth, int canvasHeight,
            PixelSelection selection)
        {
            ValidateCanvas(destination, canvasWidth, canvasHeight);
            ArgumentNullException.ThrowIfNull(selection);
            for (int y = 0; y < selection.Height; y++)
            for (int x = 0; x < selection.Width; x++)
            {
                int sourceIndex = x + y * selection.Width;
                if (!selection.Mask[sourceIndex]) continue;
                int targetX = selection.Bounds.X + x;
                int targetY = selection.Bounds.Y + y;
                if (targetX < 0 || targetX >= canvasWidth || targetY < 0 || targetY >= canvasHeight) continue;
                Color color = selection.Pixels[sourceIndex];
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
            bool[] mask = new bool[resized.Length];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int sourceX = Math.Min(selection.Width - 1, x * selection.Width / width);
                int sourceY = Math.Min(selection.Height - 1, y * selection.Height / height);
                int sourceIndex = sourceX + sourceY * selection.Width;
                int targetIndex = x + y * width;
                resized[targetIndex] = selection.Pixels[sourceIndex];
                mask[targetIndex] = selection.Mask[sourceIndex];
            }
            Point center = selection.Bounds.Center;
            return new PixelSelection(new Rectangle(center.X - width / 2, center.Y - height / 2, width, height),
                resized, mask);
        }

        public static PixelSelection RotateClockwise(PixelSelection selection)
        {
            ArgumentNullException.ThrowIfNull(selection);
            int width = selection.Height;
            int height = selection.Width;
            Color[] rotated = new Color[width * height];
            bool[] mask = new bool[rotated.Length];
            for (int y = 0; y < selection.Height; y++)
            for (int x = 0; x < selection.Width; x++)
            {
                int targetX = selection.Height - 1 - y;
                int targetY = x;
                int sourceIndex = x + y * selection.Width;
                int targetIndex = targetX + targetY * width;
                rotated[targetIndex] = selection.Pixels[sourceIndex];
                mask[targetIndex] = selection.Mask[sourceIndex];
            }
            Point center = selection.Bounds.Center;
            return new PixelSelection(new Rectangle(center.X - width / 2, center.Y - height / 2, width, height),
                rotated, mask);
        }

        private static bool ContainsPoint(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 first = polygon[j];
                Vector2 second = polygon[i];
                if (PointOnSegment(point, first, second)) return true;
                if ((second.Y > point.Y) != (first.Y > point.Y) &&
                    point.X < (first.X - second.X) * (point.Y - second.Y) / (first.Y - second.Y) + second.X)
                    inside = !inside;
            }
            return inside;
        }

        private static bool PointOnSegment(Vector2 point, Vector2 first, Vector2 second)
        {
            float cross = (point.Y - first.Y) * (second.X - first.X) -
                (point.X - first.X) * (second.Y - first.Y);
            if (Math.Abs(cross) > .0001f) return false;
            return Vector2.Dot(point - first, point - second) <= .0001f;
        }

        private static void ValidateCanvas(Color[] pixels, int width, int height)
        {
            ArgumentNullException.ThrowIfNull(pixels);
            if (width <= 0 || height <= 0 || pixels.Length != width * height)
                throw new ArgumentException("Pixel count does not match the canvas dimensions.", nameof(pixels));
        }
    }
}
