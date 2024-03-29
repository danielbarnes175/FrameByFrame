﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (timePlaying % fps == 0)
            {
                previewFrame += 1;
                if (previewFrame >= animations[currentPreview].Frames.Count)
                    previewFrame = 0;
            }

            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        { 
           animations[currentPreview].Frames[previewFrame].Draw(offset);
           foreach (BasicTexture texture in _textures)
           {
               texture.Draw(offset);
           }
           GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "Current Project Shown: " + projects[currentPreview].Substring(9), new Vector2(GlobalParameters.screenWidth - 372, GlobalParameters.screenHeight - 80), Color.Black);
            base.Draw(offset);
        }

        public void LoadProjects()
        {
            var directories = Directory.GetDirectories("Projects/");
            projects = directories.ToList();
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

        public void LoadAnimations()
        {
            LoadProjects();
            animations = new List<Animation.Animation>();
            for (int i = 0; i < projects.Count; i++)
            {
                animations.Add(new Animation.Animation());
                int frameCounter = 0;
                while (true)
                {
                    string filename = projects[i] + "/Frame_" + frameCounter + ".png";
                    if (!File.Exists(filename)) break;
                    Texture2D pngTexture = getTextureFromPng(filename);

                    BasicTexture preview = new BasicTexture(pngTexture,
                        new Vector2(GlobalParameters.screenWidth / 2, GlobalParameters.screenHeight / 2),
                        new Vector2(300, 300));
                    animations[i].Frames.Add(preview);
                    frameCounter++;
                }
            }
        }
    }
}
