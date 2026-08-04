using System;
using FrameByFrame.src.Engine.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.UI.Components
{
    public sealed class HelpComponent : Overlay
    {
        public HelpComponent(Vector2 position, Vector2 dimensions) : base((Texture2D)null, position, dimensions)
        {
            texture = TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, UITheme.Surface,
                Math.Max((int)dimensions.X, (int)dimensions.Y));
            SetColorData();
        }

        public override void Draw(Vector2 offset)
        {
            if (!isVisible) return;
            base.Draw(offset);
            int x = (int)(position.X + offset.X);
            int y = (int)(position.Y + offset.Y);
            int width = (int)dimensions.X;

            new UITextContainer
            {
                Bounds = new Rectangle(x + 22, y + 18, width - 44, 48),
                HorizontalAlignment = UIAlign.Start,
                MaxLines = 1
            }.Draw("Keyboard shortcuts", UITheme.Primary, 1.05f);

            string shortcuts =
                "P - Play or pause\n" +
                "N / M - Previous or next frame\n" +
                "B - Insert a frame\n" +
                "Delete - Remove current frame\n" +
                "O - Toggle onion skin\n" +
                "[ / ] - Change brush size\n" +
                "Backspace - Clear selected layer\n" +
                "L - Save project\n" +
                "Esc - Return home";

            new UITextContainer
            {
                Bounds = new Rectangle(x + 22, y + 72, width - 44, (int)dimensions.Y - 92),
                HorizontalAlignment = UIAlign.Start,
                VerticalAlignment = UIAlign.Start,
                Padding = 4,
                MaxLines = 12
            }.Draw(shortcuts, UITheme.Text, .85f);
        }
    }
}
