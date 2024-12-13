using FrameByFrame.src;
using FrameByFrame.src.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

public class UIElement : BasicTexture
{
    public List<Action<UIElement>> Callbacks { get; private set; }

    public UIElement(string path, Vector2 position, Vector2 dimensions) : base(path, position, dimensions)
    {
        Callbacks = new List<Action<UIElement>>();
    }

    public UIElement(Texture2D texture, Vector2 position, Vector2 dimensions) : base(texture, position, dimensions)
    {
        Callbacks = new List<Action<UIElement>>();
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

    public void RegisterCallback(Action<UIElement> callback)
    {
        if (callback != null && !Callbacks.Contains(callback))
        {
            Callbacks.Add(callback);
        }
    }

    public void DeregisterCallback(Action<UIElement> callback)
    {
        if (Callbacks.Contains(callback))
        {
            Callbacks.Remove(callback);
        }
    }

    protected void TriggerCallbacks()
    {
        foreach (var callback in Callbacks)
        {
            callback(this);
        }
    }
}
