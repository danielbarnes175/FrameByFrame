using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace FrameByFrame.src
{
    public class GlobalParameters
    {
        public static int screenHeight, screenWidth;

        public static ContentManager GlobalContent;
        public static SpriteBatch GlobalSpriteBatch;
        public static GraphicsDevice GlobalGraphics;
        public static MouseController GlobalMouse;
        public static KeyboardController GlobalKeyboard;
    }
}
