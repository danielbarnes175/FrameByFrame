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

        public static ContentManager GlobalContent;
        public static SpriteBatch GlobalSpriteBatch;
        public static GraphicsDevice GlobalGraphics;
        public static MouseController GlobalMouse;
        public static KeyboardController GlobalKeyboard;

        public static Dictionary<string, BaseScene> Scenes;
        public static BaseScene CurrentScene;

        public static SpriteFont font;

        public static Color CurrentColor;
    }
}
