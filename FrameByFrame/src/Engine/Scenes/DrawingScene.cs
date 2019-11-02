using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class DrawingScene : BaseScene
    {
        private List<BasicTexture> _textures;

        private Color _currentColor;

        public DrawingScene()
        {
            _textures = new List<BasicTexture>();
            _currentColor = Color.Black;
        }

        public override void LoadContent()
        {

        }

        public override void Update()
        {
            if (GlobalParameters.GlobalMouse.LeftClickHold())
            {
                Random random = new Random();

                Texture2D texture = CreateTexture(GlobalParameters.GlobalGraphics, 15, 15, pixel => _currentColor);

                // Vector2 oldPosition = GlobalParameters.GlobalMouse.oldMousePos;
                Vector2 pointPosition = GlobalParameters.GlobalMouse.newMousePos;
                // Vector2 currentPosition = pointPosition;
                Vector2 pointDimensions = new Vector2(15, 15);

                BasicTexture point = new BasicTexture(texture, pointPosition, pointDimensions);
                _textures.Add(point);
            }

            if (GlobalParameters.GlobalKeyboard.GetPress("P"))
            {
                System.Environment.Exit(0);
            }
            base.Update();
        }

        public override void Draw(Vector2 offset)
        {
            foreach (BasicTexture point in _textures)
            {
                point.Draw(new Vector2(5, 25));
            }
            base.Draw(offset);
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
    }
}
