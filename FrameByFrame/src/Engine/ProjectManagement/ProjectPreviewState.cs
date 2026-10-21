using System;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Audio;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Projects
{
    internal sealed class ProjectPreviewState : IDisposable
    {
        private AudioPlaybackController? _audioPlayback;
        public Animation.Animation? Animation { get; private set; }
        public Frame? CurrentFrame => Animation?.CurrentFrame;
        internal bool HasAudioPlayback => _audioPlayback != null;

        public void Start(Animation.Animation animation)
        {
            ArgumentNullException.ThrowIfNull(animation);
            Clear();
            Animation = animation;
            animation.SelectFrame(0);
            animation.Start();
            _audioPlayback = new AudioPlaybackController(animation);
        }

        public void Update(GameTime gameTime)
        {
            if (Animation == null) return;
            int previousFrame = Animation.CurrentFrameIndex;
            Animation.Animate(gameTime);
            if (Animation.CurrentFrameIndex != previousFrame)
                Animation.GetFrameAtIndex(previousFrame)?.ReleasePreviewTexture();
            _audioPlayback?.Update();
        }

        public void Clear()
        {
            _audioPlayback?.Dispose();
            _audioPlayback = null;
            Animation?.Stop();
            Animation?.Dispose();
            Animation = null;
        }

        public void Dispose() => Clear();
    }
}
