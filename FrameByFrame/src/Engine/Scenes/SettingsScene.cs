﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class SettingsScene : BaseScene
    {
        private List<BasicTexture> _textures;
        private List<BasicTexture> _colors;
        private List<BasicTexture> _layerButtons;
        private BasicTexture _colorOutline;
        private BasicTexture _paintBrush;
        private BasicTexture _exportButton;

        public SettingsScene()
        {
            _textures = new List<BasicTexture>();
            _colors = new List<BasicTexture>();
            _layerButtons = new List<BasicTexture>();
        }

        public override void LoadContent()
        {
            _paintBrush = new BasicTexture("Static\\SettingsScene/image_color-select", new Vector2(100, 40), new Vector2(64, 64));
            _textures.Add(_paintBrush);

            _layerButtons.Add(new BasicTexture("Static\\SettingsScene/button_layer-1", new Vector2(1150, 100), new Vector2(150, 50)));
            _layerButtons.Add(new BasicTexture("Static\\SettingsScene/button_layer-2", new Vector2( 1320, 100), new Vector2(150, 50)));
            _layerButtons.Add(new BasicTexture("Static\\SettingsScene/button_layer-3", new Vector2(1490, 100), new Vector2(150, 50)));

            _exportButton = new BasicTexture("Static\\SettingsScene/button_export", new Vector2(GlobalParameters.screenWidth - 100, GlobalParameters.screenHeight - 40), new Vector2(167, 50));

            Texture2D textureBlack = CreateTexture(GlobalParameters.GlobalGraphics, 30, 30, pixel => Color.Black);
            Texture2D textureRed = CreateTexture(GlobalParameters.GlobalGraphics, 30, 30, pixel => Color.Red);
            Texture2D textureBlue = CreateTexture(GlobalParameters.GlobalGraphics, 30, 30, pixel => Color.Blue);
            Texture2D textureGreen = CreateTexture(GlobalParameters.GlobalGraphics, 30, 30, pixel => Color.Green);
            Texture2D textureYellow = CreateTexture(GlobalParameters.GlobalGraphics, 30, 30, pixel => Color.Yellow);

            Vector2 pointDimensions = new Vector2(30, 30);

            _colors.Add(new BasicTexture(textureBlack, new Vector2(30, 90), pointDimensions));
            _colors.Add(new BasicTexture(textureRed, new Vector2(65, 90), pointDimensions));
            _colors.Add(new BasicTexture(textureBlue, new Vector2(100, 90), pointDimensions));
            _colors.Add(new BasicTexture(textureGreen, new Vector2(135, 90), pointDimensions));
            _colors.Add(new BasicTexture(textureYellow, new Vector2(170, 90), pointDimensions));
        }

        public override void Update(GameTime gameTime)
        {
            if (GlobalParameters.GlobalKeyboard.GetPress("S"))
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Drawing Scene"];
            if (GlobalParameters.GlobalMouse.LeftClickHold())
            {
                CheckSelection();
            }
            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            _exportButton.Draw(offset);
           foreach (BasicTexture texture in _textures)
           {
               texture.Draw(offset);
           }

           foreach (BasicTexture color in _colors)
           {
               color.Draw(offset);
           }

           foreach (BasicTexture buttons in _layerButtons)
           {
               buttons.Draw(offset);
           }
           base.Draw(offset);
        }

        public void CheckSelection()
        {
            CheckLayer();
            CheckColors();
            CheckOtherButtons();
        }

        public void CheckColors()
        {
            if (!GlobalParameters.GlobalMouse.LeftClickHold()) return;
            Vector2 clickPosition = GlobalParameters.GlobalMouse.newMousePos;
            if (clickPosition.X > 10 && clickPosition.X < 40 && clickPosition.Y > 50 && clickPosition.Y < 80)
            {
                GlobalParameters.CurrentColor = Color.Black;
            }
            else if (clickPosition.X > 45 && clickPosition.X < 75 && clickPosition.Y > 50 && clickPosition.Y < 80)
            {
                GlobalParameters.CurrentColor = Color.Red;
            }
            else if (clickPosition.X > 80 && clickPosition.X < 110 && clickPosition.Y > 50 && clickPosition.Y < 80)
            {
                GlobalParameters.CurrentColor = Color.Blue;
            }
            else if (clickPosition.X > 115 && clickPosition.X < 145 && clickPosition.Y > 50 && clickPosition.Y < 80)
            {
                GlobalParameters.CurrentColor = Color.Green;
            }
            else if (clickPosition.X > 150 && clickPosition.X < 180 && clickPosition.Y > 50 && clickPosition.Y < 80)
            {
                GlobalParameters.CurrentColor = Color.Yellow;
            }
        }

        public void CheckLayer()
        {
            if (!GlobalParameters.GlobalMouse.LeftClickHold()) return;
            Vector2 clickPosition = GlobalParameters.GlobalMouse.newMousePos;
            if (clickPosition.X > 1070 && clickPosition.X < 1220 && clickPosition.Y > 50 && clickPosition.Y < 100)
            {
                DrawingScene.selectedLayer = "_layer1";
            }
            if (clickPosition.X > 1240 && clickPosition.X < 1390 && clickPosition.Y > 50 && clickPosition.Y < 100)
            {
                DrawingScene.selectedLayer = "_layer2";
            }
            if (clickPosition.X > 1410 && clickPosition.X < 1560 && clickPosition.Y > 50 && clickPosition.Y < 100)
            {
                DrawingScene.selectedLayer = "_layer3";
            }
        }

        public void CheckOtherButtons()
        {
            
            if (!GlobalParameters.GlobalMouse.LeftClickHold()) return;
            Vector2 clickPosition = GlobalParameters.GlobalMouse.newMousePos;
            if (clickPosition.X > 1410 && clickPosition.X < 1580 && clickPosition.Y > 810 && clickPosition.Y < 860)
            {
                DrawingScene scene = (DrawingScene)GlobalParameters.Scenes["Drawing Scene"];
                scene.ExportAnimation();
            }
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
        public static Texture2D CreateTextureOutline(GraphicsDevice device, int width, int height, Func<int, Color> paint)
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
    }
}
