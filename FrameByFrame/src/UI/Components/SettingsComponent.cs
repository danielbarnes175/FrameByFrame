using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.Engine.Scenes;
using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.Services;
using FrameByFrame.src.UI.Components.Buttons;
using FrameByFrame.src.UI.Components.Buttons.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace FrameByFrame.src.UI.Components
{
    public class SettingsComponent : Overlay
    {
        public SettingsComponent(Vector2 position, Vector2 dimensions) : base((Texture2D)null, position, dimensions)
        {
            texture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, (int)dimensions.X, (int)dimensions.Y, pixel => Color.Orange, Engine.Shapes.RECTANGLE);
            this.SetColorData();

            // Add UIElements to container
            //"Static\\SettingsScene/Onion"
            container.uiElements.Add(new OnionSkinToggleButton("Static\\SettingsScene/onion_selected", "Static\\SettingsScene/onion", true, new Vector2(position.X + 10, position.Y + 10), new Vector2(50, 50)));

            Animation currentAnimation = ((DrawingScene)(GlobalParameters.Scenes["Drawing Scene"])).animation;
            Texture2D saveButtonTexture = DrawingService.CreateTexture(
                GlobalParameters.GlobalGraphics,
                167,
                50,
                pixel => new Color(220, 110, 0),
                Engine.Shapes.RECTANGLE);
            TriggerButton saveButton = new TriggerButton(
                saveButtonTexture,
                new Vector2(position.X + (dimensions.X / 2) - (167 / 2), position.Y + dimensions.Y - 60),
                new Vector2(167, 50),
                () => SaveService.SaveAnimation(currentAnimation));
            saveButton.text = "SAVE PROJECT";
            saveButton.textColor = Color.White;
            container.uiElements.Add(saveButton);
            //container.uiElements.Add(new Slider(new Vector2(position.X + 10, position.Y + 130), new Vector2(50, 200), 1, 10, 5));
        }
        public override void Update()
        {
            if (!isVisible) return;
            base.Update();
        }
        public override void Draw(Vector2 offset)
        {
            if (!isVisible) return;
            base.Draw(offset);
        }
    }
}
