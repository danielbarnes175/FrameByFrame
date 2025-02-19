using FrameByFrame.src.Engine;
using FrameByFrame.src.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameByFrame.src.UI.Components.Buttons
{
    public class ToggleButton : Button
    {
        public bool isToggled;
        public Texture2D toggledTexture;
        public Texture2D untoggledTexture;

        public BasicTexture toggledBasicTexture;
        public BasicTexture untoggledBasicTexture;

        public ToggleButton(string toggledTexturePath, string untoggledTexturePath, bool isToggled, Vector2 position, Vector2 dimensions)
            : base(untoggledTexturePath, position, dimensions)
        {
            toggledTexture = GlobalParameters.GlobalContent.Load<Texture2D>(toggledTexturePath);
            untoggledTexture = GlobalParameters.GlobalContent.Load<Texture2D>(untoggledTexturePath);

            toggledBasicTexture = new BasicTexture(toggledTexture, position, dimensions);
            untoggledBasicTexture = new BasicTexture(untoggledTexture, position, dimensions);

            this.isToggled = isToggled;
        }

        public ToggleButton(Texture2D toggledTexture, Texture2D untoggledTexture, bool isToggled, Vector2 position, Vector2 dimensions)
            : base(untoggledTexture, position, dimensions)
        {
            this.toggledTexture = toggledTexture;
            this.untoggledTexture = untoggledTexture;

            toggledBasicTexture = new BasicTexture(toggledTexture, position, dimensions);
            untoggledBasicTexture = new BasicTexture(untoggledTexture, position, dimensions);

            this.isToggled = isToggled;
        }

        public override void Update()
        {
            isBeingMousedOver = CollisionService.CheckMouseCollision(isToggled ? toggledBasicTexture : untoggledBasicTexture, true);

            if (isBeingMousedOver && GlobalParameters.GlobalMouse.LeftClick())
            {
                Toggle();
            }
        }

        public override void Draw(Vector2 offset, Vector2 origin)
        {
            Texture2D currentTexture = isToggled ? toggledTexture : untoggledTexture;

            GlobalParameters.GlobalSpriteBatch.Draw(currentTexture,
                new Rectangle((int)(position.X + offset.X), (int)(position.Y + offset.Y), (int)dimensions.X,
                    (int)dimensions.Y), null, Color.White, rotation, new Vector2(origin.X, origin.Y),
                new SpriteEffects(), 0.2f);
        }

        public void Toggle()
        {
            isToggled = !isToggled;
        }
    }
}
