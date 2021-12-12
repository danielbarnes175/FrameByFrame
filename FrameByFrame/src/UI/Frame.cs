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
        
        private Rectangle drawRectangle;

        public BasicColor[,] _layer1;
        public BasicColor[,] _layer2;
        public BasicColor[,] _layer3;

        public Texture2D[] colors;
        public Texture2D background;

        public static Vector2 position;
        public static int width { get; set; }
        public static int height { get; set; }

        public Frame(Vector2 givenPosition, Vector2 dimensions)
        {
            width = (int)dimensions.X;
            height = (int)dimensions.Y;

            position = givenPosition;
            transform = Matrix.Identity;

            drawRectangle = new Rectangle((int)position.X, (int)position.Y, width, height);

            colors = new Texture2D[2];
            colors[0] = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 1, 1, pixel => Color.Black, Shapes.RECTANGLE);
            Texture2D layerTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 1, 1, pixel => new Color(), Shapes.RECTANGLE);
            background = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, width, height, pixel => Color.White, Shapes.RECTANGLE);

            _layer1 = new BasicColor[(int)dimensions.X, (int)dimensions.Y];
            _layer2 = new BasicColor[(int)dimensions.X, (int)dimensions.Y];
            _layer3 = new BasicColor[(int)dimensions.X, (int)dimensions.Y];
        }

        public virtual void Draw(float opacity)
        {
            if (background != null)
            {
                System.Diagnostics.Debug.WriteLine(drawRectangle);
                GlobalParameters.GlobalSpriteBatch.Draw(background,
                    drawRectangle, 
                    null, 
                    Color.White * opacity, 
                    0.0f,
                    new Vector2(0,0), 
                    new SpriteEffects(), 
                    0.2f);
            }
        }
    }
}
