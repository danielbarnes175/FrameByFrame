using System;

namespace FrameByFrame.src.Engine
{
    public enum BrushShape
    {
        Round,
        Square
    }

    public sealed class BrushPreset
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Brush 1";
        public BrushShape Shape { get; set; } = BrushShape.Round;
        public int Size { get; set; } = 5;
        public int OpacityPercent { get; set; } = 100;
        public int SpacingPercent { get; set; } = 25;

        public BrushPreset Clone(string name) => new()
        {
            Name = name,
            Shape = Shape,
            Size = Size,
            OpacityPercent = OpacityPercent,
            SpacingPercent = SpacingPercent
        };

        public void Normalize()
        {
            if (Id == Guid.Empty) Id = Guid.NewGuid();
            Name = string.IsNullOrWhiteSpace(Name) ? "Custom brush" : Name.Trim();
            Size = Math.Clamp(Size, UIConstants.MIN_BRUSH_SIZE, UIConstants.MAX_BRUSH_SIZE);
            OpacityPercent = Math.Clamp(OpacityPercent, 1, 100);
            SpacingPercent = Math.Clamp(SpacingPercent, 5, 200);
            if (!Enum.IsDefined(Shape)) Shape = BrushShape.Round;
        }
    }
}
