using System;
using System.Collections.Generic;
using FrameByFrame.src.Engine.Input;
using FrameByFrame.src.Engine.Scenes;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src
{
    public interface IApplicationContext
    {
        int ScreenWidth { get; }
        int ScreenHeight { get; }
        float ScaleX { get; }
        float ScaleY { get; }
        ContentManager Content { get; }
        SpriteBatch SpriteBatch { get; }
        GraphicsDevice Graphics { get; }
        MouseController Mouse { get; }
        KeyboardController Keyboard { get; }
        SpriteFont Font { get; }
        SpriteFont UiFont { get; }
    }

    public sealed class ApplicationContext : IApplicationContext
    {
        public int ScreenWidth { get; private set; }
        public int ScreenHeight { get; private set; }
        public float ScaleX => ScreenWidth / 1600f;
        public float ScaleY => ScreenHeight / 900f;
        public ContentManager Content { get; private set; } = null!;
        public SpriteBatch SpriteBatch { get; private set; } = null!;
        public GraphicsDevice Graphics { get; private set; } = null!;
        public MouseController Mouse { get; private set; } = null!;
        public KeyboardController Keyboard { get; private set; } = null!;
        public SpriteFont Font { get; private set; } = null!;
        public SpriteFont UiFont { get; private set; } = null!;

        public ApplicationContext(int width, int height) => Resize(width, height);

        public void InitializeGraphics(ContentManager content, SpriteBatch spriteBatch,
            GraphicsDevice graphics, MouseController mouse, KeyboardController keyboard,
            SpriteFont font, SpriteFont uiFont)
        {
            Content = content;
            SpriteBatch = spriteBatch;
            Graphics = graphics;
            Mouse = mouse;
            Keyboard = keyboard;
            Font = font;
            UiFont = uiFont;
        }

        public void Resize(int width, int height)
        {
            ScreenWidth = Math.Max(1, width);
            ScreenHeight = Math.Max(1, height);
        }
    }

    public interface INavigationService
    {
        BaseScene Current { get; }
        T Get<T>(string name) where T : BaseScene;
        void Navigate(string name);
    }

    public sealed class NavigationService : INavigationService
    {
        private readonly Dictionary<string, BaseScene> _scenes = new();
        public IReadOnlyCollection<BaseScene> Scenes => _scenes.Values;
        public BaseScene Current { get; private set; } = null!;

        public void Register(string name, BaseScene scene)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentNullException.ThrowIfNull(scene);
            _scenes.Add(name, scene);
        }

        public T Get<T>(string name) where T : BaseScene =>
            _scenes.TryGetValue(name, out BaseScene? scene) && scene is T typed
                ? typed
                : throw new InvalidOperationException($"Scene '{name}' is not registered as {typeof(T).Name}.");

        public void Navigate(string name) => Current = Get<BaseScene>(name);
    }

    // Compatibility bridge for static rendering helpers. It is initialized once by Main;
    // application code receives IApplicationContext and INavigationService explicitly.
    public static class ApplicationRuntime
    {
        private static IApplicationContext? _current;
        public static IApplicationContext Current => _current ??
            throw new InvalidOperationException("The application context has not been initialized.");

        public static void Initialize(IApplicationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (_current != null && !ReferenceEquals(_current, context))
                throw new InvalidOperationException("The application context is already initialized.");
            _current = context;
        }
    }
}
