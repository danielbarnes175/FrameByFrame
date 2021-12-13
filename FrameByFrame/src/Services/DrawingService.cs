using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameByFrame.src.Engine.Services
{
    public static class DrawingService
    {
        public static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint, Shapes shape)
        {
            //initialize a texture
            Texture2D texture = new Texture2D(device, width, height);

            //the array holds the color for each pixel in the texture
            Color[] data = new Color[width * height];
            switch (shape)
            {
                case Shapes.CIRCLE:
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            double distance = Math.Sqrt(Math.Pow(i - height / 2, 2) + Math.Pow(j - width / 2, 2));
                            if (distance <= width / 2.0)
                            {
                                data[width * i + j] = paint(width * i + j);
                            }
                        }
                    }
                    break;
                case Shapes.RECTANGLE:
                    for (int pixel = 0; pixel < data.Count(); pixel++)
                    {
                        //the function applies the color according to the specified pixel
                        data[pixel] = paint(pixel);
                    }
                    break;
            }

            //set the color
            texture.SetData(data);

            return texture;
        }

        public static void SetColors(BasicColor[,] layer, Texture2D color, Vector2 pointPosition, Shapes shape, int brushSize)
        {
            switch (shape)
            {
                case Shapes.CIRCLE:
                    if (((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).drawingTool != DrawingTools.ERASER)
                    {
                        for (int i = (int)pointPosition.X - (brushSize / 2); i <= pointPosition.X + (brushSize / 2); i++)
                        {
                            for (int j = (int)pointPosition.Y - (brushSize / 2); j <= pointPosition.Y + (brushSize / 2); j++)
                            {
                                float positionX = i;
                                float positionY = j;
                                if (Math.Pow((positionX - pointPosition.X), 2) + Math.Pow((positionY - pointPosition.Y), 2) <= Math.Pow(brushSize / 2, 2))
                                {
                                    BasicColor point = new BasicColor(color, new Vector2(positionX, positionY), new Vector2(1, 1));
                                    if (positionX - (int)Frame.position.X < 0 || positionY - (int)Frame.position.Y < 0 || positionX >= Frame.width + (int)Frame.position.X || positionY >= Frame.height + (int)Frame.position.Y) continue;
                                    layer[(int)positionX - (int)Frame.position.X, (int)positionY - (int)Frame.position.Y] = point;
                                }
                            }
                        }
                    }
                    break;
                case Shapes.RECTANGLE:
                    for (int i = -1 * (brushSize / 2); i < brushSize / 2; i++)
                    {
                        for (int j = -1 * (brushSize / 2); j < brushSize / 2; j++)
                        {
                            float positionX = pointPosition.X + i;
                            float positionY = pointPosition.Y + j;
                            BasicColor point = new BasicColor(color, new Vector2(positionX, positionY), new Vector2(1, 1));
                            if (positionX < 0 || positionY < 0 || positionX >= Frame.width || positionY >= Frame.height) return;
                            layer[(int)positionX, (int)positionY] = point;
                        }
                    }
                    break;
            }
        }
    }
}
