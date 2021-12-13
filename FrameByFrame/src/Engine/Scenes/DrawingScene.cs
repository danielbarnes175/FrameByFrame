﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.UI;
using FrameByFrame.src.UI.Components.Buttons;
using FrameByFrame.src.UI.Components.Buttons.Components;
using ImageMagick;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class DrawingScene : BaseScene
    {
        public static string selectedLayer;

        private List<Frame> frames;
        private List<UIElement> components;
        public int currentFrame;
        public int totalFrames;

        private bool isPlaying;
        private int timePlaying;
        public static int fps;
        public string projectName;
        public bool loadedScene;
        public bool isOnionSkinLoaded;
        public int brushSize;

        public DrawingTools drawingTool;

        public Vector2 frameSize;
        public Vector2 framePosition;

        public DrawingScene()
        {
            
            currentFrame = 0;
            totalFrames = 1;
            isPlaying = false;
            timePlaying = 0;
            fps = 4;
            loadedScene = false;
            brushSize = 15;
            drawingTool = DrawingTools.DRAW;

            isOnionSkinLoaded = true;
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            projectName = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public override void LoadContent()
        {
            selectedLayer = "_layer1";

            components = new List<UIElement>();
            Texture2D navbarBG = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, GlobalParameters.screenWidth, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(35, 35, 35), Shapes.RECTANGLE);
            Container navbar = new Container(navbarBG, new Vector2(0, 0), new Vector2(GlobalParameters.screenWidth, GlobalParameters.screenHeight * 0.05f));

            // Create Navbar child components
            Texture2D menuButtonTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 100, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(35, 35, 35), Shapes.RECTANGLE);
            RedirectButton menuButton = new RedirectButton("Menu Scene", menuButtonTexture, new Vector2(0, 0), new Vector2(100, (int)(GlobalParameters.screenHeight * 0.05f)), "MENU", Color.White);

            Texture2D helpButtonTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 50, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(100, 100, 200), Shapes.RECTANGLE);
            Overlay helpOverlay = new Overlay(helpButtonTexture, new Vector2(500, 500), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));
            PopupButton helpButton = new PopupButton(helpOverlay, helpButtonTexture, new Vector2(menuButton.position.X + menuButton.dimensions.X, 0), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));

            Texture2D settingsButtonTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 50, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(255, 255, 0), Shapes.RECTANGLE);
            Overlay settingsOverlay = new Overlay(settingsButtonTexture, new Vector2(600, 500), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));
            PopupButton settingsButton = new PopupButton(settingsOverlay, settingsButtonTexture, new Vector2(helpButton.position.X + helpButton.dimensions.X, 0), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));

            Texture2D colorButtonTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 50, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(200, 0, 255), Shapes.CIRCLE);
            Overlay colorOverlay = new Overlay(colorButtonTexture, new Vector2(1100, 500), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));
            PopupButton colorButton = new PopupButton(colorOverlay, colorButtonTexture, new Vector2(GlobalParameters.screenWidth - colorButtonTexture.Width, 0), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));

            Texture2D layerButtonTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 50, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(0, 0, 255), Shapes.RECTANGLE);
            Overlay layerOverlay = new Overlay(layerButtonTexture, new Vector2(750, 500), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));
            PopupButton layerButton = new PopupButton(layerOverlay, layerButtonTexture, new Vector2(colorButton.position.X - colorButton.dimensions.X, 0), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));

            List<RadioButton> buttons = new List<RadioButton>();

            Texture2D eraserSelectedTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 50, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(255, 0, 0), Shapes.RECTANGLE);
            Texture2D eraserUnselectedTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 50, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(0, 255, 0), Shapes.RECTANGLE);
            Texture2D drawSelectedTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 50, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(255, 255, 0), Shapes.RECTANGLE);
            Texture2D drawUnselectedTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 50, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(0, 255, 255), Shapes.RECTANGLE);

            EraserButton eraser = new EraserButton(eraserSelectedTexture, eraserUnselectedTexture, false, new Vector2(layerButton.position.X - layerButton.dimensions.X, 0), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));
            DrawButton draw = new DrawButton(drawSelectedTexture, drawUnselectedTexture, true, new Vector2(eraser.position.X - eraser.dimensions.X, 0), new Vector2(50, (int)(GlobalParameters.screenHeight * 0.05f)));

            buttons.Add(draw);
            buttons.Add(eraser);

            ButtonGroup toolButtons = new ButtonGroup(buttons);
            // TODO ADD Clickable Buttons
            // +1 Frame, Last Frame, -1 Frame, First Frame, Play

            Texture2D frameCounterTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 150, (int)(GlobalParameters.screenHeight * 0.05f), pixel => new Color(0, 0, 0), Shapes.RECTANGLE);
            UIElement frameCounter = new FrameCounterComponent(frameCounterTexture, new Vector2(settingsButton.position.X + settingsButton.dimensions.X, 0), new Vector2(frameCounterTexture.Width, frameCounterTexture.Height));

            navbar.uiElements.Add(menuButton);
            navbar.uiElements.Add(helpButton);
            navbar.uiElements.Add(settingsButton);
            navbar.uiElements.Add(colorButton);
            navbar.uiElements.Add(layerButton);
            navbar.uiElements.Add(frameCounter);
            navbar.buttonGroups.Add(toolButtons);

            // Add the navbar to this scene
            components.Add(navbar);

            // Load Frame
            frames = new List<Frame>();
            frameSize = new Vector2(200, 200);
            framePosition = new Vector2(GlobalParameters.screenWidth / 2 - (int)frameSize.X / 2, GlobalParameters.screenHeight / 2 - (int)frameSize.Y / 2 + menuButton.dimensions.Y / 2);
            frames.Add(new Frame(framePosition, frameSize));
        }

        public override void Update(GameTime gameTime)
        {
            handleKeyboardShortcuts();
            handleIsPlaying(gameTime);
            handleMouseShortcuts();

            foreach (UIElement element in components)
            {
                element.Update();
            }

            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            GlobalParameters.GlobalGraphics.Clear(new Color(45, 45, 45));

            drawCurrentFrame();

            foreach (UIElement element in components)
            {
                element.Draw(offset, new Vector2(0, 0));
            }

            /*
            _sideMenu.Draw(offset);
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
           
            */
            base.Draw(offset);
        }


        private void handleMouseShortcuts()
        {
            // So we don't start drawing until we load the scene
            if (!GlobalParameters.GlobalMouse.LeftClickHold() && !loadedScene)
            {
                loadedScene = true;
                return;
            }

            // Draw on current frame
            if (GlobalParameters.GlobalMouse.LeftClickHold() && loadedScene)
            {
                BasicColor[,] layer = null;
                switch (selectedLayer)
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

                Vector2 mousePositionCur = GlobalParameters.GlobalMouse.newMousePos;
                Vector2 mousePositionOld = GlobalParameters.GlobalMouse.oldMousePos;
                int numInterpolations = 300;

                for (int i = 0; i < numInterpolations; i++)
                {
                    float newX = (i * ((mousePositionCur.X - mousePositionOld.X) / numInterpolations)) + mousePositionOld.X - framePosition.X;
                    float newY = (i * ((mousePositionCur.Y - mousePositionOld.Y) / numInterpolations)) + mousePositionOld.Y - framePosition.Y;
                    
                    if (newX >= Frame.width + framePosition.X || newX <= 0 || newY <= 0 || newY >= Frame.height + framePosition.Y) return;
                    Vector2 pointPosition = new Vector2(newX + framePosition.X, newY + framePosition.Y);

                    DrawingService.SetColors(layer, frames[currentFrame].colors[0], pointPosition, Shapes.CIRCLE, brushSize);
                }
            }
        }

        private void handleIsPlaying(GameTime gameTime)
        {

            if (isPlaying)
            {
                timePlaying += 1;
                Animate(gameTime);
                return;
            }
        }

        private void drawCurrentFrame()
        {
            frames[currentFrame].Draw(1.0f);

            if (!isPlaying && isOnionSkinLoaded)
            {
                DrawOnionSkin();
            }

            for (int i = 0; i < frameSize.X; i++)
            {
                for (int j = 0; j < frameSize.Y; j++)
                {
                    if (frames[currentFrame]._layer3[i, j] != null)
                        frames[currentFrame]._layer3[i, j].Draw(1.0f);
                    if (frames[currentFrame]._layer2[i, j] != null)
                        frames[currentFrame]._layer2[i, j].Draw(1.0f);
                    if (frames[currentFrame]._layer1[i, j] != null)
                        frames[currentFrame]._layer1[i, j].Draw(1.0f);
                }
            }
        }

        public void handleKeyboardShortcuts()
        {
            // Save current frame as png
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("T"))
            {
                RenderTarget2D texture = combineTextures(frames[currentFrame]);
                SaveTextureAsPng("drawing" + currentFrame + ".png", texture);
            }

            // Exit drawing
            if (GlobalParameters.GlobalKeyboard.GetPress("ESC"))
            {
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Menu Scene"];

                // Reset all values again, basically calling the constructor as if it were new.
                selectedLayer = "_layer1";
                frames = new List<Frame>();
                frames.Add(new Frame(framePosition, frameSize));
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

            // Delete current frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("DELETE"))
            {
                deleteFrame(frames, currentFrame);
            }

            // Toggle animation playing
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("P"))
            {
                isPlaying = !isPlaying;
                timePlaying = 0;
            }

            // Switch to settings scene
            if (GlobalParameters.GlobalKeyboard.GetPress("W"))
            {
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Settings Scene"];
                isPlaying = false;
            }

            // Erase current layer
            if (GlobalParameters.GlobalKeyboard.GetPress("BACKSPACE"))
            {
                if (selectedLayer == "_layer1") frames[currentFrame]._layer1 = new BasicColor[Frame.width, Frame.height];
                if (selectedLayer == "_layer2") frames[currentFrame]._layer2 = new BasicColor[Frame.width, Frame.height];
                if (selectedLayer == "_layer3") frames[currentFrame]._layer3 = new BasicColor[Frame.width, Frame.height];
            }

            // Toggle Onion Skin
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("O"))
                isOnionSkinLoaded = !isOnionSkinLoaded;

            // Load next frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("M"))
            {
                currentFrame += 1;
                if (currentFrame > totalFrames - 1)
                {
                    frames.Add(new Frame(framePosition, frameSize));
                    totalFrames += 1;
                }
            }

            // Load previous frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("N"))
            {
                if (currentFrame > 0)
                {
                    currentFrame -= 1;
                }
            }

            // Insert a frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("B"))
            {
                frames = InsertFrame(frames, currentFrame);
                totalFrames += 1;
            }
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
                    newFrames.Add(new Frame(framePosition, frameSize));
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
            for (int i = 0; i < frameSize.X; i++)
            {
                for (int j = 0; j < frameSize.Y; j++)
                {
                    if (frames[frame]._layer3[i, j] != null)
                        frames[frame]._layer3[i, j].Draw(opacity);
                    if (frames[frame]._layer2[i, j] != null)
                        frames[frame]._layer2[i, j].Draw(opacity);
                    if (frames[frame]._layer1[i, j] != null)
                        frames[frame]._layer1[i, j].Draw(opacity);
                }
            }
        }

        // Combine multiple layers
        private RenderTarget2D combineTextures(Frame givenFrame)
        {
            RenderTarget2D renderTarget2D = new RenderTarget2D(GlobalParameters.GlobalGraphics, GlobalParameters.screenWidth - 222, GlobalParameters.screenHeight);

            // Set render target
            GlobalParameters.GlobalGraphics.SetRenderTarget(renderTarget2D);
            GlobalParameters.GlobalGraphics.Clear(Color.White);

            GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

            for (int i = 0; i < frameSize.X; i++)
            {
                for (int j = 0; j < frameSize.Y; j++)
                {
                    if (frames[currentFrame]._layer3[i, j] != null)
                        givenFrame._layer3[i, j].Draw(1.0f);
                    if (frames[currentFrame]._layer2[i, j] != null)
                        givenFrame._layer2[i, j].Draw(1.0f);
                    if (frames[currentFrame]._layer1[i, j] != null)
                        givenFrame._layer1[i, j].Draw(1.0f);
                }
            }

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

        public void setDrawTool()
        {
            // drawToolTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, brushSize, brushSize, pixel => GlobalParameters.CurrentColor, Shapes.CIRCLE);
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
