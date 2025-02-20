using FrameByFrame.src.Engine;
using FrameByFrame.src.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FrameByFrame.src.UI.Components.Buttons
{
    public class RadioButton : Button
    {
        public bool isSelected;
        public Texture2D selectedTexture;
        public Texture2D unselectedTexture;

        public RadioButton(string selectedTexturePath, string unselectedTexturePath, bool isSelected, Vector2 position, Vector2 dimensions) : base(unselectedTexturePath, position, dimensions)
        {
            selectedTexture = GlobalParameters.GlobalContent.Load<Texture2D>(selectedTexturePath);
            unselectedTexture = GlobalParameters.GlobalContent.Load<Texture2D>(unselectedTexturePath);

            this.isSelected = isSelected;
        }

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

            Vector2 scaledDimensions = new Vector2(dimensions.X * GlobalParameters.scaleX, dimensions.Y * GlobalParameters.scaleY);
            Vector2 drawPosition = (position + offset) * 1.0f;
            Rectangle scaleRect = new Rectangle((int)drawPosition.X, (int)drawPosition.Y, (int)scaledDimensions.X, (int)scaledDimensions.Y);

            GlobalParameters.GlobalSpriteBatch.Draw(
                currentTexture,
                scaleRect, 
                null, 
                Color.White, 
                rotation, 
                new Vector2(origin.X, origin.Y),
                new SpriteEffects(), 
                0.2f
            );
        }

        public void toggleIsSelected()
        {
            isSelected = !isSelected;
        }
    }
}
