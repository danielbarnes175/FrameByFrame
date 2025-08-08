using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FrameByFrame.src.Engine.Services;

namespace FrameByFrame.src.Engine.UI
{
    public class UIButton
    {
        public Rectangle Bounds { get; set; }
        public Texture2D Texture { get; set; }
        public Action OnClick { get; set; }
        public string Text { get; set; }
        public Color TextColor { get; set; } = Color.Black;
        
        public UIButton(Rectangle bounds, Texture2D texture = null, Action onClick = null)
        {
            Bounds = bounds;
            Texture = texture;
            OnClick = onClick;
        }
        
        public bool IsClicked(Vector2 mousePos)
        {
            return Bounds.Contains(mousePos);
        }
        
        public void Draw(Vector2 offset)
        {
            if (Texture != null)
            {
                var drawBounds = new Rectangle(
                    Bounds.X + (int)offset.X,
                    Bounds.Y + (int)offset.Y,
                    Bounds.Width,
                    Bounds.Height
                );
                GlobalParameters.GlobalSpriteBatch.Draw(Texture, drawBounds, Color.White);
            }
            
            if (!string.IsNullOrEmpty(Text))
            {
                var textSize = GlobalParameters.font.MeasureString(Text);
                var textPos = new Vector2(
                    Bounds.X + (Bounds.Width - textSize.X) / 2,
                    Bounds.Y + (Bounds.Height - textSize.Y) / 2
                ) + offset;
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, Text, textPos, TextColor);
            }
        }
    }
    
    public class UILayout
    {
        private List<UIButton> _buttons = new List<UIButton>();
        
        public void AddButton(UIButton button)
        {
            _buttons.Add(button);
        }
        
        public void HandleClick(Vector2 mousePos)
        {
            foreach (var button in _buttons)
            {
                if (button.IsClicked(mousePos))
                {
                    button.OnClick?.Invoke();
                    break;
                }
            }
        }
        
        public void Draw(Vector2 offset)
        {
            foreach (var button in _buttons)
            {
                button.Draw(offset);
            }
        }
        
        // Helper method to create color palette
        public static UILayout CreateColorPalette(Vector2 startPos, int buttonSize, int spacing)
        {
            var layout = new UILayout();
            var colors = new[] { Color.Black, Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.White };
            
            for (int i = 0; i < colors.Length; i++)
            {
                var pos = startPos + new Vector2(i * (buttonSize + spacing), 0);
                var bounds = new Rectangle((int)pos.X, (int)pos.Y, buttonSize, buttonSize);
                var texture = TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, colors[i], buttonSize);
                var color = colors[i];
                
                layout.AddButton(new UIButton(bounds, texture, () => {
                    GlobalParameters.CurrentColor = color;
                }));
            }
            
            return layout;
        }
    }
}