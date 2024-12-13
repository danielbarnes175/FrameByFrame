using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class ProjectsScene : BaseScene
    {
        private List<BasicTexture> _textures;

        private List<string> projects;
        private List<Animation.Animation> animations;
        private int currentPreview;
        private int previewFrame;
        private int timePlaying;
        private int fps;

        public ProjectsScene()
        {
            timePlaying = 0;
            fps = 4;
            _textures = new List<BasicTexture>();

            currentPreview = 0;
            previewFrame = 0;

            if (!Directory.Exists("Projects"))
            {
                Directory.CreateDirectory("Projects");
            }
        }

        public override void LoadContent()
        {
            Texture2D borderTexture = CreateTexture(GlobalParameters.GlobalGraphics, GlobalParameters.screenWidth, 300, pixel => Color.Orange);
            _textures.Add(new BasicTexture(borderTexture, new Vector2(0, 0), new Vector2(GlobalParameters.screenWidth * 2, 300)));
            _textures.Add(new BasicTexture(borderTexture, new Vector2(0, GlobalParameters.screenHeight), new Vector2(GlobalParameters.screenWidth * 2, 300)));
            BasicTexture arrowRight = new BasicTexture("Static\\ProjectsScene/button_arrow", new Vector2(GlobalParameters.screenWidth / 2 + 200, GlobalParameters.screenHeight / 2), new Vector2(45, 45));
            arrowRight.rotation = 1.571f;
            BasicTexture arrowLeft = new BasicTexture("Static\\ProjectsScene/button_arrow", new Vector2(GlobalParameters.screenWidth / 2 - 200, GlobalParameters.screenHeight / 2), new Vector2(45, 45));
            arrowLeft.rotation = -1.571f;
            _textures.Add(arrowRight);
            _textures.Add(arrowLeft);
            _textures.Add(new BasicTexture("Static\\ProjectsScene/button_view-project-directory", new Vector2(GlobalParameters.screenWidth - 200, GlobalParameters.screenHeight - 30), new Vector2(372, 50)));
            LoadAnimations();
        }

        [DebuggerNonUserCode]
        private Texture2D getTextureFromPng(string filename)
        {
            FileStream setStream = File.Open(filename, FileMode.Open);

            Texture2D NewTexture = Texture2D.FromStream(GlobalParameters.GlobalGraphics, setStream);
            setStream.Dispose();
            return NewTexture;
        }

        public override void Update(GameTime gameTime)
        {
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC"))
            {
                currentPreview = 0;
                previewFrame = 0;
                timePlaying = 0;
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Menu Scene"];
            }
            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                Vector2 pointPosition = GlobalParameters.GlobalMouse.newMousePos;
                if (pointPosition.X > 570 && pointPosition.X < 615 && pointPosition.Y > 400 && pointPosition.Y < 445)
                {
                    currentPreview -= 1;
                    if (currentPreview < 0) currentPreview = animations.Count - 1;
                    previewFrame = 0;
                    timePlaying = 0;
                }
                else if (pointPosition.X > 970 && pointPosition.X < 1015 && pointPosition.Y > 400 && pointPosition.Y < 445)
                {
                    currentPreview += 1;
                    if (currentPreview >= animations.Count) currentPreview = 0;
                    previewFrame = 0;
                    timePlaying = 0;
                }
                else if (pointPosition.X > 1208 && pointPosition.X < 1580 && pointPosition.Y > 820 && pointPosition.Y < 866)
                {
                    string path = Directory.GetCurrentDirectory() + "/" + "Projects" + Path.DirectorySeparatorChar;
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = path,
                            UseShellExecute = true,
                            Verb = "open"
                        });
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine(e.Message);
                    }
                }
            }

            timePlaying += 1;
            if (
                animations.Count > 0
                && timePlaying % fps == 0
            )
            {
                previewFrame += 1;
                if (previewFrame >= animations[currentPreview].frames.Count)
                    previewFrame = 0;
            }

            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            if (animations.Count > 0)
            {
                animations[currentPreview].GetFrameAtIndex(previewFrame).DrawCombinedTexture(1.0f);
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, string.Concat("Current Project Shown: ", projects[currentPreview].AsSpan(9)), new Vector2(GlobalParameters.screenWidth - 372, GlobalParameters.screenHeight - 80), Color.Black);
            }
            else
            {
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "No projects found", new Vector2(GlobalParameters.screenWidth / 2 - 50, GlobalParameters.screenHeight / 2), Color.Black);
            }

            foreach (BasicTexture texture in _textures)
            {
                texture.Draw(offset);
            }

            base.Draw(offset);
        }

        public void LoadProjects()
        {
            var directories = Directory.GetDirectories("Projects/");
            projects = directories.ToList();
        }

        public static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint)
        {
            // Initialize a texture
            Texture2D texture = new Texture2D(device, width, height);

            // The array holds the color for each pixel in the texture
            Color[] data = new Color[width * height];
            for (int pixel = 0; pixel < data.Count(); pixel++)
            {
                // The function applies the color according to the specified pixel
                data[pixel] = paint(pixel);
            }

            // Set the color
            texture.SetData(data);

            return texture;
        }

        public void LoadAnimations()
        {
            LoadProjects();
            animations = new List<Animation.Animation>();
            for (int i = 0; i < projects.Count; i++)
            {
                Animation.Animation animation = new Animation.Animation("temp");

                int frameCounter = 0;
                while (true)
                {
                    string filename = projects[i] + "/Frame_" + frameCounter + ".png";
                    if (!File.Exists(filename)) break;

                    Vector2 position = new Vector2(GlobalParameters.screenWidth / 2, GlobalParameters.screenHeight / 2);
                    Vector2 dimensions = new Vector2(300, 300);
                    Texture2D pngTexture = getTextureFromPng(filename);

                    Frame frame = new Frame(position, dimensions);
                    BasicTexture texture = new BasicTexture(pngTexture, position, dimensions);
                    frame.CombinedTexture = texture;

                    animation.AddFrame(frame);
                    frameCounter++;
                }
                animations.Add(animation);
                Debug.WriteLine(animations.ToArray().ToString());
            }
        }
    }
}
