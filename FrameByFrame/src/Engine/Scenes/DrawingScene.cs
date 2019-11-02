using System;
using System.Collections.Generic;
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

        private List<Frame> frames;
        private int currentFrame;
        private int totalFrames;

        private bool isPlaying;
        private int timePlaying;

        public DrawingScene()
        {
            selectedLayer = "_layer1";
            frames = new List<Frame>();
            frames.Add(new Frame());
            currentFrame = 0;
            totalFrames = 1;
            isPlaying = false;
            timePlaying = 0;
        }

        public override void LoadContent()
        {

        }

        public override void Update(GameTime gameTime)
        {
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
            if (timePlaying % 3 != 0) return;
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
    }
}
