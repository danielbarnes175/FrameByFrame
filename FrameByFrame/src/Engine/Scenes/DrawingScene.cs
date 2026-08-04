using System;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.UI;
using FrameByFrame.src.UI.Components;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Scenes
{
    public class DrawingScene : BaseScene
    {
        public Animation.Animation animation;
        public DrawingTools drawingTool;
        private DrawingNavbarComponent _navbar;
        public bool loadedScene;

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
            _navbar.Arrange(new Rectangle(0, 0, GlobalParameters.screenWidth, UIConstants.NAVBAR_HEIGHT));
        }

        public override void Update(GameTime gameTime)
        {
            UIPointerRouter.BeginFrame();
            _navbar.Arrange(new Rectangle(0, 0, GlobalParameters.screenWidth, UIConstants.NAVBAR_HEIGHT));
            HandleKeyboardShortcuts();
            _navbar.Update();
            HandleMouseShortcuts();
            animation.Animate(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            GlobalParameters.GlobalGraphics.Clear(UIConstants.BACKGROUND_DARK);
            animation.DrawCurrentFrame();
            _navbar.Draw();
            MemoryMonitor.DrawMemoryOverlay(new Vector2(10, GlobalParameters.screenHeight - 30), UIConstants.DEBUG_MEMORY, animation);
        }

        private void HandleMouseShortcuts()
        {
            if (!GlobalParameters.GlobalMouse.LeftClickHold() && !loadedScene) { loadedScene = true; return; }
            if (UIPointerRouter.IsPointerBlocked()) return;
            if (!GlobalParameters.GlobalMouse.LeftClickHold() || !loadedScene) return;

            Color selectedColor = GetSelectedColorFromColorWheel();
            switch (drawingTool)
            {
                case DrawingTools.DRAW:
                    animation.DrawOnCurrentLayer(selectedColor);
                    break;
                case DrawingTools.ERASER:
                    animation.DrawOnCurrentLayer(Color.Transparent);
                    break;
                case DrawingTools.FILL when GlobalParameters.GlobalMouse.LeftClick():
                    animation.FillCurrentLayerAt(GlobalParameters.GlobalMouse.newMousePos, selectedColor);
                    break;
                case DrawingTools.COLOR_PICKER:
                    Color sampled = animation.SampleVisibleColorAt(GlobalParameters.GlobalMouse.newMousePos);
                    if (sampled.A > 0) SetSelectedColor(sampled);
                    break;
            }
        }

        public void BeginNewAnimation()
        {
            animation?.Dispose();
            InitializeDefaults();
            animation.InitializeFrames();
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
            animation?.Dispose();
            animation = null;
        }

        private void HandleKeyboardShortcuts()
        {
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
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("L")) SaveService.SaveAnimation(animation);
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("[") && animation.brushSize > UIConstants.MIN_BRUSH_SIZE) animation.brushSize--;
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("]") && animation.brushSize < UIConstants.MAX_BRUSH_SIZE) animation.brushSize++;
        }
    }
}
