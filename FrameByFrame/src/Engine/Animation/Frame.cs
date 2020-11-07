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
        public BasicTexture _layer2;
        public BasicTexture _layer3;

        public static int width { get; set; }
        public static int height { get; set; }

        public Frame()
        {
            width = 1165;
            height = 850;

            Texture2D texture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, width, height, pixel => new Color(), Shapes.RECTANGLE);

            _layer1 = new BasicTexture(texture, new Vector2(width / 2, height / 2), new Vector2(width, height));
            _layer2 = new BasicTexture(texture, new Vector2(width / 2, height / 2), new Vector2(width, height));
            _layer3 = new BasicTexture(texture, new Vector2(width / 2, height / 2), new Vector2(width, height));
        }
    }
}
