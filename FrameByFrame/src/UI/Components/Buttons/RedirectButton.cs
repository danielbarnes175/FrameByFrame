using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI.Components.Buttons
{
    public class RedirectButton : Button
    {
        public string target;

        public RedirectButton(string target, string path, Vector2 position, Vector2 dimensions) : base(path, position, dimensions)
        {
            this.target = target;
            isBeingMousedOver = false;
        }

        public RedirectButton(string target, Texture2D texture, Vector2 position, Vector2 dimensions) : base(texture, position, dimensions)
        {
            this.target = target;
            isBeingMousedOver = false;
        }

        public override void Update()
        {
            // If button is clicked, call on click.
            if (isBeingMousedOver && GlobalParameters.GlobalMouse.LeftClickHold())
            {
                if (target == "Projects Scene")
                {
                    ((ProjectsScene)GlobalParameters.Scenes["Projects Scene"]).LoadAnimations();
                }
                GlobalParameters.CurrentScene = GlobalParameters.Scenes[target];
            }

            base.Update();
        }

        public override void Draw(Vector2 offset)
        {
            Color color = isBeingMousedOver ? (Color.Gray * 0.5f) : Color.White;
            if (texture != null)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(texture,
                    new Rectangle((int)(position.X + offset.X), (int)(position.Y + offset.Y), (int)dimensions.X,
                        (int)dimensions.Y), null, color, rotation,
                    new Vector2(texture.Bounds.Width / 2, texture.Bounds.Height / 2), new SpriteEffects(), 0.2f);
            }
        }

        public override void Draw(Vector2 offset, float opacity)
        {
            Color color = isBeingMousedOver ? (Color.Gray * 0.5f) : Color.White;
            if (texture != null)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(texture,
                    new Rectangle((int)(position.X + offset.X), (int)(position.Y + offset.Y), (int)dimensions.X,
                        (int)dimensions.Y), null, color * opacity, rotation,
                    new Vector2(texture.Bounds.Width / 2, texture.Bounds.Height / 2), new SpriteEffects(), 0.2f);
            }
        }

        public override void Draw(Vector2 offset, Vector2 origin)
        {
            Color color = isBeingMousedOver ? new Color(255, 255, 255, 0.8f) : Color.White;
            GlobalParameters.GlobalSpriteBatch.Draw(texture,
                new Rectangle((int)(position.X + offset.X), (int)(position.Y + offset.Y), (int)dimensions.X,
                    (int)dimensions.Y), null, color, rotation, new Vector2(origin.X, origin.Y),
                new SpriteEffects(), 0.2f);
        }
    }
}
