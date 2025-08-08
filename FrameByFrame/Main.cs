using System;
using System.Collections.Generic;
using FrameByFrame.src;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Input;
using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Engine.Services;
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

            GlobalParameters.scaleX = GlobalParameters.screenWidth / 1600f;
            GlobalParameters.scaleY = GlobalParameters.screenHeight / 900f;

            this.IsMouseVisible = true;

            // Basic resizing is implemented, but not for every element, and it's buggy, so it's disabled.
            this.Window.AllowUserResizing = false;
            this.Window.ClientSizeChanged += new EventHandler<EventArgs>(OnResize);

            GlobalParameters.Scenes = new Dictionary<string, BaseScene>
            {
                { "Menu Scene", new MenuScene() },
                { "Projects Scene", new ProjectsScene() },
                { "Settings Scene", new SettingsScene() },
                { "Drawing Scene", new DrawingScene() }
            };

            GlobalParameters.CurrentScene = GlobalParameters.Scenes["Menu Scene"];
            
            // Initialize debug manager
            DebugManager.Initialize();
            
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
            // Dispose all scenes
            foreach (var scene in GlobalParameters.Scenes.Values)
            {
                if (scene is IDisposable disposable)
                    disposable.Dispose();
            }
            
            // Clear texture cache
            TextureManager.ClearCache();
            
            // Dispose cursor
            cursor?.texture?.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            // Update debug manager first to handle key presses
            DebugManager.Update();
            
            PerformanceMonitor.StartFrame();
            
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

            // Draw debug overlays only in debug mode
            if (DebugManager.IsDebugMode)
            {
                PerformanceMonitor.DrawPerformanceOverlay(new Vector2(10, 10), UIConstants.DEBUG_PERFORMANCE);
                DebugManager.DrawDebugHelp(new Vector2(10, GlobalParameters.screenHeight - 100));
            }

            GlobalParameters.GlobalSpriteBatch.End();
            
            PerformanceMonitor.EndFrame();
            base.Draw(gameTime);
        }

        private void OnResize(object sender, EventArgs e)
        {
            GlobalParameters.screenWidth = GraphicsDevice.Viewport.Width;
            GlobalParameters.screenHeight = GraphicsDevice.Viewport.Height;

            GlobalParameters.scaleX = Math.Max(GlobalParameters.screenWidth / 1600f, 0.5f);
            GlobalParameters.scaleY = Math.Max(GlobalParameters.screenHeight / 900f, 0.5f);

            graphics.PreferredBackBufferWidth = GlobalParameters.screenWidth;
            graphics.PreferredBackBufferHeight = GlobalParameters.screenHeight;
            graphics.ApplyChanges();
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
