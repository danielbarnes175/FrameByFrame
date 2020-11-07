using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Services;
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
            selectedLayer = "_layer1";
            frames = new List<Frame>();
            frames.Add(new Frame());

            Texture2D textureMenu = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 400, 800, pixel => Color.Orange, Shapes.RECTANGLE);
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
                int width = Frame.width;
                int height = Frame.height;

                Texture2D texture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, width, height, pixel => new Color(), Shapes.RECTANGLE);

                if (selectedLayer == "_layer1") frames[currentFrame]._layer1 = new BasicTexture(texture, new Vector2(width / 2, height / 2), new Vector2(width, height));
                if (selectedLayer == "_layer2") frames[currentFrame]._layer2 = new BasicTexture(texture, new Vector2(width / 2, height / 2), new Vector2(width, height));
                if (selectedLayer == "_layer3") frames[currentFrame]._layer3 = new BasicTexture(texture, new Vector2(width / 2, height / 2), new Vector2(width, height));
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
                Vector2 pointDimensions = new Vector2(brushSize, brushSize);

                if (pointPosition.X >= GlobalParameters.screenWidth - 437) return;

                Texture2D texture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, brushSize, brushSize, pixel => GlobalParameters.CurrentColor, Shapes.CIRCLE);
                BasicTexture point = new BasicTexture(texture, pointPosition, pointDimensions);

                BasicTexture layer = null;
                switch(selectedLayer)
                {
                    case "_layer1":
                        layer = frames[currentFrame]._layer1;
                        break;
                    case "_layer2":
                        layer = frames[currentFrame]._layer2;
                        break;
                    case "_layer3":
                        layer = frames[currentFrame]._layer3;
                        break;
                }

                List<BasicTexture> newLayer = new List<BasicTexture>
                {
                    layer,
                    point
                };

                Texture2D newLayerTexture = combineTextures(newLayer);

                switch (selectedLayer)
                {
                    case "_layer1":
                        frames[currentFrame]._layer1 = new BasicTexture(newLayerTexture, new Vector2(Frame.width / 2, Frame.height / 2), new Vector2(Frame.width, Frame.height));
                        break;
                    case "_layer2":
                        frames[currentFrame]._layer2 = new BasicTexture(newLayerTexture, new Vector2(Frame.width / 2, Frame.height / 2), new Vector2(Frame.width, Frame.height));
                        break;
                    case "_layer3":
                        frames[currentFrame]._layer3 = new BasicTexture(newLayerTexture, new Vector2(Frame.width / 2, Frame.height / 2), new Vector2(Frame.width, Frame.height));
                        break;
                }
            }
            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            GlobalParameters.GlobalGraphics.Clear(Color.Blue);

            if (!isPlaying && isOnionSkinLoaded)
            {
                DrawOnionSkin();
            }

            _sideMenu.Draw(offset);

            frames[currentFrame]._layer3.Draw(new Vector2(5, 25));
            frames[currentFrame]._layer2.Draw(new Vector2(5, 25));
            frames[currentFrame]._layer1.Draw(new Vector2(5, 25));

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
            if (currentFrame > totalFrames - 1)
                currentFrame = 0;
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
                DrawLayersWithOpacity(currentFrame - 3, 0.1f);
            }

            if (currentFrame - 2 >= 0)
            {
                DrawLayersWithOpacity(currentFrame - 2, 0.2f);
            }

            if (currentFrame - 1 >= 0)
            {
                DrawLayersWithOpacity(currentFrame - 1, 0.3f);
            }
        }

        private void DrawLayersWithOpacity(int frame, float opacity)
        {
            frames[frame]._layer3.Draw(new Vector2(5, 25), opacity);
            frames[frame]._layer2.Draw(new Vector2(5, 25), opacity);
            frames[frame]._layer1.Draw(new Vector2(5, 25), opacity);
        }

        // Combine multiple layers
        private RenderTarget2D combineTextures(Frame givenFrame)
        {
            RenderTarget2D renderTarget2D = new RenderTarget2D(GlobalParameters.GlobalGraphics, GlobalParameters.screenWidth - 222, GlobalParameters.screenHeight);

            // Set render target
            GlobalParameters.GlobalGraphics.SetRenderTarget(renderTarget2D);
            GlobalParameters.GlobalGraphics.Clear(Color.White);

            GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

            givenFrame._layer3.Draw(new Vector2(5, 25));
            givenFrame._layer2.Draw(new Vector2(5, 25));
            givenFrame._layer1.Draw(new Vector2(5, 25));

            GlobalParameters.GlobalSpriteBatch.End();

            // Unset render target
            GlobalParameters.GlobalGraphics.SetRenderTarget(null);

            return renderTarget2D;
        }

        // combine new point for single layer
        private RenderTarget2D combineTextures(List<BasicTexture> givenTextures)
        {
            RenderTarget2D renderTarget2D = new RenderTarget2D(GlobalParameters.GlobalGraphics, (int)givenTextures[0].dimensions.X, (int)givenTextures[0].dimensions.Y);

            // Set render target
            GlobalParameters.GlobalGraphics.SetRenderTarget(renderTarget2D);
            GlobalParameters.GlobalGraphics.Clear(new Color());

            GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            
            foreach (BasicTexture texture in givenTextures)
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
