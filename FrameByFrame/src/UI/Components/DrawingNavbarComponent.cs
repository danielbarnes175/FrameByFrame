using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.UI;
using FrameByFrame.src.UI.Components.Buttons.Components;
using FrameByFrame.src.UI.Components.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Scenes;

namespace FrameByFrame.src.UI.Components
{
    public class DrawingNavbarComponent : Container
    {
        private readonly Animation _animation;

        public DrawingNavbarComponent(Texture2D texture, Vector2 position, Vector2 dimensions, Animation animation) : base(texture, position, dimensions)
        {
            _animation = animation;
            // Create Navbar child components
            const int controlY = 12;
            Texture2D menuButtonTexture = TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, UITheme.Primary, 112);
            RedirectButton menuButton = new RedirectButton("Menu Scene", menuButtonTexture, new Vector2(12, controlY), new Vector2(112, 40), "HOME", Color.White);
            uiElements.Add(menuButton);

            Texture2D helpButtonTexture = TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, UITheme.SurfaceRaised, 40);
            HelpComponent helpOverlay = new HelpComponent(new Vector2(132, 72), new Vector2(520, 410));
            PopupButton helpButton = new PopupButton(helpOverlay, "Static\\DrawingScene/help", new Vector2(menuButton.position.X + menuButton.dimensions.X + 10, 16), new Vector2(32, 32));
            uiElements.Add(helpButton);
            
            // Register popup button with UI interaction manager
            UIInteractionManager.RegisterUIElement(() => helpButton.target.isVisible);

            SettingsComponent settingsOverlay = new SettingsComponent(new Vector2(174, 72), new Vector2(450, 330));
            PopupButton settingsButton = new PopupButton(settingsOverlay, "Static\\DrawingScene/gear", new Vector2(helpButton.position.X + helpButton.dimensions.X + 10, 16), new Vector2(32, 32));
            uiElements.Add(settingsButton);
            
            // Register settings popup
            UIInteractionManager.RegisterUIElement(() => settingsButton.target.isVisible);

