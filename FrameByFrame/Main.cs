using System;
using FrameByFrame.src;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Input;
using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.UI;
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
        BasicTexture cursor = null!;
        readonly ApplicationContext appContext;
        readonly NavigationService navigation = new();

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            appContext = new ApplicationContext(UITheme.DefaultScreenWidth, UITheme.DefaultScreenHeight);
            Content.RootDirectory = "Content";

            // Max fps with refresh rate
            // this.IsFixedTimeStep = false;
            // this.graphics.SynchronizeWithVerticalRetrace = false;

            // Try to get a certain fps
            int targetFPS = 60;
            this.graphics.SynchronizeWithVerticalRetrace = false; //Vsync
            this.IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0f / targetFPS);
            Exiting += OnExiting;
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = appContext.ScreenWidth;
            graphics.PreferredBackBufferHeight = appContext.ScreenHeight;

            this.IsMouseVisible = true;

            this.Window.AllowUserResizing = true;
            this.Window.ClientSizeChanged += new EventHandler<EventArgs>(OnResize);

            navigation.Register(UIConstants.MENU_SCENE, new MenuScene(appContext, navigation));
            navigation.Register(UIConstants.PROJECTS_SCENE, new ProjectsScene(appContext, navigation));
            navigation.Register(UIConstants.DRAWING_SCENE, new DrawingScene(appContext, navigation));
            navigation.Navigate(UIConstants.MENU_SCENE);
            
            // Initialize debug manager
            DebugManager.Initialize();
            
            graphics.ApplyChanges();
            base.Initialize();
        }

        protected override void LoadContent()
        {
            appContext.InitializeGraphics(Content, new SpriteBatch(GraphicsDevice), graphics.GraphicsDevice,
                new MouseController(), new KeyboardController(), Content.Load<SpriteFont>("Roboto"),
                Content.Load<SpriteFont>("UIFont"));
            ApplicationRuntime.Initialize(appContext);

            cursor = new BasicTexture("pencil-cursor", new Vector2(0, 0), new Vector2(28, 28));

            foreach (BaseScene scene in navigation.Scenes) scene.LoadContent();
        }

        protected override void UnloadContent()
        {
            // Dispose all scenes
            foreach (BaseScene scene in navigation.Scenes)
            {
                if (scene is IDisposable disposable)
                    disposable.Dispose();
            }
            
            // Clear texture cache
            TextureManager.ClearCache();
            
            // Dispose cursor
            cursor?.texture?.Dispose();
        }

        private void OnExiting(object? sender, EventArgs args)
        {
            navigation.Get<DrawingScene>(UIConstants.DRAWING_SCENE).PrepareForShutdown();
        }

        protected override void Update(GameTime gameTime)
        {
            // Update debug manager first to handle key presses
            DebugManager.Update();
            
            PerformanceMonitor.StartFrame();
            
            appContext.Mouse.Update();
            appContext.Keyboard.Update();
            navigation.Current.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            appContext.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwise);

            navigation.Current.Draw(Vector2.Zero);
            cursor.Draw(new Vector2(appContext.Mouse.newMousePos.X - 5, appContext.Mouse.newMousePos.Y - 25), new Vector2(0, 0));

            // Draw debug overlays only in debug mode
            if (DebugManager.IsDebugMode)
            {
                PerformanceMonitor.DrawPerformanceOverlay(new Vector2(10, 10), UIConstants.DEBUG_PERFORMANCE);
                DebugManager.DrawDebugHelp(new Vector2(10, appContext.ScreenHeight - 100));
            }

            appContext.SpriteBatch.End();
            
            PerformanceMonitor.EndFrame();
            base.Draw(gameTime);
        }

        private void OnResize(object? sender, EventArgs e)
        {
            appContext.Resize(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
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
