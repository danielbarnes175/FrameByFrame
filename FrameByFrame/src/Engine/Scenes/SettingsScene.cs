using System;
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
        private BasicTexture _paintBrush;
        private BasicTexture _exportButton;
        private BasicTexture _onion;
        private BasicTexture _backArrow;
        private BasicTexture _toggleSizeButton;
        private bool isExporting;

        public SettingsScene()
        {
            isExporting = false;
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
            _colors.Add(new BasicTexture("Static\\SettingsScene/Eraser", new Vector2(210, 90), pointDimensions));

            _onion = new BasicTexture("Static\\SettingsScene/Onion", new Vector2(210, 130), pointDimensions);
            _backArrow  = new BasicTexture("Static\\SettingsScene/Arrow_point-left", new Vector2(100, GlobalParameters.screenHeight - 75), new Vector2(96, 96));
            _toggleSizeButton = new BasicTexture("Static\\SettingsScene/button_change-size", new Vector2(GlobalParameters.screenWidth / 2, GlobalParameters.screenHeight - 30), new Vector2(120, 24)); 
        }

        public override void Update(GameTime gameTime)
        {
            if (GlobalParameters.GlobalKeyboard.GetPress("S"))
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Drawing Scene"];
            if (GlobalParameters.GlobalMouse.LeftClickHold())
            {
                CheckSelection();
            }

            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                Vector2 clickPosition = GlobalParameters.GlobalMouse.newMousePos;
                if (clickPosition.X > 195 && clickPosition.X < 215 && clickPosition.Y > 90 && clickPosition.Y < 120)
                {
                    DrawingScene scene = (DrawingScene)GlobalParameters.Scenes["Drawing Scene"];
                    scene.animation.isOnionSkinLoaded = !(scene.animation.isOnionSkinLoaded);
                }
                else if (clickPosition.X > 734 && clickPosition.X < 794 && clickPosition.Y > 830 && clickPosition.Y < 855)
                {
                    ((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).animation.brushSize -= 1;
                    if (((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).animation.brushSize <= 0) ((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).animation.brushSize = 1;
                }
                else if (clickPosition.X > 794 && clickPosition.X < 853 && clickPosition.Y > 830 && clickPosition.Y < 855)
                {
                    ((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).animation.brushSize += 1;
                    if (((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).animation.brushSize >= 30) ((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).animation.brushSize = 30;
                }
            }
            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            _exportButton.Draw(offset);
            _onion.Draw(offset);
            _backArrow.Draw(offset);
            _toggleSizeButton.Draw(offset);
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

           DrawingScene scene = (DrawingScene)GlobalParameters.Scenes["Drawing Scene"];
           string onionSkinEnabled = (scene.animation.isOnionSkinLoaded) ? "enabled" : "disabled";
           GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "Selected Color: " + GlobalParameters.CurrentColor.ToString(), new Vector2(230, 90), Color.Black);
           GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "Onion Skin " + onionSkinEnabled, new Vector2(230, 130), Color.Black);
           GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "Selected Layer: " + scene.animation.selectedLayer, new Vector2(1100, 150), Color.Black);
           
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, ((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).animation.brushSize.ToString(), new Vector2(GlobalParameters.screenWidth / 2 - 7, GlobalParameters.screenHeight - 60), Color.Black);
            if (isExporting)
            {
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "Exporting Animation...", new Vector2(GlobalParameters.screenWidth - 200, GlobalParameters.screenHeight - 90), Color.Black);
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
            else if (clickPosition.X > 190 && clickPosition.X < 220 && clickPosition.Y > 50 && clickPosition.Y < 80)
            {
                GlobalParameters.CurrentColor = Color.White;
            }
        }

        public void CheckLayer()
        {
            if (!GlobalParameters.GlobalMouse.LeftClickHold()) return;
            Vector2 clickPosition = GlobalParameters.GlobalMouse.newMousePos;
            DrawingScene scene = (DrawingScene)GlobalParameters.Scenes["Drawing Scene"];
            if (clickPosition.X > 1070 && clickPosition.X < 1220 && clickPosition.Y > 50 && clickPosition.Y < 100)
            {
                scene.animation.selectedLayer = "_layer1";
            }
            if (clickPosition.X > 1240 && clickPosition.X < 1390 && clickPosition.Y > 50 && clickPosition.Y < 100)
            {
                scene.animation.selectedLayer = "_layer2";
            }
            if (clickPosition.X > 1410 && clickPosition.X < 1560 && clickPosition.Y > 50 && clickPosition.Y < 100)
            {
                scene.animation.selectedLayer = "_layer3";
            }
        }

        public void CheckOtherButtons()
        {
            
            if (!GlobalParameters.GlobalMouse.LeftClickHold()) return;
            Vector2 clickPosition = GlobalParameters.GlobalMouse.newMousePos;
            if (clickPosition.X > 1410 && clickPosition.X < 1580 && clickPosition.Y > 810 && clickPosition.Y < 860)
            {
                isExporting = true;
            }
            else if (clickPosition.X > 55 && clickPosition.X < 130 && clickPosition.Y > 760 && clickPosition.Y < 835)
            {
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Drawing Scene"];
                DrawingScene scene = (DrawingScene)GlobalParameters.Scenes["Drawing Scene"];
                scene.loadedScene = false;
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
