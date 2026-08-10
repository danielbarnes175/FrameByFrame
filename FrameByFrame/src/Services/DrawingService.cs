using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine;

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
                    for (int pixel = 0; pixel < data.Length; pixel++)
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

        // Update SetColors to work with Color[] and width/height
        public static void SetColors(Color[] layerPixels, Texture2D texture, Vector2 pointPosition, Shapes shape, int brushSize, int width, int height, Color color)
        {
            int px = (int)pointPosition.X;
            int py = (int)pointPosition.Y;
            for (int dx = -brushSize; dx <= brushSize; dx++)
            {
                for (int dy = -brushSize; dy <= brushSize; dy++)
                {
                    int tx = px + dx;
                    int ty = py + dy;
                    if (tx >= 0 && tx < width && ty >= 0 && ty < height)
                    {
                        if (shape == Shapes.CIRCLE)
                        {
                            if (dx * dx + dy * dy <= brushSize * brushSize)
                            {
                                int idx = ty * width + tx;
                                layerPixels[idx] = color;
                            }
                        }
                        else if (shape == Shapes.RECTANGLE)
                        {
                            int idx = ty * width + tx;
                            layerPixels[idx] = color;
                        }
                    }
                }
            }
            texture.SetData(layerPixels);
        }

        public static RenderTarget2D CombineTextures(Frame givenFrame)
        {
            ArgumentNullException.ThrowIfNull(givenFrame);

            RenderTarget2D renderTarget = new RenderTarget2D(
                GlobalParameters.GlobalGraphics, givenFrame.width, givenFrame.height);
            RenderTargetBinding[] previousTargets = GlobalParameters.GlobalGraphics.GetRenderTargets();
            bool spriteBatchBegun = false;

            try
            {
                GlobalParameters.GlobalGraphics.SetRenderTarget(renderTarget);
                GlobalParameters.GlobalGraphics.Clear(Color.White);
                GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
                spriteBatchBegun = true;

                Rectangle canvasBounds = new Rectangle(0, 0, givenFrame.width, givenFrame.height);
                givenFrame.Draw(canvasBounds, 1.0f);
                givenFrame.DrawLayers(canvasBounds, 1.0f);

                GlobalParameters.GlobalSpriteBatch.End();
                spriteBatchBegun = false;
            }
            catch
            {
                renderTarget.Dispose();
                throw;
            }
            finally
            {
                try
                {
                    if (spriteBatchBegun) GlobalParameters.GlobalSpriteBatch.End();
                }
                finally
                {
                    GlobalParameters.GlobalGraphics.SetRenderTargets(previousTargets);
                }
            }

            return renderTarget;
        }
    }
}
