using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.UI.Components.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FrameByFrame.src.UI.Components
{
    public class BrushSizeComponent : Container
    {
        private FrameByFrame.src.Engine.Animation.Animation _animation;
        private TriggerButton _decreaseButton;
        private TriggerButton _increaseButton;
        private Texture2D _backgroundTexture;
        
        public BrushSizeComponent(Vector2 position, Vector2 dimensions, FrameByFrame.src.Engine.Animation.Animation animation) 
            : base((Texture2D)null, position, dimensions)
        {
            _animation = animation;
            
            // Create background texture
            _backgroundTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                UIConstants.UI_ORANGE, 
                Math.Max((int)dimensions.X, (int)dimensions.Y), 
                Shapes.RECTANGLE
            );
            texture = _backgroundTexture;
            
            SetupButtons();
        }
        
        private void SetupButtons()
        {
            int buttonSize = 20;
            int spacing = 5;
            
            // Decrease button (-)
            var decreaseTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                Color.DarkRed, 
                buttonSize, 
                Shapes.RECTANGLE
            );
            
            _decreaseButton = new TriggerButton(
                decreaseTexture,
                new Vector2(position.X + spacing, position.Y + (dimensions.Y - buttonSize) / 2),
                new Vector2(buttonSize, buttonSize),
                () => DecreaseBrushSize(),
                true
            );
            _decreaseButton.text = "-";
            _decreaseButton.textColor = Color.White;
            uiElements.Add(_decreaseButton);
            
            // Increase button (+)
            var increaseTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                Color.DarkGreen, 
                buttonSize, 
                Shapes.RECTANGLE
            );
            
            _increaseButton = new TriggerButton(
                increaseTexture,
                new Vector2(position.X + dimensions.X - buttonSize - spacing, position.Y + (dimensions.Y - buttonSize) / 2),
                new Vector2(buttonSize, buttonSize),
                () => IncreaseBrushSize(),
                true
            );
            _increaseButton.text = "+";
            _increaseButton.textColor = Color.White;
            uiElements.Add(_increaseButton);
        }
        
        private void DecreaseBrushSize()
        {
            if (_animation.brushSize > UIConstants.MIN_BRUSH_SIZE)
            {
                _animation.brushSize--;
            }
        }
        
        private void IncreaseBrushSize()
        {
            if (_animation.brushSize < UIConstants.MAX_BRUSH_SIZE)
            {
                _animation.brushSize++;
            }
        }
        
        public override void Draw(Vector2 offset, Vector2 origin)
        {
            // Draw background
            base.Draw(offset, origin);
            
            // Draw current brush size in the center
            string brushSizeText = _animation.brushSize.ToString();
            var textSize = GlobalParameters.font.MeasureString(brushSizeText);
            var textPosition = new Vector2(
                position.X + (dimensions.X - textSize.X) / 2,
                position.Y + (dimensions.Y - textSize.Y) / 2
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