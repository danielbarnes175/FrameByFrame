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

    public Button(string path, Vector2 position, Vector2 dimensions) : base(path, position, dimensions)
    {
        isBeingMousedOver = false;
    }


    public Button(Texture2D texture, Vector2 position, Vector2 dimensions) : base(texture, position, dimensions)
    {
        isBeingMousedOver = false;
    }

    public override void Update()
    {
        if (CollisionService.CheckMouseCollision(this))
        {
            isBeingMousedOver = true;
        }
        else
        {
            isBeingMousedOver = false;
        }
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
        base.Draw(offset, origin);
    }
}
