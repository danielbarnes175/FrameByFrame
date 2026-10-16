using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine
{
    internal static class ProjectModelValidator
    {
        internal static string ProjectName(string? name, string parameterName)
        {
            string normalized = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized) || normalized is "." or ".." ||
                Encoding.UTF8.GetByteCount(normalized) > ProjectLimits.MaxNameBytes ||
                Path.IsPathRooted(normalized) ||
                normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                normalized.IndexOfAny(['<', '>', ':', '"', '/', '\\', '|', '?', '*']) >= 0)
                throw new ArgumentException("Project names must contain valid filename characters.", parameterName);
            return normalized;
        }

        internal static string LayerName(string? name, string parameterName)
        {
            string normalized = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized) ||
                Encoding.UTF8.GetByteCount(normalized) > ProjectLimits.MaxNameBytes)
                throw new ArgumentException($"Layer names must be between 1 and {ProjectLimits.MaxNameBytes:N0} UTF-8 bytes.", parameterName);
            return normalized;
        }

        internal static Vector2 CanvasSize(Vector2 value, string parameterName)
        {
            if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) ||
                value.X < Animation.Animation.MinCanvasDimension ||
                value.Y < Animation.Animation.MinCanvasDimension ||
                value.X > Animation.Animation.MaxCanvasDimension ||
                value.Y > Animation.Animation.MaxCanvasDimension)
                throw new ArgumentOutOfRangeException(parameterName,
                    $"Canvas dimensions must be between {Animation.Animation.MinCanvasDimension} and {Animation.Animation.MaxCanvasDimension}.");
            return value;
        }

        internal static Vector2 Position(Vector2 value, string parameterName)
        {
            if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
                throw new ArgumentOutOfRangeException(parameterName, "Positions must contain finite coordinates.");
            return value;
        }
    }
}
