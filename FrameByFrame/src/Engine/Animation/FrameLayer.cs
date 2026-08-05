using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Animation
{
    public sealed class FrameLayer : IDisposable
    {
        public AnimationLayer Definition { get; }
        public Dictionary<int, Color> Pixels { get; } = new();
        internal Texture2D Texture { get; set; }

        public Guid Id => Definition.Id;

        public FrameLayer(AnimationLayer definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public void Dispose()
        {
            Texture?.Dispose();
            Texture = null;
            Pixels.Clear();
        }
    }
}
