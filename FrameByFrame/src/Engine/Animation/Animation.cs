using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameByFrame.src.Engine.Animation
{
    public class Animation
    {
        // Animation
        private double playbackTimer;
        public int fps;
        public bool IsPlaying { get; private set; }

        public Vector2 frameSize;
        public Vector2 framePosition;

        // Tools
        public bool isOnionSkinLoaded;
        public int brushSize;
        private int maxOnionFrames = 3;
        private float baseOpacity = 0.1f;
        public string selectedLayer;

        // Project Settings
        public string projectName;

        public LinkedList<Frame> frames;
        private LinkedListNode<Frame> currentFrame;

        public int TotalFrames => frames.Count;
        public int CurrentFrameIndex { get; private set; }
        public Frame CurrentFrame => currentFrame?.Value;

        public Animation(string projectName)
        {
            this.projectName = projectName;
            fps = 12;
            frames = new LinkedList<Frame>();
            playbackTimer = 0;
            IsPlaying = false;
            selectedLayer = "_layer1";
            CurrentFrameIndex = 0;
            brushSize = 15;
            isOnionSkinLoaded = true;
        }

        public void InitializeFrames()
        {
            frameSize = new Vector2(1200, 800);
            framePosition = new Vector2(
                GlobalParameters.screenWidth / 2 - (int)frameSize.X / 2,
                GlobalParameters.screenHeight / 2 - (int)frameSize.Y / 2);

            frames.AddLast(new Frame(framePosition, frameSize));
            currentFrame = frames.First;
        }

        public void AddFrame(Frame frame)
        {
            frames.AddLast(frame);
        }

        public void DeleteCurrentFrame()
        {
            if (frames.Count <= 1) return; // Cannot delete the last remaining frame

            var frameToRemove = currentFrame;
            currentFrame = currentFrame.Previous ?? currentFrame.Next;
            frames.Remove(frameToRemove);
            CurrentFrameIndex = Math.Max(0, CurrentFrameIndex - 1);
        }

        public void NextFrame()
        {
            CurrentFrameIndex += 1;
            if (CurrentFrameIndex > TotalFrames - 1)
            {
                frames.AddLast(new Frame(framePosition, frameSize));
            }
            currentFrame = currentFrame.Next;
        }

        public void PreviousFrame()
        {
            if (CurrentFrameIndex <= 0) return;
            if (CurrentFrameIndex > 0)
            {
                CurrentFrameIndex -= 1;
            }
            currentFrame = currentFrame.Previous;
        }

        public void EraseCurrentLayer()
        {
            if (selectedLayer == "_layer1") currentFrame.Value._layer1 = new BasicColor[Frame.staticWidth, Frame.staticHeight];
            if (selectedLayer == "_layer2") currentFrame.Value._layer2 = new BasicColor[Frame.staticWidth, Frame.staticHeight];
            if (selectedLayer == "_layer3") currentFrame.Value._layer3 = new BasicColor[Frame.staticWidth, Frame.staticHeight];
        }

        public void DeleteFrame()
        {
            // Can't delete the only frame
            if (frames.Count <= 1) return;

            var toRemove = currentFrame;
            currentFrame = currentFrame.Previous ?? currentFrame.Next;
            frames.Remove(toRemove);
            CurrentFrameIndex = Math.Max(0, CurrentFrameIndex - 1);
        }
        public void InsertFrame()
        {
            var newFrame = new Frame(framePosition, frameSize);
            frames.AddBefore(currentFrame, newFrame);
            currentFrame = currentFrame.Previous;
        }

        public void TogglePlaying()
        {
            IsPlaying = !IsPlaying;
        }

        public void Animate(GameTime gameTime)
        {
            if (!IsPlaying) return;

            double frameDuration = 1.0 / fps; // Seconds per frame
            playbackTimer += gameTime.ElapsedGameTime.TotalSeconds;

            if (playbackTimer >= frameDuration)
            {
                playbackTimer -= frameDuration;
                CurrentFrameIndex = (CurrentFrameIndex + 1) % TotalFrames;
                currentFrame = currentFrame.Next ?? frames.First;
            }
        }

        public void Stop()
        {
            IsPlaying = false;
        }

        public void Start()
        {
            IsPlaying = true;
        }

        public void DrawOnCurrentLayer()
        {
            BasicColor[,] layer = GetSelectedLayer();
            if (layer == null) return;

            Vector2 mousePositionCur = GlobalParameters.GlobalMouse.newMousePos;
            Vector2 mousePositionOld = GlobalParameters.GlobalMouse.oldMousePos;

            float xChange = mousePositionCur.X - mousePositionOld.X;
            float yChange = mousePositionCur.Y - mousePositionOld.Y;

            float distance = (float)Math.Ceiling(Math.Sqrt(Math.Pow(xChange, 2) + Math.Pow(yChange, 2)) / 2);

            for (int i = 0; i < distance; i++)
            {
                float newX = i * (xChange / distance) + mousePositionOld.X - framePosition.X;
                float newY = i * (yChange / distance) + mousePositionOld.Y - framePosition.Y;

                if (newX >= Frame.staticWidth + framePosition.X || newX <= 0 || newY <= 0 || newY >= Frame.staticHeight + framePosition.Y) continue;
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

        public void DrawCurrentFrame()
        {
            currentFrame?.Value.Draw(1.0f);

            if (!IsPlaying && isOnionSkinLoaded)
            {
                DrawOnionSkin();
            }

            DrawFrameWithOpacity(currentFrame?.Value, 1.0f);
        }

        public void DrawOnionSkin()
        {
            for (int i = 1; i <= maxOnionFrames; i++)
            {
                var frame = frames.ElementAtOrDefault(CurrentFrameIndex - i);
                if (frame != null)
                {
                    float opacity = baseOpacity * (maxOnionFrames - i + 1);
                    frame.DrawLayers(opacity);
                }
            }
        }

        private void DrawFrameWithOpacity(Frame frame, float opacity)
        {
            frame?.DrawLayers(opacity);
        }

        public Frame GetFrameAtIndex(int index)
        {
            if (index < 0 || index >= frames.Count) return null;

            var currentNode = frames.First;
            for (int i = 0; i < index; i++)
            {
                currentNode = currentNode.Next;
            }

            return currentNode?.Value;
        }
    }
}
