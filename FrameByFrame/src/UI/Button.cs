using FrameByFrame.src;
using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Services;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class Button : UIElement
{
    public ButtonActionTypes actionType;
    public string target;
    public bool isBeingMousedOver;

	public Button(ButtonActionTypes actionType, string target, string path, Vector2 position, Vector2 dimensions) : base(path, position, dimensions)
    {
        this.actionType = actionType;
        this.target = target;
        isBeingMousedOver = false;
    }


    public Button(ButtonActionTypes actionType, string target, Texture2D texture, Vector2 position, Vector2 dimensions) : base(texture, position, dimensions)
    {
        this.actionType = actionType;
        this.target = target;
    }

    public override void Update()
    {
        // If button is clicked, call on click.
        if (CollisionService.CheckMouseCollision(this))
        {
            // If just hovering
            isBeingMousedOver = true;
            // If clicked, do on click
            if (GlobalParameters.GlobalMouse.LeftClickHold())
            {
                switch (actionType)
                {
                    case ButtonActionTypes.CHANGE_SCENE:
                        
                        if (target == "Projects Scene")
                        {
                            ((ProjectsScene)GlobalParameters.Scenes["Projects Scene"]).LoadAnimations();
                        }
                        GlobalParameters.CurrentScene = GlobalParameters.Scenes[target];
                        break;

                    default:
                        break;
                }
            }
        } else
        {
            isBeingMousedOver = false;
        }
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
