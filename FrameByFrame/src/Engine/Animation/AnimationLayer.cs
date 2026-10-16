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
            Id = id ?? Guid.NewGuid();
            if (Id == Guid.Empty) throw new ArgumentException("A layer ID cannot be empty.", nameof(id));
            Name = ProjectModelValidator.LayerName(name, nameof(name));
            IsVisible = isVisible;
            IsLocked = isLocked;
        }

        internal void Rename(string name)
        {
            Name = ProjectModelValidator.LayerName(name, nameof(name));
        }
    }
}
