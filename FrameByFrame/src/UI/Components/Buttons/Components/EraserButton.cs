using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace FrameByFrame.src.UI.Components.Buttons.Components
{
    public class EraserButton : RadioButton
    {
        public EraserButton(Texture2D selectedTexture, Texture2D unselectedTexture, bool isSelected, Vector2 position, Vector2 dimensions) : base(selectedTexture, unselectedTexture, isSelected, position, dimensions) { }
        
        public override void Update()
        {
            // If selected, set the current tool to the eraser, provided it's not already set.
            if (isSelected)
            {
                ((DrawingScene)(GlobalParameters.Scenes["Drawing Scene"])).drawingTool = DrawingTools.ERASER;
            }
            base.Update();
        }
    }
}
