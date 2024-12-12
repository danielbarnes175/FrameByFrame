using System;
using System.Collections.Generic;
using System.Linq;
using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.UI;
using FrameByFrame.src.UI.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class DrawingScene : BaseScene
    {
        public static string selectedLayer;

        // Frame Management
        private LinkedList<Frame> frames;
        private LinkedListNode<Frame> currentFrame;

        public Vector2 frameSize;
        public Vector2 framePosition;

        public int currentFrameCount;
        public int totalFrames;

        // Animation
        private bool isPlaying;
        private double playbackTimer;

        private int timePlaying;
        public static int fps;

        // Tools
        public DrawingTools drawingTool;
        private List<UIElement> components;

        // Project Info
        public string projectName;

        public bool loadedScene;
        public bool isOnionSkinLoaded;
        public int brushSize;
        private int maxOnionFrames = 3;
        private float baseOpacity = 0.1f;

        public DrawingScene()
        {
            InitializeDefaults();
        }
        private void InitializeDefaults()
        {
            currentFrameCount = 0;
            totalFrames = 1;
            isPlaying = false;
            timePlaying = 1;
            loadedScene = false;
            brushSize = 15;
            drawingTool = DrawingTools.DRAW;
            isOnionSkinLoaded = true;
            fps = 12;

            frames = new LinkedList<Frame>();
            projectName = $"Project_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        public override void LoadContent()
        {
            selectedLayer = "_layer1";

            SetupUI();
            InitializeFrames();
        }

        private void SetupUI()
        {
            components = new List<UIElement>();
            Texture2D navbarBG = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, GlobalParameters.screenWidth, 32, pixel => new Color(35, 35, 35), Shapes.RECTANGLE);
            DrawingNavbarComponent navbar = new DrawingNavbarComponent(navbarBG, new Vector2(0, 0), new Vector2(GlobalParameters.screenWidth, 32));

            components.Add(navbar);
        }

        private void InitializeFrames()
        {
            frameSize = new Vector2(1200, 800);
            framePosition = new Vector2(
                GlobalParameters.screenWidth / 2 - (int)frameSize.X / 2,
                GlobalParameters.screenHeight / 2 - (int)frameSize.Y / 2);

            frames.AddLast(new Frame(framePosition, frameSize));
            currentFrame = frames.First;
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
                DrawOnCurrentLayer();
            }
        }

        private void ResetScene()
        {
            InitializeDefaults();
            LoadContent();
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
            currentFrame?.Value.Draw(1.0f);

            if (!isPlaying && isOnionSkinLoaded)
            {
                drawOnionSkin();
            }

            drawFrameWithOpacity(currentFrame?.Value, 1.0f);
        }

        public void drawOnionSkin()
        {
            for (int i = 1; i <= maxOnionFrames; i++)
            {
                var frame = frames.ElementAtOrDefault(currentFrameCount - i);
                if (frame != null)
                {
                    float opacity = baseOpacity * (maxOnionFrames - i + 1);
                    frame.DrawLayers(opacity);
                }
            }
        }

        private void drawFrameWithOpacity(Frame frame, float opacity)
        {
            frame?.DrawLayers(opacity);
        }

        public void handleKeyboardShortcuts()
        {
            // Exit drawing
            if (GlobalParameters.GlobalKeyboard.GetPress("ESC"))
            {
                ResetScene();
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Menu Scene"];
            }

            // Delete current frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("DELETE"))
            {
                deleteFrame();
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
                if (selectedLayer == "_layer1") currentFrame.Value._layer1 = new BasicColor[Frame.width, Frame.height];
                if (selectedLayer == "_layer2") currentFrame.Value._layer2 = new BasicColor[Frame.width, Frame.height];
                if (selectedLayer == "_layer3") currentFrame.Value._layer3 = new BasicColor[Frame.width, Frame.height];
            }

            // Toggle Onion Skin
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("O"))
                isOnionSkinLoaded = !isOnionSkinLoaded;

            // Load next frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("M"))
            {
                currentFrameCount += 1;
                if (currentFrameCount > totalFrames - 1)
                {
                    frames.AddLast(new Frame(framePosition, frameSize));
                    totalFrames += 1;
                }
                currentFrame = currentFrame.Next;
            }

            // Load previous frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("N"))
            {
                if (currentFrameCount <= 0) return;
                if (currentFrameCount > 0)
                {
                    currentFrameCount -= 1;
                }
                currentFrame = currentFrame.Previous;
            }

            // Insert a frame
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("B"))
            {
                insertFrame();
            }
        }
        public void Animate(GameTime gameTime)
        {
            // Calculate the time between frames based on fps (frames per second)
            double frameDuration = 1.0 / fps; // Duration of each frame in seconds

            // Increment the playback timer by the elapsed time since the last frame
            playbackTimer += gameTime.ElapsedGameTime.TotalSeconds;

            // If the playback timer exceeds the frame duration, advance the animation
            if (playbackTimer >= frameDuration)
            {
                playbackTimer -= frameDuration;
                currentFrameCount = (currentFrameCount + 1) % totalFrames;
                currentFrame = currentFrame.Next ?? frames.First;
            }
        }

        private void deleteFrame()
        {
            // Can't delete the only frame
            if (frames.Count <= 1) return;

            var toRemove = currentFrame;
            currentFrame = currentFrame.Previous ?? currentFrame.Next;
            frames.Remove(toRemove);
            totalFrames--;
            currentFrameCount = Math.Max(0, currentFrameCount - 1);
        }

        private void insertFrame()
        {
            var newFrame = new Frame(framePosition, frameSize);
            frames.AddBefore(currentFrame, newFrame);
            totalFrames++;
            currentFrame = currentFrame.Previous;
        }

        private void DrawOnCurrentLayer()
        {
            BasicColor[,] layer = GetSelectedLayer();
            if (layer == null) return;

            Vector2 mousePositionCur = GlobalParameters.GlobalMouse.newMousePos;
            Vector2 mousePositionOld = GlobalParameters.GlobalMouse.oldMousePos;

            float xChange = mousePositionCur.X - mousePositionOld.X;
            float yChange = mousePositionCur.Y - mousePositionOld.Y;

            float distance = (float)Math.Ceiling((Math.Sqrt(Math.Pow(xChange, 2) + Math.Pow(yChange, 2))) / 2);

            for (int i = 0; i < distance; i++)
            {
                float newX = (i * (xChange / distance)) + mousePositionOld.X - framePosition.X;
                float newY = (i * (yChange / distance)) + mousePositionOld.Y - framePosition.Y;

                if (newX >= Frame.width + framePosition.X || newX <= 0 || newY <= 0 || newY >= Frame.height + framePosition.Y) continue;
                Vector2 pointPosition = new Vector2(newX + framePosition.X, newY + framePosition.Y);

                DrawingService.SetColors(layer, currentFrame.Value.colors[0], pointPosition, Shapes.CIRCLE, brushSize);
            }
        }

        private BasicColor[,] GetSelectedLayer()
        {
            return selectedLayer switch
            {
                "_layer1" => currentFrame?.Value._layer1,
                "_layer2" => currentFrame?.Value._layer2,
                "_layer3" => currentFrame?.Value._layer3,
                _ => null
            };
        }

        private RenderTarget2D CombineTextures(IEnumerable<BasicTexture> textures, int width, int height)
        {
            var renderTarget = new RenderTarget2D(GlobalParameters.GlobalGraphics, width, height);

            GlobalParameters.GlobalGraphics.SetRenderTarget(renderTarget);
            GlobalParameters.GlobalGraphics.Clear(Color.Transparent);

            GlobalParameters.GlobalSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);

            foreach (var texture in textures)
            {
                texture.Draw(Vector2.Zero);
            }

            GlobalParameters.GlobalSpriteBatch.End();
            GlobalParameters.GlobalGraphics.SetRenderTarget(null);

            return renderTarget;
        }

        private RenderTarget2D CombineFrameTextures(Frame frame)
        {
            var textures = new List<BasicTexture>();
            foreach (var layer in new[] { frame._layer1, frame._layer2, frame._layer3 })
            {
                textures.AddRange((IEnumerable<BasicTexture>)layer.Cast<BasicColor>().Where(color => color != null));
            }
            return CombineTextures(textures, Frame.width, Frame.height);
        }
    }
}
