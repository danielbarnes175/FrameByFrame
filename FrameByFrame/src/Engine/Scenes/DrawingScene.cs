using System;
using System.Collections.Generic;
using System.Linq;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.UI.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class DrawingScene : BaseScene
    {
        // Frame Management
        public Animation.Animation animation;

        // Tools
        public DrawingTools drawingTool;

        // UI
        private List<UIElement> components;

        // Project Info
        public bool loadedScene;

        public DrawingScene()
        {
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            string projectName = $"Project_{DateTime.Now:yyyyMMdd_HHmmss}";

            animation = new Animation.Animation(projectName);
            drawingTool = DrawingTools.DRAW;
            loadedScene = false;
        }

        public override void LoadContent()
        {
            SetupUI();
            animation.InitializeFrames();
        }

        private void SetupUI()
        {
            components = new List<UIElement>();
            Texture2D navbarBG = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, GlobalParameters.screenWidth, 50, pixel => Color.Orange, Shapes.RECTANGLE);
            DrawingNavbarComponent navbar = new DrawingNavbarComponent(navbarBG, new Vector2(0, 0), new Vector2(GlobalParameters.screenWidth, 50));

            components.Add(navbar);
        }

        public override void Update(GameTime gameTime)
        {
            HandleKeyboardShortcuts();
            HandleMouseShortcuts();
            animation.Animate(gameTime);

            foreach (UIElement element in components)
            {
                element.Update();
            }

            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            GlobalParameters.GlobalGraphics.Clear(new Color(45, 45, 45));

            animation.DrawCurrentFrame();

            foreach (UIElement element in components)
            {
                element.Draw(offset, new Vector2(0, 0));
            }

            base.Draw(offset);
        }


        private void HandleMouseShortcuts()
        {
            // So we don't start drawing until we load the scene
            if (!GlobalParameters.GlobalMouse.LeftClickHold() && !loadedScene)
            {
                loadedScene = true;
                return;
            }

            // Draw on current frame
            if (GlobalParameters.GlobalMouse.LeftClickHold() && loadedScene)
            {
                animation.DrawOnCurrentLayer();
            }
        }

        private void ResetScene()
        {
            InitializeDefaults();
            LoadContent();
        }

        private void HandleKeyboardShortcuts()
        {
            // Exit drawing
            if (GlobalParameters.GlobalKeyboard.GetPress("ESC"))
            {
                ResetScene();
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Menu Scene"];
            }

            // Delete current frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("DELETE"))
            {
                animation.DeleteFrame();
            }

            // Toggle animation playing
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("P"))
            {
                animation.TogglePlaying();
            }

            // Switch to settings scene
            if (GlobalParameters.GlobalKeyboard.GetPress("W"))
            {
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Settings Scene"];
                animation.Stop();
            }

            // Erase current layer
            if (GlobalParameters.GlobalKeyboard.GetPress("BACKSPACE"))
            {
                animation.EraseCurrentLayer();
            }

            // Toggle Onion Skin
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("O"))
                animation.isOnionSkinLoaded = !animation.isOnionSkinLoaded;

            // Load next frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("M"))
            {
                animation.NextFrame();
            }

            // Load previous frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("N"))
            {
                animation.PreviousFrame();
            }

            // Insert a frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("B"))
            {
                animation.InsertFrame();
            }

            // Save animation
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("L"))
            {
                SaveService.SaveAnimation(animation);
            }
        }

    }
}
