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
            // Toggle visibility of overlay when button is clicked
            isBeingMousedOver = CollisionService.CheckMouseCollision(this);
            if (isBeingMousedOver && GlobalParameters.GlobalMouse.LeftClick())
            {
                target.isVisible = !target.isVisible;
            }

            // If overlay is visible, update it
            if (target.isVisible)
            {
                target.Update();

                if (!isBeingMousedOver && !CollisionService.CheckMouseCollision(target) && GlobalParameters.GlobalMouse.LeftClickHold())
                {
                    target.isVisible = false;
                }
            }
        }

        public override void Draw(Vector2 offset, Vector2 origin)
        {
            base.Draw(offset, origin);
            target.Draw(offset);
        }
    }
}
