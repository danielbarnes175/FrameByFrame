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
        private Dictionary<string, BaseScene> Scenes;
        private BaseScene CurrentScene;

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // this.IsFixedTimeStep = true;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0f / 100.0f);
            //this.graphics.SynchronizeWithVerticalRetrace = true;
            // this.TargetElapsedTime = new System.TimeSpan(0, 0, 0, 0, 16);
        }

        protected override void Initialize()
        {
            GlobalParameters.screenWidth = 1600;
            GlobalParameters.screenHeight = 900;

            graphics.PreferredBackBufferWidth = GlobalParameters.screenWidth;
            graphics.PreferredBackBufferHeight = GlobalParameters.screenHeight;

            Scenes.Add("Menu Scene", new MenuScene());
            Scenes.Add("Projects Scene", new ProjectsScene());
            Scenes.Add("Settings Scene", new SettingsScene());
            Scenes.Add("Drawing Scene", new DrawingScene());

            CurrentScene = Scenes["Menu Scene"];
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

            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            cursor = new BasicTexture("Static\\pencil-cursor", new Vector2(0, 0), new Vector2(28, 28));
        }

        protected override void UnloadContent()
        {

        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            GlobalParameters.GlobalMouse.Update();
            GlobalParameters.GlobalKeyboard.Update();

            GlobalParameters.GlobalMouse.UpdateOld();
            GlobalParameters.GlobalKeyboard.UpdateOld();

            CurrentScene.Update();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            CurrentScene.Draw(Vector2.Zero);
            cursor.Draw(new Vector2(GlobalParameters.GlobalMouse.newMousePos.X, GlobalParameters.GlobalMouse.newMousePos.Y), new Vector2(0, 0));

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
