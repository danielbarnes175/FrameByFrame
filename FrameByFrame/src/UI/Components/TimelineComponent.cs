using System;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.UI.Components
{
    public sealed class TimelineComponent : UIElement
    {
        private const int NavigationWidth = 44;
        private const int Gap = 8;
        private const int ThumbnailWidth = 112;
        private readonly Animation _animation;
        private readonly UIActionButton _previousPage;
        private readonly UIActionButton _nextPage;
        private int _firstVisibleFrame;

        public TimelineComponent(Animation animation)
        {
            _animation = animation;
            _previousPage = new UIActionButton("<", () => Scroll(-1));
            _nextPage = new UIActionButton(">", () => Scroll(1));
        }

        private int VisibleFrameCount => Math.Max(1,
            (Bounds.Width - NavigationWidth * 2 - Gap * 4) / (ThumbnailWidth + Gap));

        public override void Arrange(Rectangle bounds)
        {
            base.Arrange(bounds);
            int buttonY = bounds.Y + (bounds.Height - 44) / 2;
            _previousPage.Arrange(new Rectangle(bounds.X + Gap, buttonY, NavigationWidth, 44));
            _nextPage.Arrange(new Rectangle(bounds.Right - Gap - NavigationWidth, buttonY, NavigationWidth, 44));
            EnsureSelectionVisible();
        }

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            _previousPage.IsEnabled = _firstVisibleFrame > 0;
            _nextPage.IsEnabled = _firstVisibleFrame + VisibleFrameCount < _animation.TotalFrames;
            _previousPage.Update();
            _nextPage.Update();

            int startX = _previousPage.Bounds.Right + Gap;
            for (int slot = 0; slot < VisibleFrameCount; slot++)
            {
                int frameIndex = _firstVisibleFrame + slot;
                if (frameIndex >= _animation.TotalFrames) break;
                Rectangle thumbnail = ThumbnailBounds(startX, slot);
                if (UIPointerRouter.Clicked(thumbnail)) _animation.SelectFrame(frameIndex);
            }
            EnsureSelectionVisible();
        }

        public override void Draw()
        {
            UIRenderer.Fill(Bounds, UITheme.Surface);
            UIRenderer.Fill(new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 3), UITheme.Primary);
            _previousPage.Draw();
            _nextPage.Draw();

            int startX = _previousPage.Bounds.Right + Gap;
            for (int slot = 0; slot < VisibleFrameCount; slot++)
            {
                int frameIndex = _firstVisibleFrame + slot;
                if (frameIndex >= _animation.TotalFrames) break;
                Rectangle thumbnail = ThumbnailBounds(startX, slot);
                Rectangle preview = new(thumbnail.X + 4, thumbnail.Y + 4, thumbnail.Width - 8, thumbnail.Height - 28);
                _animation.GetFrameAtIndex(frameIndex)?.DrawPreview(preview, 1f);
                bool selected = frameIndex == _animation.CurrentFrameIndex;
                UIRenderer.Border(thumbnail, selected ? UITheme.Primary : UITheme.Border, selected ? 3 : 1);
                new UITextContainer { Bounds = new Rectangle(thumbnail.X, thumbnail.Bottom - 24, thumbnail.Width, 22), MaxLines = 1 }
                    .Draw((frameIndex + 1).ToString(), selected ? UITheme.Primary : UITheme.TextMuted, .65f);
            }
        }

        private Rectangle ThumbnailBounds(int startX, int slot) => new(
            startX + slot * (ThumbnailWidth + Gap), Bounds.Y + 10, ThumbnailWidth, Bounds.Height - 20);

        private void Scroll(int direction)
        {
            int page = VisibleFrameCount;
            _firstVisibleFrame = Math.Clamp(_firstVisibleFrame + direction * page, 0,
                Math.Max(0, _animation.TotalFrames - page));
        }

        private void EnsureSelectionVisible()
        {
            int count = VisibleFrameCount;
            if (_animation.CurrentFrameIndex < _firstVisibleFrame)
                _firstVisibleFrame = _animation.CurrentFrameIndex;
            else if (_animation.CurrentFrameIndex >= _firstVisibleFrame + count)
                _firstVisibleFrame = _animation.CurrentFrameIndex - count + 1;
            _firstVisibleFrame = Math.Clamp(_firstVisibleFrame, 0, Math.Max(0, _animation.TotalFrames - count));
        }
    }
}
