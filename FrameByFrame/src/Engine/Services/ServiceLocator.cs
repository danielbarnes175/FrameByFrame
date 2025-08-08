using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrameByFrame.src.Engine.Input;

namespace FrameByFrame.src.Engine.Services
{
    public class GameServices
    {
        public GraphicsDevice GraphicsDevice { get; }
        public SpriteBatch SpriteBatch { get; }
        public SpriteFont Font { get; }
        public MouseController Mouse { get; }
        public KeyboardController Keyboard { get; }
        
        public GameServices(
            GraphicsDevice graphicsDevice,
            SpriteBatch spriteBatch,
            SpriteFont font,
            MouseController mouse,
            KeyboardController keyboard)
        {
            GraphicsDevice = graphicsDevice;
            SpriteBatch = spriteBatch;
            Font = font;
            Mouse = mouse;
            Keyboard = keyboard;
        }
    }
    
    // Service locator pattern (better than static GlobalParameters)
    public static class ServiceLocator
    {
        private static GameServices _services;
        
        public static void Initialize(GameServices services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }
        
        public static GameServices Services => _services ?? throw new InvalidOperationException("Services not initialized");
    }
}