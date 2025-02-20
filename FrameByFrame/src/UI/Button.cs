using FrameByFrame.src;
using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Services;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class Button : UIElement
{
    public bool isBeingMousedOver;
    protected bool ignoreOpacityOnHover;

    public string text;
    public Color? textColor;

    public Button(string path, Vector2 position, Vector2 dimensions, bool ignoreOpacityOnHover = false) : base(path, position, dimensions)
    {
        isBeingMousedOver = false;
        this.ignoreOpacityOnHover = ignoreOpacityOnHover;
    }


    public Button(Texture2D texture, Vector2 position, Vector2 dimensions, bool ignoreOpacityOnHover = false) : base(texture, position, dimensions)
    {
        this.ignoreOpacityOnHover = ignoreOpacityOnHover;
        isBeingMousedOver = false;
    }

    public override void Update()
    {
        isBeingMousedOver = CollisionService.CheckMouseCollision(this, ignoreOpacityOnHover);
    }

    public override void Draw(Vector2 offset)
    {
        base.Draw(offset);
    }

    public override void Draw(Vector2 offset, float opacity)
    {
        base.Draw(offset, opacity);
    }

    public override void Draw(Vector2 offset, Vector2 origin)
    {
        Color color = isBeingMousedOver ? new Color(255, 255, 255, 0.8f) : Color.White;
        Color textColorAdjusted = (Color)(isBeingMousedOver ? (Color.Black * 0.5f) : Color.Black);
        if (texture != null)
        {
            Vector2 scaledDimensions = new Vector2(dimensions.X * GlobalParameters.scaleX, dimensions.Y * GlobalParameters.scaleY);
            Vector2 drawPosition = (position + offset) * 1.0f;
            Rectangle scaleRect = new Rectangle((int)drawPosition.X, (int)drawPosition.Y, (int)scaledDimensions.X, (int)scaledDimensions.Y);

            GlobalParameters.GlobalSpriteBatch.Draw(
                texture,
                scaleRect,
                null,
                color,
                rotation,
                new Vector2(origin.X, origin.Y),
                SpriteEffects.None,
                0.2f
            );
        }

        if (!string.IsNullOrEmpty(text))
        {
            Vector2 stringSize = GlobalParameters.font.MeasureString(text);
            GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, text, new Vector2(position.X + ((dimensions.X - stringSize.X) / 2), position.Y + ((dimensions.Y - stringSize.Y) / 2)), textColorAdjusted);
        }
    }
}
