using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.Engine
{
    public class BasicColor
    {
        public Vector2 position, dimensions;
        public Texture2D texture;
        public BasicColor(Texture2D texture, Vector2 position, Vector2 dimensions)
        {
            this.position = position;
            this.dimensions = dimensions;

            this.texture = texture;
        }

        public virtual void Update()
        {

        }
        public virtual void Draw(float opacity)
        {
            if (texture != null)
            {
                Vector2 scaledDimensions = new Vector2(dimensions.X * GlobalParameters.scaleX, dimensions.Y * GlobalParameters.scaleY);
            Vector2 drawPosition = (position + Vector2.Zero) * 1.0f;
            Rectangle scaleRect = new Rectangle((int)drawPosition.X, (int)drawPosition.Y, (int)scaledDimensions.X, (int)scaledDimensions.Y);

                GlobalParameters.GlobalSpriteBatch.Draw(texture,
                    scaleRect, null, Color.White * opacity, 0.0f,
                    new Vector2(texture.Bounds.Width / 2, texture.Bounds.Height / 2), new SpriteEffects(), 0.2f);
            }
        }
    }
}
