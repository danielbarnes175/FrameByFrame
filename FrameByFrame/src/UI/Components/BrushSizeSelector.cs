using System;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.UI.Components
{
    public class BrushSizeSelector : UIElement
    {
        private FrameByFrame.src.Engine.Animation.Animation _animation;
        private UILayout _layout;
        private Rectangle _displayArea;
        
        public BrushSizeSelector(Vector2 position, Vector2 dimensions, FrameByFrame.src.Engine.Animation.Animation animation) 
            : base((Texture2D)null, position, dimensions)
        {
            _animation = animation;
            
            // Create background texture
            texture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                UIConstants.UI_ORANGE, 
                Math.Max((int)dimensions.X, (int)dimensions.Y), 
                Shapes.RECTANGLE
            );
            
            SetupLayout();
        }
        
        private void SetupLayout()
        {
            _layout = new UILayout();
            int buttonSize = 20;
            int spacing = 5;
            
            // Calculate display area (center part for showing brush size)
            _displayArea = new Rectangle(
                (int)position.X + buttonSize + spacing * 2,
                (int)position.Y + 5,
                (int)dimensions.X - (buttonSize * 2 + spacing * 4),
                (int)dimensions.Y - 10
            );
            
            // Decrease button (-)
            var decreaseBounds = new Rectangle(
                (int)position.X + spacing, 
                (int)position.Y + (int)(dimensions.Y - buttonSize) / 2,
                buttonSize, 
                buttonSize
            );
            var decreaseTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                Color.DarkRed, 
                buttonSize, 
                Shapes.RECTANGLE
            );
            
            var decreaseButton = new UIButton(decreaseBounds, decreaseTexture, () => {
                if (_animation.brushSize > UIConstants.MIN_BRUSH_SIZE)
                {
                    _animation.brushSize--;
                }
            });
            decreaseButton.Text = "-";
            decreaseButton.TextColor = Color.White;
            _layout.AddButton(decreaseButton);
            
            // Increase button (+)
            var increaseBounds = new Rectangle(
                (int)position.X + (int)dimensions.X - buttonSize - spacing,
                (int)position.Y + (int)(dimensions.Y - buttonSize) / 2,
                buttonSize,
                buttonSize
            );
            var increaseTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                Color.DarkGreen, 
                buttonSize, 
                Shapes.RECTANGLE
            );
            
            var increaseButton = new UIButton(increaseBounds, increaseTexture, () => {
                if (_animation.brushSize < UIConstants.MAX_BRUSH_SIZE)
                {
                    _animation.brushSize++;
                }
            });
            increaseButton.Text = "+";
            increaseButton.TextColor = Color.White;
            _layout.AddButton(increaseButton);
        }
        
        public override void Update()
        {
            base.Update();
            
            // Handle mouse clicks on buttons
            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                _layout.HandleClick(GlobalParameters.GlobalMouse.newMousePos);
            }
        }
        
        public override void Draw(Vector2 offset, Vector2 origin)
        {
            // Draw background
            base.Draw(offset, origin);
            
            // Draw buttons
            _layout.Draw(offset);
            
            // Draw current brush size in the center
            string brushSizeText = $"Brush: {_animation.brushSize}";
            var textSize = GlobalParameters.font.MeasureString(brushSizeText);
            var textPosition = new Vector2(
                _displayArea.X + (_displayArea.Width - textSize.X) / 2,
                _displayArea.Y + (_displayArea.Height - textSize.Y) / 2
            ) + offset;
            
            GlobalParameters.GlobalSpriteBatch.DrawString(
                GlobalParameters.font, 
                brushSizeText, 
                textPosition, 
                Color.Black
            );
        }
    }
}