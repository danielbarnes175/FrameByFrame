using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameByFrame.src.UI.Components
{
    public class LayerSelectorComponent : Overlay
    {
        public Action<string> OnLayerSelected; // Callback for layer selection
        private List<string> layers;          // List of layer names
        private string selectedLayer;         // Currently selected layer
        private SpriteFont font;              // Font for displaying layer names

        public LayerSelectorComponent(Vector2 position, Vector2 dimensions, List<string> layers, SpriteFont font)
            : base((Texture2D)null, position, dimensions)
        {
            texture = TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, UITheme.Surface, Math.Max((int)dimensions.X, (int)dimensions.Y));
            this.SetColorData();
            this.layers = layers;
            this.font = font;
            this.selectedLayer = layers.Count > 0 ? layers[0] : null; // Default to the first layer
        }

        public override void Update()
        {
            base.Update();

            if (!isVisible) return;

            // Handle layer selection
            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                Vector2 mousePosition = GlobalParameters.GlobalMouse.newMousePos;

                if (CollisionService.CheckMouseCollision(this))
                {
                    // Calculate the index of the clicked layer
                    int layerHeight = (int)(dimensions.Y / layers.Count);
                    int clickedIndex = (int)((mousePosition.Y - position.Y) / layerHeight);

                    if (clickedIndex >= 0 && clickedIndex < layers.Count)
                    {
                        selectedLayer = layers[clickedIndex];
                        OnLayerSelected?.Invoke(selectedLayer); // Notify listeners
                    }
                }
            }
        }

        public override void Draw(Vector2 offset)
        {
            if (!isVisible) return;

            base.Draw(offset);

            // Draw the layer options
            int layerHeight = (int)(dimensions.Y / layers.Count);

            for (int i = 0; i < layers.Count; i++)
            {
                string layerName = layers[i];
                Rectangle layerRect = new Rectangle(
                    (int)((position.X + offset.X)),
                    (int)((position.Y + offset.Y + i * layerHeight)),
                    (int)dimensions.X,
                    layerHeight
                );

                // Highlight the selected layer
                Color backgroundColor = layerName == selectedLayer ? UITheme.Primary : UITheme.SurfaceRaised;
                GlobalParameters.GlobalSpriteBatch.Draw(
                    TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, Color.White, 1),
                    layerRect,
                    backgroundColor
                );

                // Draw the layer name
                Vector2 textSize = font.MeasureString(layerName);
                Vector2 textPosition = new Vector2(
                    layerRect.X + (layerRect.Width - textSize.X) / 2,
                    layerRect.Y + (layerRect.Height - textSize.Y) / 2
                );

                GlobalParameters.GlobalSpriteBatch.DrawString(
                    font,
                    layerName,
                    textPosition,
                    layerName == selectedLayer ? Color.White : UITheme.Text
                );
            }

            UIRenderer.Border(new Rectangle((int)(position.X + offset.X), (int)(position.Y + offset.Y),
                (int)dimensions.X, (int)dimensions.Y), UITheme.Primary, 3);
        }
    }
}
