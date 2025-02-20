using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI.Components.Buttons
{
    public class FrameCounterComponent : UIElement
    {
        public string text;
        public Color? textColor;

        public FrameCounterComponent(string path, Vector2 position, Vector2 dimensions, string text = null, Color? textColor = null) : base(path, position, dimensions)
        {
            this.text = text;
            this.textColor = (textColor is null) ? Color.Black : textColor;
        }

        public FrameCounterComponent(Texture2D texture, Vector2 position, Vector2 dimensions, string text = null, Color? textColor = null) : base(texture, position, dimensions)
        {
            this.text = text;
            this.textColor = (textColor is null) ? Color.Black : textColor;
        }

        public override void Update()
        {
            // If button is clicked, show overlay.
            int currentFrame = ((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).animation.CurrentFrameIndex;
            int frameCount = ((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).animation.TotalFrames;

            text = currentFrame + 1 + " / " + frameCount;

            base.Update();
        }

        public override void Draw(Vector2 offset, Vector2 origin)
        {
            if (texture != null)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(texture,
                new Rectangle((int)(position.X + offset.X), (int)(position.Y + offset.Y), (int)dimensions.X,
                    (int)dimensions.Y), null, Color.White, rotation, new Vector2(origin.X, origin.Y),
                new SpriteEffects(), 0.2f);
            }

            if (!string.IsNullOrEmpty(text))
            {
                Vector2 stringSize = GlobalParameters.font.MeasureString(text);
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, text, new Vector2(position.X + ((dimensions.X - stringSize.X) / 2), position.Y + ((dimensions.Y - stringSize.Y) / 2)), Color.White);
            }
        }
    }
}
