using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI.Components.Buttons
{
    public class PopupButton : Button
    {
        public Overlay target;
        public string text;
        public Color? textColor;

        public PopupButton(Overlay target, string path, Vector2 position, Vector2 dimensions, string text = null, Color? textColor = null) : base(path, position, dimensions)
        {
            this.target = target;
            this.text = text;
            this.textColor = (textColor is null) ? Color.Black : textColor;
            isBeingMousedOver = false;
        }

        public PopupButton(Overlay target, Texture2D texture, Vector2 position, Vector2 dimensions, string text = null, Color? textColor = null) : base(texture, position, dimensions)
        {
            this.target = target;
            this.text = text;
            this.textColor = (textColor is null) ? Color.Black : textColor;
            isBeingMousedOver = false;
        }

        public override void Update()
        {
            // If button is clicked, show overlay.
            if (isBeingMousedOver && GlobalParameters.GlobalMouse.LeftClickHold())
            {
                target.isVisible = true;
            }
            else if (!isBeingMousedOver && GlobalParameters.GlobalMouse.LeftClickHold())
            {
                target.isVisible = false;
            }

            base.Update();
        }

        public override void Draw(Vector2 offset, Vector2 origin)
        {
            Color color = isBeingMousedOver ? new Color(255, 255, 255, 0.8f) : Color.White;
            Color textColorAdjusted = (Color)(isBeingMousedOver ? (textColor * 0.5f) : textColor);
            if (texture != null)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(texture,
                new Rectangle((int)(position.X + offset.X), (int)(position.Y + offset.Y), (int)dimensions.X,
                    (int)dimensions.Y), null, color, rotation, new Vector2(origin.X, origin.Y),
                new SpriteEffects(), 0.2f);
            }

            if (!string.IsNullOrEmpty(text))
            {
                Vector2 stringSize = GlobalParameters.font.MeasureString(text);
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, text, new Vector2(position.X + ((dimensions.X - stringSize.X) / 2), position.Y + ((dimensions.Y - stringSize.Y) / 2)), textColorAdjusted);
            }

            target.Draw(offset);
        }
    }
}
