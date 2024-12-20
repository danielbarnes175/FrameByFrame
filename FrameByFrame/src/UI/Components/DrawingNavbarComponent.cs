﻿using FrameByFrame.src.Engine.Services;
using FrameByFrame.src.Engine;
using FrameByFrame.src.UI.Components.Buttons.Components;
using FrameByFrame.src.UI.Components.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using FrameByFrame.src.Engine.Animation;

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

            Overlay settingsOverlay = new Overlay("Static\\SettingsScene/button_export", new Vector2(600, 500), new Vector2(32, 32));
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

            Texture2D eraserSelectedTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 32, 32, pixel => new Color(255, 0, 0), Shapes.RECTANGLE);
            Texture2D eraserUnselectedTexture = DrawingService.CreateTexture(GlobalParameters.GlobalGraphics, 32, 32, pixel => new Color(0, 255, 0), Shapes.RECTANGLE);
            EraserButton eraser = new EraserButton("Static\\DrawingScene/eraser_selected", "Static\\DrawingScene/eraser", false, new Vector2(layerButton.position.X - layerButton.dimensions.X - 15, 10), new Vector2(32, 32));
            DrawButton draw = new DrawButton("Static\\DrawingScene/brush_selected", "Static\\DrawingScene/brush", true, new Vector2(eraser.position.X - eraser.dimensions.X - 5, 10), new Vector2(32, 32));

            buttons.Add(draw);
            buttons.Add(eraser);

            ButtonGroup toolButtons = new ButtonGroup(buttons);
            // TODO ADD Clickable Buttons
            // +1 Frame, Last Frame, -1 Frame, First Frame, Play

            buttonGroups.Add(toolButtons);
        }
    }
}
