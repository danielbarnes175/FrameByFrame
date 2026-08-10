using System;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FrameByFrame.src.UI.Components
{
    public sealed class TimelineComponent : UIElement
    {
        private const int NavigationWidth = 44;
        private const int Gap = 8;
        private const int ToolbarHeight = 58;
        private const int ActionSize = 44;
        private const int DragThreshold = 5;
        private int ThumbnailWidth => Math.Clamp(Bounds.Width / 4, 64, 112);
        private readonly Animation _animation;
        private readonly UIActionButton _previousPage;
        private readonly UIActionButton _nextPage;
        private readonly UIActionButton _duplicate;
        private readonly UIActionButton _insert;
        private readonly UIActionButton _delete;
        private readonly UIActionButton _copy;
        private readonly UIActionButton _paste;
        private int _firstVisibleFrame;
        private int _dragSource = -1;
        private int _dropTarget = -1;
        private Vector2 _dragStart;
        private bool _isDragging;

        public TimelineComponent(Animation animation)
        {
            _animation = animation;
            _previousPage = new UIActionButton("<", () => Scroll(-1));
            _nextPage = new UIActionButton(">", () => Scroll(1));
            _duplicate = new UIActionButton("D", _animation.DuplicateCurrentFrame)
            {
                Tooltip = "Duplicate selected frame - Ctrl+D"
            };
            _insert = new UIActionButton("+", _animation.InsertFrame)
            {
                Tooltip = "Insert a blank frame - B"
            };
            _delete = new UIActionButton("X", _animation.DeleteFrame)
            {
                Tooltip = "Delete selected frame - Delete"
            };
            _copy = new UIActionButton("C", _animation.CopyCurrentFrame)
            {
                Tooltip = "Copy selected frame - Ctrl+C"
            };
            _paste = new UIActionButton("P", () => _animation.PasteFrame())
            {
                Tooltip = "Paste frame after selection - Ctrl+V"
            };
        }

        private int VisibleFrameCount => Math.Max(1,
            (Bounds.Width - NavigationWidth * 2 - Gap * 4) / (ThumbnailWidth + Gap));

        public override void Arrange(Rectangle bounds)
        {
            base.Arrange(bounds);
            int buttonY = bounds.Y + ToolbarHeight +
                Math.Max(0, (bounds.Height - ToolbarHeight - NavigationWidth) / 2);
            _previousPage.Arrange(new Rectangle(bounds.X + Gap, buttonY, NavigationWidth, 44));
            _nextPage.Arrange(new Rectangle(bounds.Right - Gap - NavigationWidth, buttonY, NavigationWidth, 44));
            int actionY = bounds.Y + (ToolbarHeight - ActionSize) / 2;
            _duplicate.Arrange(new Rectangle(bounds.X + Gap, actionY, ActionSize, ActionSize));
            _insert.Arrange(new Rectangle(_duplicate.Bounds.Right + Gap, actionY, ActionSize, ActionSize));
            _delete.Arrange(new Rectangle(_insert.Bounds.Right + Gap, actionY, ActionSize, ActionSize));
            _copy.Arrange(new Rectangle(_delete.Bounds.Right + Gap, actionY, ActionSize, ActionSize));
            _paste.Arrange(new Rectangle(_copy.Bounds.Right + Gap, actionY, ActionSize, ActionSize));
            EnsureSelectionVisible();
        }

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            _previousPage.IsEnabled = _firstVisibleFrame > 0;
            _nextPage.IsEnabled = _firstVisibleFrame + VisibleFrameCount < _animation.TotalFrames;
            _delete.IsEnabled = _animation.TotalFrames > 1;
            _previousPage.Update();
            _nextPage.Update();
            _duplicate.Update();
            _insert.Update();
            _delete.Update();
            _copy.Update();
            _paste.Update();

            int startX = _previousPage.Bounds.Right + Gap;
            for (int slot = 0; slot < VisibleFrameCount; slot++)
            {
                int frameIndex = _firstVisibleFrame + slot;
                if (frameIndex >= _animation.TotalFrames) break;
                Rectangle thumbnail = ThumbnailBounds(startX, slot);
                UpdateThumbnailDrag(thumbnail, frameIndex);
            }
            FinishDragIfReleased();
            EnsureSelectionVisible();
        }

        public override void Draw()
        {
            UIRenderer.Fill(Bounds, UITheme.Surface);
            UIRenderer.Fill(new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 3), UITheme.Primary);
            UIRenderer.Fill(new Rectangle(Bounds.X, Bounds.Y + 3, Bounds.Width, ToolbarHeight - 3), UITheme.SurfaceRaised);
            UIRenderer.Fill(new Rectangle(Bounds.X, Bounds.Y + ToolbarHeight - 1, Bounds.Width, 1), UITheme.Border);
            _previousPage.Draw();
            _nextPage.Draw();
            _duplicate.Draw(drawTooltip: false);
            _insert.Draw(drawTooltip: false);
            _delete.Draw(drawTooltip: false);
            _copy.Draw(drawTooltip: false);
            _paste.Draw(drawTooltip: false);

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
                if (_isDragging && frameIndex == _dropTarget)
                    UIRenderer.Border(thumbnail, UITheme.ToolSelected, 3);
                new UITextContainer { Bounds = new Rectangle(thumbnail.X, thumbnail.Bottom - 24, thumbnail.Width, 22), MaxLines = 1 }
                    .Draw((frameIndex + 1).ToString(), selected ? UITheme.Primary : UITheme.TextMuted, .65f);
            }

            // Tooltips render last so neighboring controls cannot cover them.
            _duplicate.DrawTooltip();
            _insert.DrawTooltip();
            _delete.DrawTooltip();
            _copy.DrawTooltip();
            _paste.DrawTooltip();
        }

        private Rectangle ThumbnailBounds(int startX, int slot) => new(
            startX + slot * (ThumbnailWidth + Gap), Bounds.Y + ToolbarHeight + Gap,
            ThumbnailWidth, Math.Max(1, Bounds.Height - ToolbarHeight - Gap * 2));

        private void UpdateThumbnailDrag(Rectangle thumbnail, int frameIndex)
        {
            UIPointerRouter.Block(thumbnail);
            if (GlobalParameters.GlobalMouse.LeftClick() && UIPointerRouter.ContainsPointer(thumbnail))
            {
                _animation.SelectFrame(frameIndex);
                _dragSource = frameIndex;
                _dropTarget = frameIndex;
                _dragStart = GlobalParameters.GlobalMouse.newMousePos;
            }

            if (_dragSource < 0 || !GlobalParameters.GlobalMouse.LeftClickHold()) return;
            UIPointerRouter.Held(this, thumbnail);
            if (!_isDragging && Vector2.Distance(_dragStart, GlobalParameters.GlobalMouse.newMousePos) >= DragThreshold)
                _isDragging = true;
            if (_isDragging && UIPointerRouter.ContainsPointer(thumbnail)) _dropTarget = frameIndex;
        }

        private void FinishDragIfReleased()
        {
            bool released = GlobalParameters.GlobalMouse.oldMouse.LeftButton == ButtonState.Pressed &&
                GlobalParameters.GlobalMouse.newMouse.LeftButton == ButtonState.Released;
            if (!released) return;
            if (_isDragging && _dragSource >= 0 && _dropTarget >= 0)
                _animation.MoveFrame(_dragSource, _dropTarget);
            _dragSource = -1;
            _dropTarget = -1;
            _isDragging = false;
        }

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
