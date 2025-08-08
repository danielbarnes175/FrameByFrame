using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;

namespace FrameByFrame.src.UI.Components
{
    public class UISlider : UIElement
    {
        // Properties
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public int Value { get; private set; }
        
        // Events
        public Action<int> OnValueChanged { get; set; }
        
        // Visual components
        private Rectangle _trackBounds;
        private Rectangle _handleBounds;
        private Texture2D _trackTexture;
        private Texture2D _handleTexture;
        private Texture2D _fillTexture;
        
        // Interaction state
        private bool _isDragging = false;
        private bool _isHovering = false;
        private int _handleWidth = 14;
        private int _trackHeight = 4;
        
        // Click blocking
        public bool IsMouseOver { get; private set; }
        
        public UISlider(Vector2 position, Vector2 dimensions, int minValue, int maxValue, int initialValue) 
            : base((Texture2D)null, position, dimensions)
        {
            MinValue = minValue;
            MaxValue = maxValue;
            Value = Math.Clamp(initialValue, MinValue, MaxValue);
            
            SetupSliderComponents();
            UpdateHandlePosition();
        }
        
        private void SetupSliderComponents()
        {
            // Calculate track bounds (centered vertically in the slider area)
            _trackBounds = new Rectangle(
                (int)position.X + _handleWidth / 2,
                (int)position.Y + (int)(dimensions.Y - _trackHeight) / 2 + 8, // Offset down for value text
                (int)dimensions.X - _handleWidth,
                _trackHeight
            );
            
            // Create textures using app's color scheme - orange theme
            _trackTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                new Color(140, 70, 20), // Dark orange-brown track
                Math.Max(_trackBounds.Width, _trackBounds.Height), 
                Shapes.RECTANGLE
            );
            
            _handleTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                Color.White, 
                _handleWidth, 
                Shapes.CIRCLE
            );
            
            _fillTexture = TextureManager.GetOrCreateColorTexture(
                GlobalParameters.GlobalGraphics, 
                new Color(255, 200, 100), // Light orange fill
                Math.Max(_trackBounds.Width, _trackBounds.Height), 
                Shapes.RECTANGLE
            );
        }
        
        private void UpdateHandlePosition()
        {
            // Calculate handle position based on current value
            float valueRatio = (float)(Value - MinValue) / (MaxValue - MinValue);
            int handleCenterX = (int)(_trackBounds.X + valueRatio * _trackBounds.Width);
            
            _handleBounds = new Rectangle(
                handleCenterX - _handleWidth / 2,
                _trackBounds.Y - (_handleWidth - _trackHeight) / 2,
                _handleWidth,
                _handleWidth // Circular handle
            );
        }
        
        public override void Update()
        {
            base.Update();
            
            Vector2 mousePos = GlobalParameters.GlobalMouse.newMousePos;
            
            // Check if mouse is hovering over the slider
            Rectangle sliderArea = new Rectangle((int)position.X, (int)position.Y, (int)dimensions.X, (int)dimensions.Y);
            _isHovering = sliderArea.Contains(mousePos);
            IsMouseOver = _isHovering; // Export this for click blocking
            
            // Start dragging if mouse is pressed on the slider area
            if (GlobalParameters.GlobalMouse.LeftClick() && _isHovering)
            {
                _isDragging = true;
                UpdateValueFromMousePosition(mousePos);
            }
            
            // Continue dragging if mouse is held down
            if (_isDragging && GlobalParameters.GlobalMouse.LeftClickHold())
            {
                UpdateValueFromMousePosition(mousePos);
                IsMouseOver = true; // Keep blocking while dragging
            }
            
            // Stop dragging when mouse is released
            if (!GlobalParameters.GlobalMouse.LeftClickHold())
            {
                _isDragging = false;
            }
        }
        
        private void UpdateValueFromMousePosition(Vector2 mousePos)
        {
            // Clamp mouse position to track bounds
            float relativeX = Math.Clamp(mousePos.X - _trackBounds.X, 0, _trackBounds.Width);
            float ratio = relativeX / _trackBounds.Width;
            
            // Calculate new value
            int newValue = MinValue + (int)Math.Round(ratio * (MaxValue - MinValue));
            newValue = Math.Clamp(newValue, MinValue, MaxValue);
            
            // Update value if it changed
            if (newValue != Value)
            {
                Value = newValue;
                UpdateHandlePosition();
                OnValueChanged?.Invoke(Value);
            }
        }
        
        public void SetValue(int value)
        {
            int clampedValue = Math.Clamp(value, MinValue, MaxValue);
            if (clampedValue != Value)
            {
                Value = clampedValue;
                UpdateHandlePosition();
                // Don't trigger OnValueChanged for programmatic changes
            }
        }
        
        public override void Draw(Vector2 offset, Vector2 origin)
        {
            // Draw track background
            var trackDrawBounds = new Rectangle(
                _trackBounds.X + (int)offset.X,
                _trackBounds.Y + (int)offset.Y,
                _trackBounds.Width,
                _trackBounds.Height
            );
            GlobalParameters.GlobalSpriteBatch.Draw(_trackTexture, trackDrawBounds, Color.White);
            
            // Draw fill (from start to handle position)
            if (Value > MinValue)
            {
                float valueRatio = (float)(Value - MinValue) / (MaxValue - MinValue);
                int fillWidth = Math.Max(1, (int)(valueRatio * _trackBounds.Width));
                
                var fillDrawBounds = new Rectangle(
                    _trackBounds.X + (int)offset.X,
                    _trackBounds.Y + (int)offset.Y,
                    fillWidth,
                    _trackBounds.Height
                );
                GlobalParameters.GlobalSpriteBatch.Draw(_fillTexture, fillDrawBounds, Color.White);
            }
            
            // Draw handle with hover and drag states
            var handleDrawBounds = new Rectangle(
                _handleBounds.X + (int)offset.X,
                _handleBounds.Y + (int)offset.Y,
                _handleBounds.Width,
                _handleBounds.Height
            );
            
            // Handle color based on state - using orange theme
            Color handleColor = _isDragging ? new Color(255, 220, 150) : // Light orange when dragging
                               _isHovering ? new Color(255, 240, 200) :  // Very light orange when hovering
                               Color.White;                              // White normally
            
            // Draw handle shadow for depth
            var shadowBounds = new Rectangle(
                handleDrawBounds.X + 1,
                handleDrawBounds.Y + 1,
                handleDrawBounds.Width,
                handleDrawBounds.Height
            );
            GlobalParameters.GlobalSpriteBatch.Draw(_handleTexture, shadowBounds, new Color(80, 40, 10) * 0.5f); // Dark orange shadow
            GlobalParameters.GlobalSpriteBatch.Draw(_handleTexture, handleDrawBounds, handleColor);
            
            // Draw value text above the slider
            string valueText = Value.ToString();
            var textSize = GlobalParameters.font.MeasureString(valueText);
            var textPosition = new Vector2(
                position.X + (dimensions.X - textSize.X) / 2,
                position.Y + 2
            ) + offset;
            
            GlobalParameters.GlobalSpriteBatch.DrawString(
                GlobalParameters.font, 
                valueText, 
                textPosition, 
                Color.White
            );
        }
    }
}