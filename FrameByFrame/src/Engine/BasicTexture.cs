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

        public BasicTexture(string PATH, Vector2 POS, Vector2 dimensions)
        {
            position = POS;
            this.dimensions = dimensions;

            texture = GlobalParameters.GlobalContent.Load<Texture2D>(PATH);

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

        public BasicTexture(Texture2D texture, Vector2 POS, Vector2 dimensions)
        {
            position = POS;
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

        public virtual void Draw(Vector2 OFFSET)
        {
            if (texture != null)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(texture,
                    new Rectangle((int)(position.X + OFFSET.X), (int)(position.Y + OFFSET.Y), (int)dimensions.X,
                        (int)dimensions.Y), null, Color.White, rotation,
                    new Vector2(texture.Bounds.Width / 2, texture.Bounds.Height / 2), new SpriteEffects(), 0.2f);
            }
        }

        public virtual void Draw(Vector2 OFFSET, float opacity)
        {
            if (texture != null)
            {
                GlobalParameters.GlobalSpriteBatch.Draw(texture,
                    new Rectangle((int)(position.X + OFFSET.X), (int)(position.Y + OFFSET.Y), (int)dimensions.X,
                        (int)dimensions.Y), null, Color.White * opacity, rotation,
                    new Vector2(texture.Bounds.Width / 2, texture.Bounds.Height / 2), new SpriteEffects(), 0.2f);
            }
        }

        public virtual void Draw(Vector2 OFFSET, Vector2 ORIGIN)
        {
            GlobalParameters.GlobalSpriteBatch.Draw(texture,
                new Rectangle((int)(position.X + OFFSET.X), (int)(position.Y + OFFSET.Y), (int)dimensions.X,
                    (int)dimensions.Y), null, Color.White, rotation, new Vector2(ORIGIN.X, ORIGIN.Y),
                new SpriteEffects(), 0.2f);
        }
    }
}
