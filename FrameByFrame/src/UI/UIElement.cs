using FrameByFrame.src;
using FrameByFrame.src.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public class UIElement : BasicTexture
{
    public UIElement(string path, Vector2 position, Vector2 dimensions) : base(path, position, dimensions)
    {

    }

    public UIElement(Texture2D texture, Vector2 position, Vector2 dimensions) : base(texture, position, dimensions)
    {

    }

    public override void Update()
    {
        base.Update();
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
