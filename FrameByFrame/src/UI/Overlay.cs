using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI
{
    public class Overlay : UIElement
    {
        public Container container;
        public bool isVisible;

        public Overlay(string path, Vector2 position, Vector2 dimensions) : base(path, position, dimensions)
        {
            container = new Container(path, position, dimensions);
            isVisible = false;
        }

        public Overlay(Texture2D texture, Vector2 position, Vector2 dimensions) : base(texture, position, dimensions)
        {
            container = new Container(texture, position, dimensions);
            isVisible = false;
        }

        public override void Update()
        {
            if (!isVisible) return;

            base.Update();
            container.Update();
        }

        public override void Draw(Vector2 offset)
        {
            if (!isVisible) return;

            base.Draw(offset, Vector2.Zero);
            container.Draw(offset, Vector2.Zero);
        }
    }
}
