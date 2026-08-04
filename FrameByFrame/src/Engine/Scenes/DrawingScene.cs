using System;
using System.Collections.Generic;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.Engine.UI;
using FrameByFrame.src.UI.Components;
using FrameByFrame.src.UI.Components.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class DrawingScene : BaseScene
    {
        public Animation.Animation animation;
        public DrawingTools drawingTool;
        private List<UIElement> components;
        private int _layoutWidth;
        private int _layoutHeight;
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

        public Color GetSelectedColorFromColorWheel()
        {
            foreach (UIElement element in components)
            {
                if (element is not DrawingNavbarComponent navbar) continue;
                foreach (UIElement navbarElement in navbar.uiElements)
                    if (navbarElement is PopupButton { target: ColorWheelComponent colorWheel })
                        return colorWheel.SelectedColor;
            }
            return Color.Black;
        }

        private void SetSelectedColor(Color color)
        {
            foreach (UIElement element in components)
            {
                if (element is not DrawingNavbarComponent navbar) continue;
                foreach (UIElement navbarElement in navbar.uiElements)
                {
                    if (navbarElement is PopupButton { target: ColorWheelComponent colorWheel })
                    {
                        colorWheel.SetSelectedColor(color);
                        return;
                    }
                }
            }
        }

        private void SetupUI()
        {
            UIInteractionManager.Clear();
            components = [];
            Texture2D background = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, Color.White, GlobalParameters.screenWidth);
            components.Add(new DrawingNavbarComponent(background, Vector2.Zero,
                new Vector2(GlobalParameters.screenWidth, UIConstants.NAVBAR_HEIGHT), animation));
            _layoutWidth = GlobalParameters.screenWidth;
            _layoutHeight = GlobalParameters.screenHeight;
        }

        public override void Update(GameTime gameTime)
        {
            if (_layoutWidth != GlobalParameters.screenWidth || _layoutHeight != GlobalParameters.screenHeight)
                SetupUI();
            HandleKeyboardShortcuts();
            foreach (UIElement element in components) element.Update();
            UIInteractionManager.Update();
            HandleMouseShortcuts();
            animation.Animate(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            GlobalParameters.GlobalGraphics.Clear(UIConstants.BACKGROUND_DARK);
            animation.DrawCurrentFrame();
            foreach (UIElement element in components) element.Draw(offset, Vector2.Zero);
            MemoryMonitor.DrawMemoryOverlay(new Vector2(10, GlobalParameters.screenHeight - 30), UIConstants.DEBUG_MEMORY, animation);
        }

        private void HandleMouseShortcuts()
        {
            if (!GlobalParameters.GlobalMouse.LeftClickHold() && !loadedScene) { loadedScene = true; return; }
            Rectangle navbar = new(0, 0, GlobalParameters.screenWidth, UIConstants.NAVBAR_HEIGHT);
            if (UIInteractionManager.IsUIBlocking() || UIInteractionManager.IsMouseOverNavbar(navbar)) return;
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
            UIInteractionManager.Clear();
            animation?.Dispose();
            InitializeDefaults();
            animation.InitializeFrames();
            SetupUI();
        }

        public void LoadAnimation(Animation.Animation loadedAnimation)
        {
            ArgumentNullException.ThrowIfNull(loadedAnimation);
            UIInteractionManager.Clear();
            animation?.Dispose();
            animation = loadedAnimation;
            drawingTool = DrawingTools.DRAW;
            loadedScene = false;
            SetupUI();
        }

        public override void Dispose()
        {
            UIInteractionManager.Clear();
            components?.Clear();
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
