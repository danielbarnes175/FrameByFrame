using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI
{
    public class Frame
    {
        public Matrix transform;

        public BasicColor[,] _layer1;
        public BasicColor[,] _layer2;
        public BasicColor[,] _layer3;

        public Texture2D[] colors;

        public static int width { get; set; }
        public static int height { get; set; }

        public Frame(Vector2 position, Vector2 dimensions)
        {
            width = (int)dimensions.X;
            height = (int)dimensions.Y;
            transform = Matrix.Identity;

            colors = new Texture2D[2];
            colors[0] = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 1, 1, pixel => Color.Black, Shapes.RECTANGLE);
            Texture2D layerTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 1, 1, pixel => new Color(), Shapes.RECTANGLE);

            _layer1 = new BasicColor[(int)dimensions.X, (int)dimensions.Y];
            _layer2 = new BasicColor[(int)dimensions.X, (int)dimensions.Y];
            _layer3 = new BasicColor[(int)dimensions.X, (int)dimensions.Y];

            for (int i = 0; i < dimensions.X; i++)
            {
                for (int j = 0; j < dimensions.Y; j++)
                {
                    /*_layer1[i, j] = new BasicColor(colors[0], new Vector2(i, j), new Vector2(1, 1));
                    _layer2[i, j] = new BasicColor(colors[0], new Vector2(i, j), new Vector2(1, 1));
                    _layer3[i, j] = new BasicColor(colors[0], new Vector2(i, j), new Vector2(1, 1));*/
                }
            }
        }
    }
}
