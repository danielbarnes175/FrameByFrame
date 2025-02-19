using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Engine;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace FrameByFrame.src.UI.Components.Buttons.Components
{
    public class OnionSkinToggleButton : ToggleButton
    {
        public OnionSkinToggleButton(string selectedTexturePath, string unselectedTexturePath, bool isSelected, Vector2 position, Vector2 dimensions) 
            : base(selectedTexturePath, unselectedTexturePath, isSelected, position, dimensions) { }
        public OnionSkinToggleButton(Texture2D selectedTexture, Texture2D unselectedTexture, bool isSelected, Vector2 position, Vector2 dimensions) 
            : base(selectedTexture, unselectedTexture, isSelected, position, dimensions) { }

        public override void Update()
        {
            base.Update();

            // If selected, toggle the onion skin
            ((DrawingScene)(GlobalParameters.Scenes["Drawing Scene"])).animation.isOnionSkinEnabled = isToggled;
        }
    }
}
