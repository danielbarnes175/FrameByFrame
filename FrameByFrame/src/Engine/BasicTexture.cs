using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine
{
    public class BasicTexture
    {
        public float rotation;
        public Vector2 position, dimensions;
        public Texture2D texture;

        public Color[] data;
        public Color[,] colorData;

        public BasicTexture(string path, Vector2 pos, Vector2 dimensions)
        {
            position = pos;
            this.dimensions = dimensions;

            texture = GlobalParameters.GlobalContent.Load<Texture2D>(path);
            if (texture != null)
            {
                SetColorData();
            }
        }

        public BasicTexture(Texture2D texture, Vector2 pos, Vector2 dimensions)
        {
            position = pos;
            this.dimensions = dimensions;
            this.texture = texture;

            if (texture != null)
            {
                SetColorData();
            }
        }

        public void SetColorData()
        {
            data = new Color[this.texture.Width * this.texture.Height];
            this.texture.GetData<Color>(data);
            colorData = new Color[this.texture.Width, this.texture.Height];

            for (int x = 0; x < texture.Width; x++)
            {
                for (int y = 0; y < texture.Height; y++)
                {
                    colorData[x, y] = data[x + y * texture.Width];
                }
            }
        }

        public virtual void Update()
        {

        }

        public virtual void Draw(Vector2 offset)
        {
            Draw(offset, 1.0f, 1.0f);
        }

        public virtual void Draw(Vector2 offset, float opacity)
        {
            Draw(offset, opacity, 1.0f);
        }

        public virtual void Draw(Vector2 offset, Vector2 origin)
        {
            Draw(offset, 1.0f, 1.0f, origin);
        }

        public virtual void Draw(Vector2 offset, float opacity, float scaleFactor, Vector2? origin = null)
        {
            if (texture != null)
            {
                Vector2 scaledDimensions = new Vector2(dimensions.X * GlobalParameters.scaleX, dimensions.Y * GlobalParameters.scaleY);
                Vector2 drawPosition = (position + offset) * scaleFactor;
                Rectangle scaleRect = new Rectangle((int)drawPosition.X, (int)drawPosition.Y, (int)scaledDimensions.X, (int)scaledDimensions.Y);

                GlobalParameters.GlobalSpriteBatch.Draw(
                    texture,
                    scaleRect,
                    null,
                    Color.White * opacity,
                    rotation,
                    origin ?? new Vector2(texture.Bounds.Width / 2, texture.Bounds.Height / 2),
                    SpriteEffects.None,
                    0.2f
                );
            }
        }
    }
}
