﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI
{
    public class Container : UIElement
    {
        public List<UIElement> uiElements;
        public List<ButtonGroup> buttonGroups;

        public Container(string path, Vector2 position, Vector2 dimensions) : base(path, position, dimensions)
        {
            uiElements = new List<UIElement>();
            buttonGroups = new List<ButtonGroup>();
        }

        public Container(Texture2D texture, Vector2 position, Vector2 dimensions) : base(texture, position, dimensions)
        {
            uiElements = new List<UIElement>();
            buttonGroups = new List<ButtonGroup>();
        }

        public override void Update()
        {
            base.Update();

            foreach (UIElement element in uiElements)
            {
                element.Update();
            }

            foreach (ButtonGroup group in buttonGroups)
            {
                group.Update();
            }
        }

        public override void Draw(Vector2 offset, Vector2 origin)
        {
            base.Draw(offset, origin);
            foreach (UIElement element in uiElements)
            {
                element.Draw(offset, origin);
            }

            foreach (ButtonGroup group in buttonGroups)
            {
                group.Draw(offset, origin);
            }
        }
    }
}
