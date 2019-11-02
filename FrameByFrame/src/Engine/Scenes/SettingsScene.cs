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
        private BasicTexture _colorOutline;

        public SettingsScene()
        {
            _textures = new List<BasicTexture>();
            _colors = new List<BasicTexture>();
        }

        public override void LoadContent()
        {


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

        public override void Update()
        {
            if (GlobalParameters.GlobalKeyboard.GetPress("S"))
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Drawing Scene"];
            CheckColors();
            base.Update();
        }

        public override void Draw(Vector2 offset)
        {
           foreach (BasicTexture texture in _textures)
           {
               texture.Draw(offset);
           }

           foreach (BasicTexture color in _colors)
           {
               color.Draw(offset);
           }
           base.Draw(offset);
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
