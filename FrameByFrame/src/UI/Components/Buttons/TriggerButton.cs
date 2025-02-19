using FrameByFrame.src.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameByFrame.src.UI.Components.Buttons
{
    public class TriggerButton : Button
    {
        private Action onClick;

        public TriggerButton(string texturePath, Vector2 position, Vector2 dimensions, Action onClick, bool ignoreOpacityOnHover = false)
            : base(texturePath, position, dimensions, ignoreOpacityOnHover)
        {
            this.onClick = onClick;
        }

        public TriggerButton(Texture2D texture, Vector2 position, Vector2 dimensions, Action onClick, bool ignoreOpacityOnHover = false)
            : base(texture, position, dimensions, ignoreOpacityOnHover)
        {
            this.onClick = onClick;
        }

        public override void Update()
        {
            isBeingMousedOver = CollisionService.CheckMouseCollision(this, ignoreOpacityOnHover);

            if (isBeingMousedOver && GlobalParameters.GlobalMouse.LeftClick())
            {
                onClick?.Invoke();
            }
        }
    }

}
