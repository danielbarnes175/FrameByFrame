using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class DrawingScene : BaseScene
    {
        public static string selectedLayer;

        private BasicTexture _sideMenu;
        private List<Frame> frames;
        private int currentFrame;
        private int totalFrames;

        private bool isPlaying;
        private int timePlaying;
        public static int fps;

        public DrawingScene()
        {
            selectedLayer = "_layer1";
            frames = new List<Frame>();
            frames.Add(new Frame());
            currentFrame = 0;
            totalFrames = 1;
            isPlaying = false;
            timePlaying = 0;
            fps = 4;
        }

        public override void LoadContent()
        {
            Texture2D textureMenu = CreateTexture(GlobalParameters.GlobalGraphics, 400, 800, pixel => Color.Orange);
            Vector2 menuDimensions = new Vector2(400, 800);
            _sideMenu = new BasicTexture(textureMenu, new Vector2(GlobalParameters.screenWidth - 225, GlobalParameters.screenHeight / 2), menuDimensions);
        }

        public override void Update(GameTime gameTime)
        {
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("T"))
            {
                RenderTarget2D texture = combineTextures();
                SaveTextureAsPng("test" + currentFrame + ".png", texture);
            }
            if (GlobalParameters.GlobalKeyboard.GetPress("ESC"))
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Menu Scene"];
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("P"))
            {
                isPlaying = !isPlaying;
                timePlaying = 0;
            }

            if (GlobalParameters.GlobalKeyboard.GetPress("W"))
            {
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Settings Scene"];
                isPlaying = false;
            }

            if (isPlaying)
            {
                timePlaying += 1;
                Animate(gameTime);
                return;
            }
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("M"))
            {
                currentFrame += 1;
                if (currentFrame > totalFrames - 1)
                {
                    frames.Add(new Frame());
                    totalFrames += 1;
                }
            }

            if (GlobalParameters.GlobalKeyboard.GetPressSingle("N"))
            {
                if (currentFrame > 0)
                {
                    currentFrame -= 1;
                }
            }
            if (GlobalParameters.GlobalMouse.LeftClickHold())
            {
                Vector2 pointPosition = GlobalParameters.GlobalMouse.newMousePos;
                if (pointPosition.X >= GlobalParameters.screenWidth - 437) return;
                Vector2 pointDimensions = new Vector2(15, 15);

                Texture2D texture = CreateTexture(GlobalParameters.GlobalGraphics, 15, 15, pixel => GlobalParameters.CurrentColor);
                BasicTexture point = new BasicTexture(texture, pointPosition, pointDimensions);

                if (selectedLayer == "_layer1") frames[currentFrame]._layer1.Add(point);
                if (selectedLayer == "_layer2") frames[currentFrame]._layer2.Add(point);
                if (selectedLayer == "_layer3") frames[currentFrame]._layer3.Add(point);
            }
            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            if (!isPlaying)
            {
                DrawOnionSkin();
            }

            _sideMenu.Draw(offset);
            foreach (BasicTexture point in frames[currentFrame]._layer3)
            {
                point.Draw(new Vector2(5, 25));
            }

            foreach (BasicTexture point in frames[currentFrame]._layer2)
            {
                point.Draw(new Vector2(5, 25));
            }

            foreach (BasicTexture point in frames[currentFrame]._layer1)
            {
                point.Draw(new Vector2(5, 25));
            }
            base.Draw(offset);
        }

        public void Animate(GameTime gameTime)
        {
            if (timePlaying % fps != 0) return;
            currentFrame += 1;
            if (currentFrame > totalFrames-1)
                currentFrame = 0;
        }

        public static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint)
        {
            //initialize a texture
            Texture2D texture = new Texture2D(device, width, height);

            //the array holds the color for each pixel in the texture
            Color[] data = new Color[width * height];
            for (int pixel = 0; pixel < data.Count(); pixel++)
            {
                //the function applies the color according to the specified pixel
                data[pixel] = paint(pixel);
            }

            //set the color
            texture.SetData(data);

            return texture;
        }

        public void DrawOnionSkin()
        {
            if (currentFrame - 3 >= 0)
            {
                DrawLayersWithOpacity(currentFrame - 3, 0.1f);
            }

            if (currentFrame - 2 >= 0)
            {
                DrawLayersWithOpacity(currentFrame - 2, 0.15f);
            }

            if (currentFrame - 1 >= 0)
            {
                DrawLayersWithOpacity(currentFrame - 1, 0.25f);
            }
        }

        private void DrawLayersWithOpacity(int frame, float opacity)
        {
            foreach (BasicTexture point in frames[frame]._layer3)
            {
                point.Draw(new Vector2(5, 25), opacity);
            }

            foreach (BasicTexture point in frames[frame]._layer2)
            {
                point.Draw(new Vector2(5, 25), opacity);
            }

            foreach (BasicTexture point in frames[frame]._layer1)
            {
                point.Draw(new Vector2(5, 25), opacity);
            }
        }

        private RenderTarget2D combineTextures()
        {
            RenderTarget2D renderTarget2D = new RenderTarget2D(GlobalParameters.GlobalGraphics, GlobalParameters.screenWidth - 222, GlobalParameters.screenHeight);

            // Set render target
            GlobalParameters.GlobalGraphics.SetRenderTarget(renderTarget2D);
            GlobalParameters.GlobalGraphics.Clear(Color.White);
            // Render your tiles as usual, this is just an example
            GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

            foreach (BasicTexture texture in frames[currentFrame]._layer3)
            {
                texture.Draw(Vector2.Zero);
            }
            foreach (BasicTexture texture in frames[currentFrame]._layer2)
            {
                texture.Draw(Vector2.Zero);
            }
            foreach (BasicTexture texture in frames[currentFrame]._layer1)
            {
                texture.Draw(Vector2.Zero);
            }
            GlobalParameters.GlobalSpriteBatch.End();

            // Unset render target
            GlobalParameters.GlobalGraphics.SetRenderTarget(null);

            return renderTarget2D;
        }

        private void SaveTextureAsPng(string filename, RenderTarget2D texture)
        {
            FileStream setStream = File.Open(filename, FileMode.Create);
            StreamWriter writer = new StreamWriter(setStream);
            texture.SaveAsPng(setStream, texture.Width, texture.Height);
            setStream.Dispose();
            Console.WriteLine("Saved Texture as PNG to " + Directory.GetCurrentDirectory());
        }
    }
}
