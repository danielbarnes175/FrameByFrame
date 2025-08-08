using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;

namespace FrameByFrame.src.UI.Components
{
    public class BrushSizeSlider : Container
    {
        private FrameByFrame.src.Engine.Animation.Animation _animation;
        private UISlider _slider;
        private Texture2D _backgroundTexture;
        
        public bool IsMouseOver => _slider?.IsMouseOver ?? false;
        
        public BrushSizeSlider(Vector2 position, Vector2 dimensions, FrameByFrame.src.Engine.Animation.Animation animation) 
            : base((Texture2D)null, position, dimensions)
        {
            _animation = animation;
            
            // Create background texture matching the navbar orange
            _backgroundTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                UIConstants.UI_ORANGE, // Use the same orange as the navbar
                Math.Max((int)dimensions.X, (int)dimensions.Y), 
                Shapes.RECTANGLE
            );
            texture = _backgroundTexture;
            
            SetupSlider();
        }
        
        private void SetupSlider()
        {
            // Create the slider with padding from the container edges
            int padding = 10;
            
            Vector2 sliderPosition = new Vector2(
                position.X + padding,
                position.Y + 4
            );
            
            Vector2 sliderDimensions = new Vector2(
                dimensions.X - (padding * 2),
                dimensions.Y - 8
            );
            
            _slider = new UISlider(
                sliderPosition,
                sliderDimensions,
                UIConstants.MIN_BRUSH_SIZE,
                UIConstants.MAX_BRUSH_SIZE,
                _animation.brushSize
            );
            
            // Subscribe to value changes
            _slider.OnValueChanged = (newValue) =>
            {
                _animation.brushSize = newValue;
            };
            
            uiElements.Add(_slider);
        }
        
        public override void Update()
        {
            // Update slider value if brush size changed externally (e.g., keyboard shortcuts)
            if (_slider.Value != _animation.brushSize)
            {
                _slider.SetValue(_animation.brushSize);
            }
            
            base.Update();
        }
        
        public override void Draw(Vector2 offset, Vector2 origin)
        {
            // Draw background with subtle border effect
            var backgroundBounds = new Rectangle(
                (int)position.X + (int)offset.X,
                (int)position.Y + (int)offset.Y,
                (int)dimensions.X,
                (int)dimensions.Y
            );
            
            // Draw subtle border using darker orange
            var borderTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                new Color(180, 90, 0), // Darker orange border
                Math.Max((int)dimensions.X, (int)dimensions.Y), 
                Shapes.RECTANGLE
            );
            
            var borderBounds = new Rectangle(
                backgroundBounds.X - 1,
                backgroundBounds.Y - 1,
                backgroundBounds.Width + 2,
                backgroundBounds.Height + 2
            );
            GlobalParameters.GlobalSpriteBatch.Draw(borderTexture, borderBounds, Color.White);
            
            // Draw background
            GlobalParameters.GlobalSpriteBatch.Draw(_backgroundTexture, backgroundBounds, Color.White);
            
            // Draw child elements (slider)
            foreach (UIElement element in uiElements)
            {
                element.Draw(offset, origin);
            }
            
            // Removed the "Brush" label as requested
        }
    }
}