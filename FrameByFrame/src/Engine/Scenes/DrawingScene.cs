using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using ImageMagick;
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
        public string projectName;
        public bool loadedScene;
        public bool isOnionSkinLoaded;
        public int brushSize;

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
            loadedScene = false;
            brushSize = 15;

            isOnionSkinLoaded = true;
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            projectName = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public override void LoadContent()
        {
            Texture2D textureMenu = CreateTexture(GlobalParameters.GlobalGraphics, 400, 800, pixel => Color.Orange);
            Vector2 menuDimensions = new Vector2(400, 800);
            _sideMenu = new BasicTexture(textureMenu, new Vector2(GlobalParameters.screenWidth - 225, GlobalParameters.screenHeight / 2), menuDimensions);
        }

        public override void Update(GameTime gameTime)
        {
            if (!GlobalParameters.GlobalMouse.LeftClickHold())
            {
                loadedScene = true;
            }
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("T"))
            {
                RenderTarget2D texture = combineTextures(frames[currentFrame]);
                SaveTextureAsPng("drawing" + currentFrame + ".png", texture);
            }

            if (GlobalParameters.GlobalKeyboard.GetPress("ESC"))
            {
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Menu Scene"];

                // Reset all values again, basically calling the constructor as if it were new.'
                selectedLayer = "_layer1";
                frames = new List<Frame>();
                frames.Add(new Frame());
                currentFrame = 0;
                totalFrames = 1;
                isPlaying = false;
                timePlaying = 0;
                fps = 4;
                loadedScene = false;
                brushSize = 15;

                isOnionSkinLoaded = true;
                Random random = new Random();
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                projectName = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            }

            if (GlobalParameters.GlobalKeyboard.GetPressSingle("DELETE"))
            {
                deleteFrame(frames, currentFrame);
            }

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

            if (GlobalParameters.GlobalKeyboard.GetPress("BACKSPACE"))
            {
                if (selectedLayer == "_layer1") frames[currentFrame]._layer1 = new List<BasicTexture>();
                if (selectedLayer == "_layer2") frames[currentFrame]._layer2 = new List<BasicTexture>();
                if (selectedLayer == "_layer3") frames[currentFrame]._layer3 = new List<BasicTexture>();
            }

            if (GlobalParameters.GlobalKeyboard.GetPressSingle("O"))
                isOnionSkinLoaded = !isOnionSkinLoaded;

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

            if (GlobalParameters.GlobalKeyboard.GetPressSingle("B"))
            {
                frames = InsertFrame(frames, currentFrame);
                totalFrames += 1;
            }
            if (GlobalParameters.GlobalMouse.LeftClickHold() && loadedScene)
            {
                Vector2 pointPosition = GlobalParameters.GlobalMouse.newMousePos;
                if (pointPosition.X >= GlobalParameters.screenWidth - 437) return;
                Vector2 pointDimensions = new Vector2(brushSize, brushSize);

                Texture2D texture = CreateTexture(GlobalParameters.GlobalGraphics, brushSize, brushSize, pixel => GlobalParameters.CurrentColor);
                BasicTexture point = new BasicTexture(texture, pointPosition, pointDimensions);

                if (selectedLayer == "_layer1") frames[currentFrame]._layer1.Add(point);
                if (selectedLayer == "_layer2") frames[currentFrame]._layer2.Add(point);
                if (selectedLayer == "_layer3") frames[currentFrame]._layer3.Add(point);
            }
            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            
            if (!isPlaying && isOnionSkinLoaded)
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

            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, currentFrame + 1 + " / " + totalFrames, new Vector2(GlobalParameters.screenWidth - 225, GlobalParameters.screenHeight / 4 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "Controls: ", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 20 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"M\" - Next Frame", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 40 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"N\" - Previous Frame", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 60 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"B\" - Insert a New Frame Before the Current Frame", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 80 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"P\" - Play/Pause Animation", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 100 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"W\" - Open Settings Menu", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 120 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"S\" - Close Settings Menu", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 140 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"O\" - Toggle Onion Skin", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 160 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"ESC\" - Return to Main Menu WITHOUT Saving", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 180 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"DELETE\" - Delete the Current Frame", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 200 - 150), Color.Black);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "\"BACKSPACE\" - Delete the Current Layer", new Vector2(GlobalParameters.screenWidth - 400, GlobalParameters.screenHeight / 4 + 220 - 150), Color.Black);
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

        private void deleteFrame(List<Frame> givenFrames, int givenCurrentFrame)
        {
            if (totalFrames == 1) return;

            List<Frame> newFrames = new List<Frame>();
            for (int i = 0; i < frames.Count; i++)
            {
                if (i != givenCurrentFrame)
                {
                    newFrames.Add(givenFrames[i]);
                }
            }

            frames = newFrames;
            totalFrames = frames.Count;
        }

        private List<Frame> InsertFrame(List<Frame> givenFrames, int givenCurrentFrame)
        {
            List<Frame> newFrames = new List<Frame>();

            for (int i = 0; i < givenFrames.Count; i++)
            {
                if (i == givenCurrentFrame)
                {
                    newFrames.Add(new Frame());
                    newFrames.Add(givenFrames[i]);
                }
                else
                {
                    newFrames.Add(givenFrames[i]);
                }

            }

            return newFrames;
        }

        public void DrawOnionSkin()
        {
            if (currentFrame - 3 >= 0)
            {
                DrawLayersWithOpacity(currentFrame - 3, 0.01f);
            }

            if (currentFrame - 2 >= 0)
            {
                DrawLayersWithOpacity(currentFrame - 2, 0.05f);
            }

            if (currentFrame - 1 >= 0)
            {
                DrawLayersWithOpacity(currentFrame - 1, 0.1f);
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

        private RenderTarget2D combineTextures(Frame givenFrame)
        {
            RenderTarget2D renderTarget2D = new RenderTarget2D(GlobalParameters.GlobalGraphics, GlobalParameters.screenWidth - 222, GlobalParameters.screenHeight);

            // Set render target
            GlobalParameters.GlobalGraphics.SetRenderTarget(renderTarget2D);
            GlobalParameters.GlobalGraphics.Clear(Color.White);
            // Render your tiles as usual, this is just an example
            GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

            foreach (BasicTexture texture in givenFrame._layer3)
            {
                texture.Draw(Vector2.Zero);
            }
            foreach (BasicTexture texture in givenFrame._layer2)
            {
                texture.Draw(Vector2.Zero);
            }
            foreach (BasicTexture texture in givenFrame._layer1)
            {
                texture.Draw(Vector2.Zero);
            }
            GlobalParameters.GlobalSpriteBatch.End();

            // Unset render target
            GlobalParameters.GlobalGraphics.SetRenderTarget(null);

            return renderTarget2D;
        }

        public void ExportAnimation()
        {
            for (int i = 0; i < frames.Count; i++)
            {
                RenderTarget2D texture = combineTextures(frames[i]);
                System.IO.Directory.CreateDirectory("Projects/" + projectName);
                SaveTextureAsPng("Projects/" + projectName + "/Frame_" + i + ".png", texture);
            }

            CreateGif("Projects/" + projectName + "/_" + projectName + ".gif");
            Console.WriteLine("Exported Animation as GIF to " + Directory.GetCurrentDirectory() + "Projects/" + projectName);
        }

        private void SaveTextureAsPng(string filename, RenderTarget2D texture)
        {
            FileStream setStream = File.Open(filename, FileMode.Create);
            StreamWriter writer = new StreamWriter(setStream);
            texture.SaveAsPng(setStream, texture.Width, texture.Height);
            setStream.Dispose();
            
        }

        private void CreateGif(string filename)
        {
            using (MagickImageCollection collection = new MagickImageCollection())
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    collection.Add("Projects/" + projectName + "/Frame_" + i + ".png");
                    collection[0].AnimationDelay = 8;
                }

                collection.Write(filename);
            }
        }
    }
}
