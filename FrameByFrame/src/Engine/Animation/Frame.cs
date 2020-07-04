using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Animation
{
    public class Frame
    {
        public BasicTexture _layer1;
        public List<BasicTexture> _layer2;
        public List<BasicTexture> _layer3;

        public static int width = 1165;
        public static int height = 850;
        public Frame()
        {
            Texture2D texture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, width, height, pixel => Color.White, Shapes.RECTANGLE);

            _layer1 = new BasicTexture(texture, new Vector2(width / 2, height / 2), new Vector2(width, height));
            _layer2 = new List<BasicTexture>();
            _layer3 = new List<BasicTexture>();
        }
    }
}
