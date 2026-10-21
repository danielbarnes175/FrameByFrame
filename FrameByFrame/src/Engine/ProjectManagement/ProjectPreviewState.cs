using System;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Projects
{
    internal sealed class ProjectPreviewState : IDisposable
    {
        public Animation.Animation? Animation { get; private set; }
        public Frame? CurrentFrame => Animation?.CurrentFrame;

        public void Start(Animation.Animation animation)
        {
            ArgumentNullException.ThrowIfNull(animation);
            Clear();
            Animation = animation;
            animation.SelectFrame(0);
            animation.Start();
        }

        public void Update(GameTime gameTime)
        {
            if (Animation == null) return;
            int previousFrame = Animation.CurrentFrameIndex;
            Animation.Animate(gameTime);
            if (Animation.CurrentFrameIndex != previousFrame)
                Animation.GetFrameAtIndex(previousFrame)?.ReleasePreviewTexture();
        }

        public void Clear()
        {
            Animation?.Stop();
            Animation?.Dispose();
            Animation = null;
        }

        public void Dispose() => Clear();
    }
}
