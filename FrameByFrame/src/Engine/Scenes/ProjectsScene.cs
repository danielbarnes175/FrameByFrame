using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class ProjectsScene : BaseScene
    {
        private List<BasicTexture> _textures;
        private Texture2D _borderTexture;
        private Texture2D _actionButtonTexture;

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
            _borderTexture = CreateTexture(GlobalParameters.GlobalGraphics, GlobalParameters.screenWidth, 300, pixel => Color.Orange);
            _actionButtonTexture = CreateTexture(GlobalParameters.GlobalGraphics, 1, 1, pixel => Color.White);
            _textures.Add(new BasicTexture(_borderTexture, new Vector2(0, 0), new Vector2(GlobalParameters.screenWidth * 2, 300)));
            _textures.Add(new BasicTexture(_borderTexture, new Vector2(0, GlobalParameters.screenHeight), new Vector2(GlobalParameters.screenWidth * 2, 300)));
            BasicTexture arrowRight = new BasicTexture("Static\\ProjectsScene/button_arrow", new Vector2(GlobalParameters.screenWidth / 2 + 200, GlobalParameters.screenHeight / 2), new Vector2(45, 45));
            arrowRight.rotation = 1.571f;
            BasicTexture arrowLeft = new BasicTexture("Static\\ProjectsScene/button_arrow", new Vector2(GlobalParameters.screenWidth / 2 - 200, GlobalParameters.screenHeight / 2), new Vector2(45, 45));
            arrowLeft.rotation = -1.571f;
            _textures.Add(arrowRight);
            _textures.Add(arrowLeft);
            _textures.Add(new BasicTexture("Static\\ProjectsScene/button_view-project-directory", new Vector2(GlobalParameters.screenWidth - 200, GlobalParameters.screenHeight - 30), new Vector2(372, 50)));
            LoadAnimations();
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

            if (GlobalParameters.GlobalKeyboard.GetPressSingle("ENTER"))
            {
                OpenSelectedProject();
            }

            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                Vector2 pointPosition = GlobalParameters.GlobalMouse.newMousePos;
                if (animations.Count > 0 && GetEditButtonBounds().Contains(pointPosition))
                {
                    OpenSelectedProject();
                }
                else if (animations.Count > 0 && GetExportButtonBounds().Contains(pointPosition))
                {
                    ExportSelectedProject();
                }
                else if (pointPosition.X > 570 && pointPosition.X < 615 && pointPosition.Y > 400 && pointPosition.Y < 445)
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
                    catch (Exception)
                    {
                        //Console.WriteLine(e.Message);
                    }
                }
                else
                {
                    Rectangle previewBounds = GetPreviewBounds();
                    if (previewBounds.Contains(pointPosition))
                    {
                        OpenSelectedProject();
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
                Frame frame = animations[currentPreview].GetFrameAtIndex(previewFrame);
                frame?.DrawPreview(GetPreviewBounds(), 1.0f);
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, $"Current Project Shown: {animations[currentPreview].projectName}", new Vector2(GlobalParameters.screenWidth - 372, GlobalParameters.screenHeight - 80), Color.Black);

            }
            else
            {
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "No projects found", new Vector2(GlobalParameters.screenWidth / 2 - 50, GlobalParameters.screenHeight / 2), Color.Black);
            }

            foreach (BasicTexture texture in _textures)
            {
                texture.Draw(offset);
            }

            if (animations.Count > 0)
            {
                DrawActionButton(GetEditButtonBounds(), "EDIT");
                DrawActionButton(GetExportButtonBounds(), "EXPORT");
            }

            base.Draw(offset);
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
            DisposePreviewAnimations();
            projects = new List<string>();
            animations = new List<Animation.Animation>();
            currentPreview = 0;
            previewFrame = 0;
            timePlaying = 0;

            foreach (string saveFilename in Directory.GetFiles("Projects", "*.fbf"))
            {
                try
                {
                    Animation.Animation savedAnimation = SaveService.LoadAnimation(saveFilename);
                    projects.Add(saveFilename);
                    animations.Add(savedAnimation);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"Skipping invalid save '{saveFilename}': {exception.Message}");
                }
            }
        }

        private Rectangle GetPreviewBounds()
        {
            return new Rectangle(
                GlobalParameters.screenWidth / 2 - 150,
                GlobalParameters.screenHeight / 2 - 150,
                300,
                300);
        }

        private Rectangle GetEditButtonBounds()
        {
            return new Rectangle(GlobalParameters.screenWidth / 2 - 155, GlobalParameters.screenHeight / 2 + 175, 145, 48);
        }

        private Rectangle GetExportButtonBounds()
        {
            return new Rectangle(GlobalParameters.screenWidth / 2 + 10, GlobalParameters.screenHeight / 2 + 175, 145, 48);
        }

        private void DrawActionButton(Rectangle bounds, string label)
        {
            bool isHovered = bounds.Contains(GlobalParameters.GlobalMouse.newMousePos);
            Color buttonColor = isHovered ? new Color(255, 180, 90) : new Color(220, 110, 0);
            GlobalParameters.GlobalSpriteBatch.Draw(_actionButtonTexture, bounds, buttonColor);

            Vector2 labelSize = GlobalParameters.font.MeasureString(label);
            Vector2 labelPosition = new Vector2(
                bounds.X + (bounds.Width - labelSize.X) / 2,
                bounds.Y + (bounds.Height - labelSize.Y) / 2);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, label, labelPosition, Color.White);
        }

        private void OpenSelectedProject()
        {
            if (animations == null || animations.Count == 0)
            {
                return;
            }

            try
            {
                Animation.Animation loadedAnimation = SaveService.LoadAnimation(projects[currentPreview]);
                DrawingScene drawingScene = (DrawingScene)GlobalParameters.Scenes["Drawing Scene"];
                drawingScene.LoadAnimation(loadedAnimation);
                GlobalParameters.CurrentScene = drawingScene;
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Unable to open project '{projects[currentPreview]}': {exception.Message}");
            }
        }

        private void ExportSelectedProject()
        {
            if (animations == null || animations.Count == 0)
                return;

            try
            {
                SaveService.ExportAnimation(animations[currentPreview]);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Unable to export project '{projects[currentPreview]}': {exception.Message}");
            }
        }

        private void DisposePreviewAnimations()
        {
            if (animations == null) return;

            foreach (Animation.Animation animation in animations)
            {
                animation?.Dispose();
            }

            animations.Clear();
        }

        public override void Dispose()
        {
            DisposePreviewAnimations();
            _borderTexture?.Dispose();
            _borderTexture = null;
            _actionButtonTexture?.Dispose();
            _actionButtonTexture = null;
            projects?.Clear();
            _textures?.Clear();
        }
    }
}
