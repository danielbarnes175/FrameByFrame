using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI
{
    public class Container : UIElement
    {
        public List<UIElement> uiElements;

        public Container(string path, Vector2 position, Vector2 dimensions) : base(path, position, dimensions)
        {
            uiElements = new List<UIElement>();
        }

        public Container(Texture2D texture, Vector2 position, Vector2 dimensions) : base(texture, position, dimensions)
        {
            uiElements = new List<UIElement>();
        }

        public override void Update()
        {
            foreach (UIElement element in uiElements)
            {
                element.Update();
            }

            base.Update();
        }

        public override void Draw(Vector2 offset)
        {
            foreach (UIElement element in uiElements)
            {
                element.Draw(offset);
            }

            base.Draw(offset);
        }
    }
}
