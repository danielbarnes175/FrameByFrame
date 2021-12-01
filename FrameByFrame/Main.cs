using System;
using System.Collections.Generic;
using FrameByFrame.src;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Input;
using FrameByFrame.src.Engine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FrameByFrame
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Main : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        BasicTexture cursor;

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Max fps with refresh rate
            // this.IsFixedTimeStep = false;
            // this.graphics.SynchronizeWithVerticalRetrace = false;

            // Try to get a certain fps
            int targetFPS = 60;
            this.graphics.SynchronizeWithVerticalRetrace = false; //Vsync
            this.IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0f / targetFPS);
        }

        protected override void Initialize()
        {
            GlobalParameters.screenWidth = 1600;
            GlobalParameters.screenHeight = 900;

            graphics.PreferredBackBufferWidth = GlobalParameters.screenWidth;
            graphics.PreferredBackBufferHeight = GlobalParameters.screenHeight;

            GlobalParameters.Scenes = new Dictionary<string, BaseScene>();
            GlobalParameters.Scenes.Add("Menu Scene", (BaseScene)new MenuScene());
            GlobalParameters.Scenes.Add("Projects Scene", new ProjectsScene());
            GlobalParameters.Scenes.Add("Settings Scene", new SettingsScene());
            GlobalParameters.Scenes.Add("Drawing Scene", new DrawingScene());

            GlobalParameters.CurrentScene = GlobalParameters.Scenes["Menu Scene"];
            graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            GlobalParameters.GlobalContent = this.Content;
            GlobalParameters.GlobalSpriteBatch = new SpriteBatch(GraphicsDevice);
            GlobalParameters.GlobalGraphics = graphics.GraphicsDevice;
            GlobalParameters.GlobalKeyboard = new KeyboardController();
            GlobalParameters.GlobalMouse = new MouseController();

            GlobalParameters.CurrentColor = Color.Black;
            GlobalParameters.font = Content.Load<SpriteFont>("Static\\Roboto");

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            cursor = new BasicTexture("Static\\pencil-cursor", new Vector2(0, 0), new Vector2(28, 28));

            foreach (KeyValuePair<string, BaseScene> scene in GlobalParameters.Scenes)
            {
                scene.Value.LoadContent();
            }
        }

        protected override void UnloadContent()
        {

        }

        protected override void Update(GameTime gameTime)
        {
            GlobalParameters.GlobalMouse.Update();
            GlobalParameters.GlobalKeyboard.Update();

            GlobalParameters.GlobalKeyboard.UpdateOld();

            GlobalParameters.CurrentScene.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwise);

            GlobalParameters.CurrentScene.Draw(Vector2.Zero);
            cursor.Draw(new Vector2(GlobalParameters.GlobalMouse.newMousePos.X - 5, GlobalParameters.GlobalMouse.newMousePos.Y - 25), new Vector2(0, 0));

            GlobalParameters.GlobalSpriteBatch.End();
            base.Draw(gameTime);
        }
    }

    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new Main())
                game.Run();
        }
    }
}
