using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI.Components.Buttons
{
    public class RedirectButton : Button
    {
        public string target;

        public RedirectButton(string target, string path, Vector2 position, Vector2 dimensions, string text = null, Color? textColor = null) : base(path, position, dimensions)
        {
            this.target = target;
            this.text = text;
            this.textColor = (textColor is null) ? Color.Black : textColor;
            isBeingMousedOver = false;
        }

        public RedirectButton(string target, Texture2D texture, Vector2 position, Vector2 dimensions, string text = null, Color? textColor = null) : base(texture, position, dimensions)
        {
            this.target = target;
            this.text = text;
            this.textColor = (textColor is null) ? Color.Black : textColor;
            isBeingMousedOver = false;
        }

        public override void Update()
        {
            // Refresh hover state before handling the click so navigation uses
            // the current mouse position rather than the previous frame's state.
            base.Update();

            if (isBeingMousedOver && GlobalParameters.GlobalMouse.LeftClick())
            {
                if (target == "Projects Scene")
                {
                    ((ProjectsScene)GlobalParameters.Scenes["Projects Scene"]).LoadAnimations();
                }
                else if (target == "Drawing Scene")
                {
                    // Require the menu click to be released before the drawing
                    // scene accepts canvas input.
                    ((DrawingScene)GlobalParameters.Scenes["Drawing Scene"]).loadedScene = false;
                }

                GlobalParameters.CurrentScene = GlobalParameters.Scenes[target];
            }
        }
    }
}
