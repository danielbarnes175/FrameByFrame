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

        private List<BasicTexture> _layer1;
        private List<BasicTexture> _layer2;
        private List<BasicTexture> _layer3;

        public static string selectedLayer;

        public DrawingScene()
        {
            _textures = new List<BasicTexture>();
            _layer1 = new List<BasicTexture>();
            _layer2 = new List<BasicTexture>();
            _layer3 = new List<BasicTexture>();

            selectedLayer = "_layer1";
        }

        public override void LoadContent()
        {

        }

        public override void Update()
        {
            if (GlobalParameters.GlobalKeyboard.GetPress("W"))
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Settings Scene"];
            if (GlobalParameters.GlobalMouse.LeftClickHold())
            {
                Vector2 pointPosition = GlobalParameters.GlobalMouse.newMousePos;
                Vector2 pointDimensions = new Vector2(15, 15);

                Texture2D texture = CreateTexture(GlobalParameters.GlobalGraphics, 15, 15, pixel => GlobalParameters.CurrentColor);
                BasicTexture point = new BasicTexture(texture, pointPosition, pointDimensions);

                if (selectedLayer == "_layer1") _layer1.Add(point);
                if (selectedLayer == "_layer2") _layer2.Add(point);
                if (selectedLayer == "_layer3") _layer3.Add(point);
            }
            base.Update();
        }

        public override void Draw(Vector2 offset)
        {
            foreach (BasicTexture point in _layer3)
            {
                point.Draw(new Vector2(5, 25));
            }

            foreach (BasicTexture point in _layer2)
            {
                point.Draw(new Vector2(5, 25));
            }

            foreach (BasicTexture point in _layer1)
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
