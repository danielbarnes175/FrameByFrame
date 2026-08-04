using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FrameByFrame.src.UI.Components.Buttons.Components
{
    public sealed class DrawingToolButton : RadioButton
    {
        private readonly DrawingTools _tool;
        private readonly Texture2D _icon;

        public DrawingToolButton(DrawingTools tool, Texture2D icon,
            Vector2 position, Vector2 dimensions)
            : base(icon, icon, false, position, dimensions)
        {
            _tool = tool;
            _icon = icon;
        }

        public override void Update()
        {
            if (isSelected)
                ((DrawingScene)GlobalParameters.Scenes[UIConstants.DRAWING_SCENE]).drawingTool = _tool;
            base.Update();
        }

        public override void Draw(Vector2 offset, Vector2 origin)
        {
            Rectangle bounds = new(
                (int)(position.X + offset.X),
                (int)(position.Y + offset.Y),
                (int)(dimensions.X * GlobalParameters.scaleX),
                (int)(dimensions.Y * GlobalParameters.scaleY));

            Rectangle visualBounds = bounds;
            if (isSelected)
            {
                visualBounds.Y -= 2;
                UIRenderer.Fill(new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width, bounds.Height), new Color(0, 0, 0, 28));
                UIRenderer.Fill(visualBounds, UITheme.ToolSelectedSurface);
                UIRenderer.Border(visualBounds, UITheme.ToolSelected, 2);
            }

            Color iconColor = isBeingMousedOver && !isSelected ? Color.White * 0.72f : Color.White;
            GlobalParameters.GlobalSpriteBatch.Draw(_icon, visualBounds, iconColor);
        }
    }
}
