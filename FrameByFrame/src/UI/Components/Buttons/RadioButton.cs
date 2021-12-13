using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI.Components.Buttons
{
    public class RadioButton : Button
    {
        public bool isSelected;
        public Texture2D selectedTexture;
        public Texture2D unselectedTexture;

        public RadioButton(Texture2D selectedTexture, Texture2D unselectedTexture, bool isSelected, Vector2 position, Vector2 dimensions) : base(unselectedTexture, position, dimensions)
        {
            this.selectedTexture = selectedTexture;
            this.unselectedTexture = unselectedTexture;

            this.isSelected = isSelected;
        }

        public override void Update()
        {
            base.Update();
        }

        public override void Draw(Vector2 offset, Vector2 origin)
        {
            Texture2D currentTexture = isSelected ? selectedTexture : unselectedTexture;

            GlobalParameters.GlobalSpriteBatch.Draw(currentTexture,
                new Rectangle((int)(position.X + offset.X), (int)(position.Y + offset.Y), (int)dimensions.X,
                    (int)dimensions.Y), null, Color.White, rotation, new Vector2(origin.X, origin.Y),
                new SpriteEffects(), 0.2f);
        }

        public void toggleIsSelected()
        {
            isSelected = !isSelected;
        }
    }
}
