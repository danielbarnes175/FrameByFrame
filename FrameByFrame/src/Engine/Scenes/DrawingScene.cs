using System;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.UI;
using FrameByFrame.src.UI.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FrameByFrame.src.Engine.Scenes
{
    public class DrawingScene : BaseScene
    {
        public Animation.Animation animation;
        public DrawingTools drawingTool;
        private DrawingNavbarComponent _navbar;
        private TimelineComponent _timeline;
        public bool loadedScene;
        private bool _pixelEditActive;

        public DrawingScene() => InitializeDefaults();

        private void InitializeDefaults()
        {
            animation = new Animation.Animation($"Project_{DateTime.Now:yyyyMMdd_HHmmss}");
            drawingTool = DrawingTools.DRAW;
            loadedScene = false;
        }

        public override void LoadContent()
        {
            SetupUI();
            if (animation.TotalFrames == 0) animation.InitializeFrames();
        }

        public Color GetSelectedColorFromColorWheel() => _navbar?.SelectedColor ?? Color.Black;

        private void SetSelectedColor(Color color)
        {
            _navbar?.SetSelectedColor(color);
        }

        private void SetupUI()
        {
            _navbar?.Dispose();
            _navbar = new DrawingNavbarComponent(animation);
            _timeline = new TimelineComponent(animation);
            _navbar.Arrange(new Rectangle(0, 0, GlobalParameters.screenWidth, UITheme.AppBarHeight));
        }

        public override void Update(GameTime gameTime)
        {
            UIPointerRouter.BeginFrame();
            int navbarHeight = DrawingNavbarComponent.PreferredHeight(GlobalParameters.screenWidth);
            int timelineHeight = Math.Min(UITheme.TimelineHeight,
                Math.Max(72, (GlobalParameters.screenHeight - navbarHeight) / 3));
            _navbar.Arrange(new Rectangle(0, 0, GlobalParameters.screenWidth, navbarHeight));
            _timeline.Arrange(new Rectangle(0, GlobalParameters.screenHeight - timelineHeight,
                GlobalParameters.screenWidth, timelineHeight));
            HandleKeyboardShortcuts();
            if (!_navbar.HasOpenPopover) _timeline.Update();
            _navbar.Update();
            HandleMouseShortcuts();
            animation.Animate(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            GlobalParameters.GlobalGraphics.Clear(UITheme.CanvasStage);
            int navbarHeight = _navbar.Bounds.Height;
            int timelineHeight = _timeline.Bounds.Height;
            int margin = Math.Min(24, Math.Max(4, Math.Min(GlobalParameters.screenWidth, GlobalParameters.screenHeight) / 30));
            Rectangle stage = new(margin, navbarHeight + margin, Math.Max(1, GlobalParameters.screenWidth - margin * 2),
                Math.Max(1, GlobalParameters.screenHeight - navbarHeight - timelineHeight - margin * 2));
            animation.DrawCurrentFrame(UILayoutEngine.FitAspect(stage, animation.frameSize.X / animation.frameSize.Y));
            _timeline.Draw();
            _navbar.Draw();
            MemoryMonitor.DrawMemoryOverlay(new Vector2(10, GlobalParameters.screenHeight - 30), UIConstants.DEBUG_MEMORY, animation);
        }

        private void HandleMouseShortcuts()
        {
            if (!GlobalParameters.GlobalMouse.LeftClickHold())
            {
                if (_pixelEditActive) animation.CommitPixelEdit();
                _pixelEditActive = false;
                if (!loadedScene) loadedScene = true;
                return;
            }
            if (UIPointerRouter.IsPointerBlocked()) return;
            if (!GlobalParameters.GlobalMouse.LeftClickHold() || !loadedScene) return;

            Color selectedColor = GetSelectedColorFromColorWheel();
            switch (drawingTool)
            {
                case DrawingTools.DRAW:
                    BeginPixelEdit();
                    animation.DrawOnCurrentLayer(selectedColor);
                    break;
                case DrawingTools.ERASER:
                    BeginPixelEdit();
                    animation.DrawOnCurrentLayer(Color.Transparent);
                    break;
                case DrawingTools.FILL when GlobalParameters.GlobalMouse.LeftClick():
                    animation.BeginPixelEdit();
                    animation.FillCurrentLayerAt(GlobalParameters.GlobalMouse.newMousePos, selectedColor);
                    animation.CommitPixelEdit();
                    break;
                case DrawingTools.COLOR_PICKER:
                    Color sampled = animation.SampleVisibleColorAt(GlobalParameters.GlobalMouse.newMousePos);
                    if (sampled.A > 0) SetSelectedColor(sampled);
                    break;
            }
        }

        private void BeginPixelEdit()
        {
            animation.BeginPixelEdit();
            _pixelEditActive = true;
        }

        public void BeginNewAnimation(int width, int height)
        {
            animation?.Dispose();
            InitializeDefaults();
            animation.InitializeFrames(width, height);
            SetupUI();
        }

        public void LoadAnimation(Animation.Animation loadedAnimation)
        {
            ArgumentNullException.ThrowIfNull(loadedAnimation);
            animation?.Dispose();
            animation = loadedAnimation;
            drawingTool = DrawingTools.DRAW;
            loadedScene = false;
            SetupUI();
        }

        public override void Dispose()
        {
            _navbar?.Dispose();
            _navbar = null;
            _timeline = null;
            animation?.Dispose();
            animation = null;
        }

        private void HandleKeyboardShortcuts()
        {
            bool control = GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.LeftControl)
                || GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.RightControl);
            if (control && GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.Z)) animation.Undo();
            if (control && GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.Y)) animation.Redo();
            if (control && GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.D)) animation.DuplicateCurrentFrame();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC"))
            {
                animation.Stop();
                GlobalParameters.CurrentScene = GlobalParameters.Scenes[UIConstants.MENU_SCENE];
            }
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("DELETE")) animation.DeleteFrame();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("P")) animation.TogglePlaying();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("BACKSPACE")) animation.EraseCurrentLayer();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("O")) animation.isOnionSkinEnabled = !animation.isOnionSkinEnabled;
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("M")) animation.NextFrame();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("N")) animation.PreviousFrame();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("B")) animation.InsertFrame();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("L")) _navbar.SaveAnimation();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("[") && animation.brushSize > UIConstants.MIN_BRUSH_SIZE) animation.brushSize--;
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("]") && animation.brushSize < UIConstants.MAX_BRUSH_SIZE) animation.brushSize++;
        }
    }
}
