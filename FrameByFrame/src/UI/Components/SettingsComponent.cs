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
        private readonly Animation _animation;

        public SettingsComponent(Vector2 position, Vector2 dimensions) : base((Texture2D)null, position, dimensions)
        {
            texture = TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, UITheme.Surface, Math.Max((int)dimensions.X, (int)dimensions.Y));
            this.SetColorData();

            _animation = ((DrawingScene)(GlobalParameters.Scenes["Drawing Scene"])).animation;
            container.uiElements.Add(new OnionSkinToggleButton("Static\\SettingsScene/onion_selected", "Static\\SettingsScene/Onion", _animation.isOnionSkinEnabled, new Vector2(position.X + 28, position.Y + 86), new Vector2(48, 48)));

            Texture2D adjustmentTexture = TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, UITheme.SurfaceRaised, 48);
            TriggerButton decreaseFps = new TriggerButton(adjustmentTexture,
                new Vector2(position.X + 205, position.Y + 151), new Vector2(48, 42),
                () => _animation.fps = Math.Max(1, _animation.fps - 1));
            decreaseFps.text = "-";
            decreaseFps.textColor = UITheme.Text;
            container.uiElements.Add(decreaseFps);

            TriggerButton increaseFps = new TriggerButton(adjustmentTexture,
                new Vector2(position.X + 329, position.Y + 151), new Vector2(48, 42),
                () => _animation.fps = Math.Min(60, _animation.fps + 1));
            increaseFps.text = "+";
            increaseFps.textColor = UITheme.Text;
            container.uiElements.Add(increaseFps);

            Texture2D saveButtonTexture = DrawingService.CreateTexture(
                GlobalParameters.GlobalGraphics,
                167,
                50,
                pixel => UITheme.Primary,
                Engine.Shapes.RECTANGLE);
            TriggerButton saveButton = new TriggerButton(
                saveButtonTexture,
                new Vector2(position.X + (dimensions.X / 2) - 105, position.Y + dimensions.Y - 70),
                new Vector2(210, 50),
                () => SaveService.SaveAnimation(_animation));
            saveButton.text = "SAVE PROJECT";
            saveButton.textColor = Color.White;
            container.uiElements.Add(saveButton);
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
            int x = (int)(position.X + offset.X);
            int y = (int)(position.Y + offset.Y);
            int width = (int)dimensions.X;
            new UITextContainer { Bounds = new Rectangle(x + 24, y + 16, width - 48, 48), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Animation settings", UITheme.Primary, .72f);
            new UITextContainer { Bounds = new Rectangle(x + 88, y + 89, width - 112, 42), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw($"Onion skin: {(_animation.isOnionSkinEnabled ? "On" : "Off")}", UITheme.Text, .6f);
            new UITextContainer { Bounds = new Rectangle(x + 28, y + 151, 160, 42), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Playback FPS", UITheme.Text, .6f);
            new UITextContainer { Bounds = new Rectangle(x + 255, y + 151, 72, 42), MaxLines = 1 }
                .Draw(_animation.fps.ToString(), UITheme.Primary, .65f);
        }
    }
}
