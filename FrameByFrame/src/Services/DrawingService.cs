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
        private static readonly Color CheckerLight = new(238, 238, 238);
        private static readonly Color CheckerDark = new(204, 204, 204);

        public static Color CheckerboardColor(int x, int y, int tileSize = 8) =>
            ((x / Math.Max(1, tileSize)) + (y / Math.Max(1, tileSize))) % 2 == 0
                ? CheckerLight
                : CheckerDark;

        public static void DrawCanvasBackground(Rectangle bounds, bool isTransparent, Color backgroundColor)
        {
            Texture2D pixel = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, Color.White, 1);
            if (!isTransparent)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(pixel, bounds, backgroundColor);
                return;
            }

            const int tileSize = 12;
            for (int y = bounds.Y; y < bounds.Bottom; y += tileSize)
            for (int x = bounds.X; x < bounds.Right; x += tileSize)
            {
                Rectangle tile = new(x, y, Math.Min(tileSize, bounds.Right - x),
                    Math.Min(tileSize, bounds.Bottom - y));
                GlobalParameters.GlobalSpriteBatch.Draw(pixel, tile,
                    CheckerboardColor((x - bounds.X) / tileSize, (y - bounds.Y) / tileSize, 1));
            }
        }

        public static void DrawCanvasBackground(CanvasView view, bool isTransparent, Color backgroundColor)
        {
            ArgumentNullException.ThrowIfNull(view);
            Texture2D pixel = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, Color.White, 1);
            if (!isTransparent)
            {
                DrawCanvasTile(pixel, view, 0, 0, view.CanvasWidth, view.CanvasHeight, backgroundColor);
                return;
            }

            const int tileSize = 24;
            for (int y = 0; y < view.CanvasHeight; y += tileSize)
            for (int x = 0; x < view.CanvasWidth; x += tileSize)
            {
                int width = Math.Min(tileSize, view.CanvasWidth - x);
                int height = Math.Min(tileSize, view.CanvasHeight - y);
                DrawCanvasTile(pixel, view, x, y, width, height,
                    CheckerboardColor(x / tileSize, y / tileSize, 1));
            }
        }

        public static void DrawCanvasOutline(CanvasView view, Color color, float thickness = 2f)
        {
            ArgumentNullException.ThrowIfNull(view);
            Texture2D pixel = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, Color.White, 1);
            float canvasThickness = Math.Max(1f / view.Scale, thickness / view.Scale);
            DrawCanvasTile(pixel, view, 0, 0, view.CanvasWidth, canvasThickness, color);
            DrawCanvasTile(pixel, view, 0, view.CanvasHeight - canvasThickness,
                view.CanvasWidth, canvasThickness, color);
            DrawCanvasTile(pixel, view, 0, 0, canvasThickness, view.CanvasHeight, color);
            DrawCanvasTile(pixel, view, view.CanvasWidth - canvasThickness, 0,
                canvasThickness, view.CanvasHeight, color);
        }

        private static void DrawCanvasTile(Texture2D pixel, CanvasView view, float x, float y,
            float width, float height, Color color)
        {
            Vector2 center = view.CanvasToScreen(new Vector2(x + width / 2f, y + height / 2f));
            GlobalParameters.GlobalSpriteBatch.Draw(pixel, center, null, color, view.RotationRadians,
                new Vector2(.5f, .5f), new Vector2(width * view.Scale, height * view.Scale),
                SpriteEffects.None, 0f);
        }

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

        public static RenderTarget2D CombineTextures(Animation.Animation animation, Frame givenFrame,
            Color? backgroundOverride = null)
        {
            ArgumentNullException.ThrowIfNull(animation);
            ArgumentNullException.ThrowIfNull(givenFrame);

            RenderTarget2D renderTarget = new RenderTarget2D(
                GlobalParameters.GlobalGraphics, givenFrame.width, givenFrame.height);
            RenderTargetBinding[] previousTargets = GlobalParameters.GlobalGraphics.GetRenderTargets();
            bool spriteBatchBegun = false;

            try
            {
                GlobalParameters.GlobalGraphics.SetRenderTarget(renderTarget);
                GlobalParameters.GlobalGraphics.Clear(backgroundOverride ??
                    (animation.IsCanvasBackgroundTransparent ? Color.Transparent : animation.CanvasBackgroundColor));
                GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
                spriteBatchBegun = true;

                Rectangle canvasBounds = new Rectangle(0, 0, givenFrame.width, givenFrame.height);
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
