using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Input;
using FrameByFrame.src.Engine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace FrameByFrame.src
{
    public class GlobalParameters
    {
        public static int screenHeight, screenWidth;
        public static float scaleX, scaleY;

        // Initialized by Main.LoadContent before any scene is updated or drawn.
        public static ContentManager GlobalContent = null!;
        public static SpriteBatch GlobalSpriteBatch = null!;
        public static GraphicsDevice GlobalGraphics = null!;
        public static MouseController GlobalMouse = null!;
        public static KeyboardController GlobalKeyboard = null!;

        public static Dictionary<string, BaseScene> Scenes = null!;
        public static BaseScene CurrentScene = null!;

        public static SpriteFont font = null!;
        public static SpriteFont uiFont = null!;

    }
}
