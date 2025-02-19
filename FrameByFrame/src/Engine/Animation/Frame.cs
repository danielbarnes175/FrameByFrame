﻿using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FrameByFrame.src.Engine.Animation
{
    public class Frame
    {
        public Matrix transform;

        private Rectangle drawRectangle;

        public BasicColor[,] _layer1;
        public BasicColor[,] _layer2;
        public BasicColor[,] _layer3;

        public BasicTexture CombinedTexture;

        public Texture2D[] colors;
        public Texture2D background;

        public static Vector2 position;
        public static int staticWidth { get; set; }
        public static int staticHeight { get; set; }

        public int width { get; set; }
        public int height { get; set; }

        public Frame(Vector2 givenPosition, Vector2 dimensions)
        {
            staticWidth = (int)dimensions.X;
            staticHeight = (int)dimensions.Y;

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
                Vector2 scaledDimensions = new Vector2(width * GlobalParameters.scaleX, height * GlobalParameters.scaleY);
                Vector2 drawPosition = (position + Vector2.Zero) * 1.0f;
                Rectangle scaleRect = new Rectangle((int)drawPosition.X, (int)drawPosition.Y, (int)scaledDimensions.X, (int)scaledDimensions.Y);

                GlobalParameters.GlobalSpriteBatch.Draw(background,
                    drawRectangle,
                    scaleRect,
                    Color.White * opacity,
                    0.0f,
                    new Vector2(0, 0),
                    new SpriteEffects(),
                    0.2f);
            }
        }

        public virtual void DrawLayers(float opacity)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    _layer3[i, j]?.Draw(opacity);
                    _layer2[i, j]?.Draw(opacity);
                    _layer1[i, j]?.Draw(opacity);
                }
            }
        }

        public static BasicColor[,] ConvertTextureToLayer(Texture2D texture, Vector2 position, Vector2 dimensions)
        {
            int width = (int)dimensions.X;
            int height = (int)dimensions.Y;

            // Create a BasicColor layer array matching the dimensions
            BasicColor[,] layer = new BasicColor[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Assign a BasicColor object for each pixel
                    layer[x, y] = new BasicColor(texture, position, dimensions);
                }
            }

            return layer;
        }

        public void DrawCombinedTexture(float opacity)
        {
            CombinedTexture.Draw(Vector2.Zero, opacity);
        }
    }
}
