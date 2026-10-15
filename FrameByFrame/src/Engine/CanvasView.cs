using System;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine
{
    /// <summary>Editor-only transform for presenting a canvas without changing project pixels.</summary>
    public sealed class CanvasView
    {
        public const float MinimumZoom = .25f;
        public const float MaximumZoom = 4f;
        public const float ZoomStep = .25f;

        private Rectangle _viewport;
        private int _canvasWidth = 1;
        private int _canvasHeight = 1;

        public float Zoom { get; private set; } = 1f;
        public float RotationDegrees { get; private set; }
        public float RotationRadians => MathHelper.ToRadians(RotationDegrees);
        public Vector2 PanOffset { get; private set; }
        public Vector2 Center => _viewport.Center.ToVector2() + PanOffset;
        public float Scale { get; private set; } = 1f;
        public int CanvasWidth => _canvasWidth;
        public int CanvasHeight => _canvasHeight;

        public void Arrange(Rectangle viewport, int canvasWidth, int canvasHeight)
        {
            _viewport = viewport;
            _canvasWidth = Math.Max(1, canvasWidth);
            _canvasHeight = Math.Max(1, canvasHeight);
            float fitScale = Math.Min(
                Math.Max(1, viewport.Width) / (float)_canvasWidth,
                Math.Max(1, viewport.Height) / (float)_canvasHeight);
            Scale = Math.Max(.0001f, fitScale * Zoom);
        }

        public void ZoomAt(Vector2 screenPoint, float amount)
        {
            Vector2 canvasPoint = ScreenToCanvas(screenPoint);
            SetZoom(Zoom + amount);
            PanOffset += screenPoint - CanvasToScreen(canvasPoint);
        }

        public void SetZoom(float zoom)
        {
            Zoom = Math.Clamp(zoom, MinimumZoom, MaximumZoom);
            Arrange(_viewport, _canvasWidth, _canvasHeight);
        }

        public void RotateBy(float degrees) => RotationDegrees = NormalizeDegrees(RotationDegrees + degrees);

        public void PanBy(Vector2 screenDelta) => PanOffset += screenDelta;

        public void Reset()
        {
            Zoom = 1f;
            RotationDegrees = 0f;
            PanOffset = Vector2.Zero;
            Arrange(_viewport, _canvasWidth, _canvasHeight);
        }

        public Vector2 CanvasToScreen(Vector2 canvasPoint)
        {
            Vector2 local = (canvasPoint - new Vector2(_canvasWidth / 2f, _canvasHeight / 2f)) * Scale;
            float cos = MathF.Cos(RotationRadians);
            float sin = MathF.Sin(RotationRadians);
            return Center + new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos);
        }

        public Vector2 ScreenToCanvas(Vector2 screenPoint)
        {
            Vector2 local = screenPoint - Center;
            float cos = MathF.Cos(-RotationRadians);
            float sin = MathF.Sin(-RotationRadians);
            Vector2 unrotated = new(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos);
            return unrotated / Scale + new Vector2(_canvasWidth / 2f, _canvasHeight / 2f);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint)
        {
            Vector2 canvas = ScreenToCanvas(screenPoint);
            return canvas.X >= 0 && canvas.X < _canvasWidth && canvas.Y >= 0 && canvas.Y < _canvasHeight;
        }

        private static float NormalizeDegrees(float degrees)
        {
            degrees %= 360f;
            if (degrees > 180f) degrees -= 360f;
            if (degrees <= -180f) degrees += 360f;
            return degrees;
        }
    }
}
