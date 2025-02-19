using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.Engine;
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
        public DrawingNavbarComponent(Texture2D texture, Vector2 position, Vector2 dimensions, Animation animation) : base(texture, position, dimensions)
        {
            // Create Navbar child components
            Texture2D menuButtonTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 100, 32, pixel => new Color(255, 140, 0), Shapes.RECTANGLE);
            RedirectButton menuButton = new RedirectButton("Menu Scene", menuButtonTexture, new Vector2(5 * uiElements.Count, 10), new Vector2(100, 32), "MENU", Color.White);
            uiElements.Add(menuButton);

            Texture2D helpButtonTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 32, 32, pixel => new Color(100, 100, 200), Shapes.RECTANGLE);
            Overlay helpOverlay = new Overlay("Static\\SettingsScene/button_export", new Vector2(500, 500), new Vector2(32, 32));
            PopupButton helpButton = new PopupButton(helpOverlay, "Static\\DrawingScene/help", new Vector2(menuButton.position.X + menuButton.dimensions.X + 5 * uiElements.Count, 10), new Vector2(32, 32));
            uiElements.Add(helpButton);

            SettingsComponent settingsOverlay = new SettingsComponent(new Vector2(150, 55), new Vector2(450, 600));
            PopupButton settingsButton = new PopupButton(settingsOverlay, "Static\\DrawingScene/gear", new Vector2(helpButton.position.X + helpButton.dimensions.X + 5 * uiElements.Count, 10), new Vector2(32, 32));
            uiElements.Add(settingsButton);

            Texture2D colorButtonTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 32, 32, pixel => new Color(200, 0, 255), Shapes.CIRCLE);
            ColorWheelComponent colorOverlay = new ColorWheelComponent(new Vector2(1399, 50), new Vector2(200, 200));
            PopupButton colorButton = new PopupButton(colorOverlay, colorButtonTexture, new Vector2(GlobalParameters.screenWidth - colorButtonTexture.Width - 5, 10), new Vector2(32, 32));
            colorOverlay.OnColorSelected += (Color selectedColor) =>
            {
                colorButton.texture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 32, 32, pixel => selectedColor, Shapes.CIRCLE);
            };
            uiElements.Add(colorButton);

            List<string> layers = new List<string> { "_layer1", "_layer2", "_layer3" };
            LayerSelectorComponent layerOverlay = new LayerSelectorComponent(new Vector2(1400, 50), new Vector2(200, 150), layers, GlobalParameters.font);
            layerOverlay.OnLayerSelected = (selectedLayer) =>
            {
                animation.selectedLayer = selectedLayer;
            };
            PopupButton layerButton = new PopupButton(layerOverlay, "Static\\DrawingScene/layers", new Vector2(colorButton.position.X - colorButton.dimensions.X - 10, 10), new Vector2(32, 32));
            uiElements.Add(layerButton);

            Texture2D frameCounterTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 132, 32, pixel => Color.Orange, Shapes.RECTANGLE);
            UIElement frameCounter = new FrameCounterComponent(frameCounterTexture, new Vector2(settingsButton.position.X + settingsButton.dimensions.X, 10), new Vector2(frameCounterTexture.Width, frameCounterTexture.Height));
            uiElements.Add(frameCounter);

            // Create and add tool buttons
            List<RadioButton> buttons = new List<RadioButton>();

            EraserButton eraser = new EraserButton("Static\\DrawingScene/eraser_selected", "Static\\DrawingScene/eraser", false, new Vector2(layerButton.position.X - layerButton.dimensions.X - 15, 10), new Vector2(32, 32));
            DrawButton draw = new DrawButton("Static\\DrawingScene/brush_selected", "Static\\DrawingScene/brush", true, new Vector2(eraser.position.X - eraser.dimensions.X - 5, 10), new Vector2(32, 32));

            buttons.Add(draw);
            buttons.Add(eraser);

            ButtonGroup toolButtons = new ButtonGroup(buttons);
            buttonGroups.Add(toolButtons);

            Animation currentAnimation = ((DrawingScene)(GlobalParameters.Scenes["Drawing Scene"])).animation;
            TriggerButton goToStartButton = new TriggerButton("Static\\DrawingScene/first_frame", new Vector2(frameCounter.position.X + frameCounter.dimensions.X, 10), new Vector2(32, 32), () => animation.FirstFrame(), true);
            TriggerButton previousFrameButton = new TriggerButton("Static\\DrawingScene/previous_frame", new Vector2(goToStartButton.position.X + goToStartButton.dimensions.X + 10, 10), new Vector2(32, 32), () => animation.PreviousFrame(), true);
            TriggerButton playButton = new TriggerButton("Static\\DrawingScene/play", new Vector2(previousFrameButton.position.X + previousFrameButton.dimensions.X + 10, 10), new Vector2(32, 32), () => animation.TogglePlaying(), true);
            TriggerButton nextFrameButton = new TriggerButton("Static\\DrawingScene/next_frame", new Vector2(playButton.position.X + playButton.dimensions.X + 10, 10), new Vector2(32, 32), () => animation.NextFrame(), true);
            TriggerButton goToEndButton = new TriggerButton("Static\\DrawingScene/last_frame", new Vector2(nextFrameButton.position.X + nextFrameButton.dimensions.X + 10, 10), new Vector2(32, 32), () => animation.LastFrame(), true);

            uiElements.Add(goToStartButton);
            uiElements.Add(previousFrameButton);
            uiElements.Add(playButton);
            uiElements.Add(nextFrameButton);
            uiElements.Add(goToEndButton);
        }
    }
}
