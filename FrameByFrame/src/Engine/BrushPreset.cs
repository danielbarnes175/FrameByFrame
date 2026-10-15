using System;

namespace FrameByFrame.src.Engine
{
    /// <summary>A color-independent brush-tip stamp that can also receive imported brush masks.</summary>
    public sealed class BrushPreset
    {
        public const int DefaultTipSize = 16;
        public const int MaximumTipDimension = 64;
        public const int CurrentFormatVersion = 1;

        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Solid Pen";
        public int FormatVersion { get; set; } = CurrentFormatVersion;
        public int TipWidth { get; set; } = DefaultTipSize;
        public int TipHeight { get; set; } = DefaultTipSize;
        public byte[] TipAlpha { get; set; } = CreateSolidTip(DefaultTipSize, DefaultTipSize);

        public byte AlphaAt(int x, int y) =>
            x >= 0 && x < TipWidth && y >= 0 && y < TipHeight
                ? TipAlpha[x + y * TipWidth]
                : (byte)0;

        public bool SetAlpha(int x, int y, byte alpha)
        {
            if (x < 0 || x >= TipWidth || y < 0 || y >= TipHeight) return false;
            int index = x + y * TipWidth;
            if (TipAlpha[index] == alpha) return false;
            TipAlpha[index] = alpha;
            return true;
        }

        public void Fill(byte alpha) => Array.Fill(TipAlpha, alpha);

        public BrushPreset Clone(string name) => new()
        {
            Name = name,
            FormatVersion = CurrentFormatVersion,
            TipWidth = TipWidth,
            TipHeight = TipHeight,
            TipAlpha = (byte[])TipAlpha.Clone()
        };

        public void Normalize()
        {
            if (Id == Guid.Empty) Id = Guid.NewGuid();
            Name = string.IsNullOrWhiteSpace(Name) ? "Custom brush" : Name.Trim();
            FormatVersion = CurrentFormatVersion;
            TipWidth = Math.Clamp(TipWidth, 1, MaximumTipDimension);
            TipHeight = Math.Clamp(TipHeight, 1, MaximumTipDimension);
            if (TipAlpha == null || TipAlpha.Length != TipWidth * TipHeight)
                TipAlpha = CreateSolidTip(TipWidth, TipHeight);
        }

        public static BrushPreset CreateSolidPen() => new();

        public static BrushPreset CreatePencil() => new()
        {
            Name = "Pencil",
            TipAlpha = CreatePencilTip(DefaultTipSize)
        };

        public static BrushPreset CreateMarker() => new()
        {
            Name = "Marker",
            TipAlpha = CreateMarkerTip(DefaultTipSize)
        };

        public static BrushPreset CreateOil() => new()
        {
            Name = "Oil",
            TipAlpha = CreateOilTip(DefaultTipSize)
        };

        public static BrushPreset CreateDither() => CreateCheckerDither();

        public static BrushPreset CreateCheckerDither() => new()
        {
            Name = "Checker 50%",
            TipAlpha = CreatePatternTip(DefaultTipSize, (x, y) => (x + y) % 2 == 0)
        };

        public static BrushPreset CreateSparseDither() => new()
        {
            Name = "Sparse 25%",
            TipAlpha = CreatePatternTip(DefaultTipSize, (x, y) => x % 2 == 0 && y % 2 == 0)
        };

        public static BrushPreset CreateHorizontalTone() => new()
        {
            Name = "Horizontal Tone",
            TipAlpha = CreatePatternTip(DefaultTipSize, (_, y) => y % 3 != 2)
        };

        public static BrushPreset CreateDiagonalHatch() => new()
        {
            Name = "Diagonal Hatch",
            TipAlpha = CreatePatternTip(DefaultTipSize, (x, y) => (x + y) % 4 == 0)
        };

        public static BrushPreset[] CreateBuiltIns() => new[]
        {
            CreateSolidPen(), CreatePencil(), CreateMarker(), CreateOil(),
            CreateCheckerDither(), CreateSparseDither(), CreateHorizontalTone(), CreateDiagonalHatch()
        };

        private static byte[] CreateSolidTip(int width, int height)
        {
            byte[] alpha = new byte[width * height];
            float centerX = (width - 1) / 2f;
            float centerY = (height - 1) / 2f;
            float radiusX = Math.Max(.5f, width / 2f);
            float radiusY = Math.Max(.5f, height / 2f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (MathF.Pow((x - centerX) / radiusX, 2) + MathF.Pow((y - centerY) / radiusY, 2) <= 1f)
                    alpha[x + y * width] = byte.MaxValue;
            return alpha;
        }

        private static byte[] CreatePencilTip(int size) => CreatePencilTip(size, size);

        private static byte[] CreatePencilTip(int width, int height)
        {
            byte[] alpha = new byte[width * height];
            float centerX = (width - 1) / 2f;
            float centerY = (height - 1) / 2f;
            float radiusX = Math.Max(.5f, width / 2f);
            float radiusY = Math.Max(.5f, height / 2f);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float distance = MathF.Sqrt(MathF.Pow((x - centerX) / radiusX, 2) +
                    MathF.Pow((y - centerY) / radiusY, 2));
                if (distance >= 1f) continue;
                int grain = (x * 29 + y * 47 + x * y * 7) % 9;
                float edge = Math.Clamp((1f - distance) / .24f, 0f, 1f);
                alpha[x + y * width] = (byte)Math.Round(edge * (grain < 2 ? 90 : grain < 5 ? 165 : 225));
            }
            return alpha;
        }

        private static byte[] CreateMarkerTip(int size)
        {
            byte[] alpha = new byte[size * size];
            for (int y = 2; y < size - 2; y++)
            for (int x = 1; x < size - 1; x++)
                alpha[x + y * size] = 185;
            return alpha;
        }

        private static byte[] CreateOilTip(int size)
        {
            byte[] alpha = new byte[size * size];
            float center = (size - 1) / 2f;
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = MathF.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                if (distance > radius) continue;
                int bristle = (x * 37 + y * 11) % 9;
                alpha[x + y * size] = bristle switch
                {
                    0 or 1 => (byte)40,
                    2 or 3 => (byte)110,
                    4 or 5 or 6 => (byte)190,
                    _ => byte.MaxValue
                };
            }
            return alpha;
        }

        private static byte[] CreatePatternTip(int size, Func<int, int, bool> contains)
        {
            byte[] alpha = new byte[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (contains(x, y)) alpha[x + y * size] = byte.MaxValue;
            return alpha;
        }
    }
}
