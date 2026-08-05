using System;

namespace FrameByFrame.src.Engine.Animation
{
    public sealed class AnimationLayer
    {
        public Guid Id { get; }
        public string Name { get; private set; }
        public bool IsVisible { get; set; }
        public bool IsLocked { get; set; }

        public AnimationLayer(string name, Guid? id = null, bool isVisible = true, bool isLocked = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A layer name is required.", nameof(name));

            Id = id ?? Guid.NewGuid();
            if (Id == Guid.Empty) throw new ArgumentException("A layer ID cannot be empty.", nameof(id));
            Name = name.Trim();
            IsVisible = isVisible;
            IsLocked = isLocked;
        }

        internal void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A layer name is required.", nameof(name));
            Name = name.Trim();
        }
    }
}