            Texture2D colorButtonTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 32, 32, pixel => new Color(200, 0, 255), Shapes.CIRCLE);
            ColorWheelComponent colorOverlay = new ColorWheelComponent(new Vector2(GlobalParameters.screenWidth - 248, 72), new Vector2(236, 200));
            PopupButton colorButton = new PopupButton(colorOverlay, colorButtonTexture, new Vector2(GlobalParameters.screenWidth - 48, 16), new Vector2(32, 32));
            Color[] swatchPixels = new Color[32 * 32];
            void UpdateColorSwatch(Color selectedColor)
            {
                const int outerRadiusSquared = 15 * 15;
                const int innerRadiusSquared = 13 * 13;
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        int dx = x - 16;
                        int dy = y - 16;
                        int distanceSquared = dx * dx + dy * dy;
                        swatchPixels[x + y * 32] = distanceSquared > outerRadiusSquared
                            ? Color.Transparent
                            : distanceSquared >= innerRadiusSquared ? Color.Black : selectedColor;
                    }
                }
                colorButtonTexture.SetData(swatchPixels);
            }
            colorOverlay.OnColorSelected += UpdateColorSwatch;
            UpdateColorSwatch(new Color(200, 0, 255));
            uiElements.Add(colorButton);
            
            // Register color picker popup
            UIInteractionManager.RegisterUIElement(() => colorButton.target.isVisible);

            List<string> layers = new List<string> { "_layer1", "_layer2", "_layer3" };
            LayerSelectorComponent layerOverlay = new LayerSelectorComponent(new Vector2(GlobalParameters.screenWidth - 212, 72), new Vector2(200, 150), layers, GlobalParameters.font);
            layerOverlay.OnLayerSelected = (selectedLayer) =>
            {
                animation.selectedLayer = selectedLayer;
            };
            PopupButton layerButton = new PopupButton(layerOverlay, "Static\\DrawingScene/layers", new Vector2(colorButton.position.X - colorButton.dimensions.X - 12, 16), new Vector2(32, 32));
            uiElements.Add(layerButton);
            
            // Register layer selector popup
            UIInteractionManager.RegisterUIElement(() => layerButton.target.isVisible);

            Texture2D frameCounterTexture = TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, UITheme.Primary, 140);
            UIElement frameCounter = new FrameCounterComponent(frameCounterTexture, new Vector2(settingsButton.position.X + settingsButton.dimensions.X + 14, controlY), new Vector2(140, 40));
            uiElements.Add(frameCounter);

            // Create and add tool buttons
            List<RadioButton> buttons = new List<RadioButton>();

            Texture2D brushIcon = GlobalParameters.GlobalContent.Load<Texture2D>("Static\\DrawingScene/brush");
            Texture2D eraserIcon = GlobalParameters.GlobalContent.Load<Texture2D>("Static\\DrawingScene/eraser");
            Texture2D bucketIcon = GlobalParameters.GlobalContent.Load<Texture2D>("bucket_tool");
            Texture2D eyedropperIcon = GlobalParameters.GlobalContent.Load<Texture2D>("eyedropper_tool");
            DrawingToolButton picker = new DrawingToolButton(DrawingTools.COLOR_PICKER, eyedropperIcon,
                new Vector2(layerButton.position.X - layerButton.dimensions.X - 16, 16), new Vector2(32, 32));
            DrawingToolButton fill = new DrawingToolButton(DrawingTools.FILL, bucketIcon,
                new Vector2(picker.position.X - picker.dimensions.X - 8, 16), new Vector2(32, 32));
            DrawingToolButton eraser = new DrawingToolButton(DrawingTools.ERASER, eraserIcon,
                new Vector2(fill.position.X - fill.dimensions.X - 8, 16), new Vector2(32, 32));
            DrawingToolButton draw = new DrawingToolButton(DrawingTools.DRAW, brushIcon,
                new Vector2(eraser.position.X - eraser.dimensions.X - 8, 16), new Vector2(32, 32));
            draw.isSelected = true;

            buttons.Add(draw);
            buttons.Add(eraser);
            buttons.Add(fill);
            buttons.Add(picker);

            ButtonGroup toolButtons = new ButtonGroup(buttons);
            buttonGroups.Add(toolButtons);

            TriggerButton goToStartButton = new TriggerButton("Static\\DrawingScene/first_frame", new Vector2(frameCounter.position.X + frameCounter.dimensions.X + 12, 16), new Vector2(32, 32), () => animation.FirstFrame(), true);
            TriggerButton previousFrameButton = new TriggerButton("Static\\DrawingScene/previous_frame", new Vector2(goToStartButton.position.X + goToStartButton.dimensions.X + 8, 16), new Vector2(32, 32), () => animation.PreviousFrame(), true);
            TriggerButton playButton = new TriggerButton("Static\\DrawingScene/play", new Vector2(previousFrameButton.position.X + previousFrameButton.dimensions.X + 8, 16), new Vector2(32, 32), () => animation.TogglePlaying(), true);
            TriggerButton nextFrameButton = new TriggerButton("Static\\DrawingScene/next_frame", new Vector2(playButton.position.X + playButton.dimensions.X + 8, 16), new Vector2(32, 32), () => animation.NextFrame(), true);
            TriggerButton goToEndButton = new TriggerButton("Static\\DrawingScene/last_frame", new Vector2(nextFrameButton.position.X + nextFrameButton.dimensions.X + 8, 16), new Vector2(32, 32), () => animation.LastFrame(), true);

            uiElements.Add(goToStartButton);
            uiElements.Add(previousFrameButton);
            uiElements.Add(playButton);
            uiElements.Add(nextFrameButton);
            uiElements.Add(goToEndButton);

            // Add brush size slider
            BrushSizeSlider brushSizeSlider = new BrushSizeSlider(
                new Vector2(goToEndButton.position.X + goToEndButton.dimensions.X + 16, controlY),
                new Vector2(140, 40),
                animation
            );
            uiElements.Add(brushSizeSlider);
            
            // Register the brush size slider with the UI interaction manager
            UIInteractionManager.RegisterUIElement(() => brushSizeSlider.IsMouseOver);
        }

        public override void Draw(Vector2 offset, Vector2 origin)
        {
            base.Draw(offset, origin);

            Rectangle divider = new(0, UIConstants.NAVBAR_HEIGHT - 3, GlobalParameters.screenWidth, 3);
            UIRenderer.Fill(divider, UITheme.Primary);

            if (GlobalParameters.screenWidth >= 1200)
            {
                int labelX = 740;
                int labelWidth = Math.Max(1, GlobalParameters.screenWidth - labelX - 390);
                new UITextContainer
                {
                    Bounds = new Rectangle(labelX, 8, labelWidth, 48),
                    MaxLines = 1
                }.Draw(_animation.projectName, UITheme.TextMuted, .9f);
            }
        }
    }
}
