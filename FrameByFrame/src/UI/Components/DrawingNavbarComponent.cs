using System;
using System.Collections.Generic;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.Engine.Scenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.UI.Components
{
    public sealed class DrawingNavbarComponent : UIElement, IDisposable
    {
        private enum PopoverKind { None, Help, Settings, Color, Layers }

        private readonly Animation _animation;
        private readonly UIActionButton _home;
        private readonly UIIconButton _help;
        private readonly UIIconButton _settings;
        private readonly UIIconButton _color;
        private readonly UIIconButton _layers;
        private readonly List<UIIconButton> _tools = new();
        private readonly List<DrawingTools> _toolKinds = new();
        private readonly List<UIIconButton> _playback = new();
        private readonly UISlider _brushSize;
        private readonly UIToggle _onionSkin;
        private readonly UIActionButton _fpsDown;
        private readonly UIActionButton _fpsUp;
        private readonly UIActionButton _save;
        private readonly UIPopover _helpPopover = new();
        private readonly UIPopover _settingsPopover = new();
        private readonly UIPopover _colorPopover = new() { ShowOutline = false };
        private readonly UIPopover _layersPopover = new();
        private readonly Texture2D _colorWheelTexture;
        private readonly Texture2D _swatchTexture;
        private readonly Color[] _swatchPixels = new Color[32 * 32];
        private PopoverKind _openPopover;
        private Color _selectedColor = Color.Black;
        private float _selectedHue;
        private float _selectedSaturation;
        private float _selectedValue;

        public Color SelectedColor => _selectedColor;

        public DrawingNavbarComponent(Animation animation)
        {
            _animation = animation;
            _home = new UIActionButton("HOME", GoHome);
            _help = Icon("Static\\DrawingScene/help", () => Toggle(PopoverKind.Help), "Help");
            _settings = Icon("Static\\DrawingScene/gear", () => Toggle(PopoverKind.Settings), "Animation settings");
            _layers = Icon("Static\\DrawingScene/layers", () => Toggle(PopoverKind.Layers), "Layers");

            _swatchTexture = new Texture2D(GlobalParameters.GlobalGraphics, 32, 32);
            UpdateColorSwatch();
            _color = new UIIconButton(_swatchTexture, () => Toggle(PopoverKind.Color)) { Tooltip = "Brush color" };

            AddTool(DrawingTools.DRAW, "Static\\DrawingScene/brush", "Paintbrush");
            AddTool(DrawingTools.ERASER, "Static\\DrawingScene/eraser", "Eraser");
            AddTool(DrawingTools.FILL, "bucket_tool", "Bucket fill");
            AddTool(DrawingTools.COLOR_PICKER, "eyedropper_tool", "Color picker");

            _playback.Add(Icon("Static\\DrawingScene/first_frame", _animation.FirstFrame, "First frame"));
            _playback.Add(Icon("Static\\DrawingScene/previous_frame", _animation.PreviousFrame, "Previous frame - N"));
            _playback.Add(Icon("Static\\DrawingScene/play", _animation.TogglePlaying, "Play / pause - P"));
            _playback.Add(Icon("Static\\DrawingScene/next_frame", _animation.NextFrame, "Next frame - M"));
            _playback.Add(Icon("Static\\DrawingScene/last_frame", _animation.LastFrame, "Last frame"));

            _brushSize = new UISlider(UIConstants.MIN_BRUSH_SIZE, UIConstants.MAX_BRUSH_SIZE,
                _animation.brushSize, value => _animation.brushSize = value);
            _onionSkin = new UIToggle(_animation.isOnionSkinEnabled,
                value => _animation.isOnionSkinEnabled = value);
            _fpsDown = new UIActionButton("-", () => _animation.fps = Math.Max(1, _animation.fps - 1));
            _fpsUp = new UIActionButton("+", () => _animation.fps = Math.Min(60, _animation.fps + 1));
            _save = new UIActionButton("SAVE PROJECT", () => SaveService.SaveAnimation(_animation));
            _colorWheelTexture = GenerateColorPickerTexture(236, 200);
        }

        private UIIconButton Icon(string asset, Action action, string tooltip) =>
            new(GlobalParameters.GlobalContent.Load<Texture2D>(asset), action) { Tooltip = tooltip };

        private void AddTool(DrawingTools tool, string asset, string tooltip)
        {
            UIIconButton button = Icon(asset, () => SelectTool(tool), tooltip);
            button.UseToolSelectionStyle = true;
            _tools.Add(button);
            _toolKinds.Add(tool);
        }

        private void SelectTool(DrawingTools tool) =>
            ((DrawingScene)GlobalParameters.Scenes[UIConstants.DRAWING_SCENE]).drawingTool = tool;

        private void GoHome()
        {
            _animation.Stop();
            GlobalParameters.CurrentScene = GlobalParameters.Scenes[UIConstants.MENU_SCENE];
        }

        private void Toggle(PopoverKind kind) => _openPopover = _openPopover == kind ? PopoverKind.None : kind;

        public override void Arrange(Rectangle bounds)
        {
            base.Arrange(bounds);
            const int y = 16;
            _home.Arrange(new Rectangle(12, 12, 112, 40));
            _help.Arrange(new Rectangle(134, y, 32, 32));
            _settings.Arrange(new Rectangle(176, y, 32, 32));

            Rectangle frameCounter = new(222, 12, 140, 40);
            int x = frameCounter.Right + 12;
            foreach (UIIconButton button in _playback)
            {
                button.Arrange(new Rectangle(x, y, 32, 32));
                x += 40;
            }
            _brushSize.Arrange(new Rectangle(x + 8, 12, 140, 40));

            _color.Arrange(new Rectangle(bounds.Right - 48, y, 32, 32));
            _layers.Arrange(new Rectangle(_color.Bounds.X - 44, y, 32, 32));
            int toolX = _layers.Bounds.X - 48 - 32 * 4;
            foreach (UIIconButton tool in _tools)
            {
                tool.Arrange(new Rectangle(toolX, y, 32, 32));
                toolX += 40;
            }

            _helpPopover.Arrange(ClampPopover(new Rectangle(132, 72, 520, 410)));
            _settingsPopover.Arrange(ClampPopover(new Rectangle(174, 72, 450, 330)));
            _colorPopover.Arrange(ClampPopover(new Rectangle(bounds.Right - 248, 72, 236, 200)));
            _layersPopover.Arrange(ClampPopover(new Rectangle(bounds.Right - 212, 72, 200, 150)));

            Rectangle settings = _settingsPopover.Bounds;
            _onionSkin.Arrange(new Rectangle(settings.X + 28, settings.Y + 92, 48, 28));
            _fpsDown.Arrange(new Rectangle(settings.X + 205, settings.Y + 151, 48, 42));
            _fpsUp.Arrange(new Rectangle(settings.X + 329, settings.Y + 151, 48, 42));
            _save.Arrange(new Rectangle(settings.Center.X - 105, settings.Bottom - 70, 210, 50));
        }

        private static Rectangle ClampPopover(Rectangle bounds)
        {
            bounds.X = Math.Clamp(bounds.X, 8, Math.Max(8, GlobalParameters.screenWidth - bounds.Width - 8));
            bounds.Y = Math.Clamp(bounds.Y, UIConstants.NAVBAR_HEIGHT + 8,
                Math.Max(UIConstants.NAVBAR_HEIGHT + 8, GlobalParameters.screenHeight - bounds.Height - 8));
            return bounds;
        }

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            _home.Update();
            _help.Update();
            _settings.Update();
            _color.Update();
            _layers.Update();
            foreach (UIIconButton button in _tools) button.Update();
            foreach (UIIconButton button in _playback) button.Update();
            _brushSize.SetValue(_animation.brushSize);
            _brushSize.Update();

            _helpPopover.IsOpen = _openPopover == PopoverKind.Help;
            _settingsPopover.IsOpen = _openPopover == PopoverKind.Settings;
            _colorPopover.IsOpen = _openPopover == PopoverKind.Color;
            _layersPopover.IsOpen = _openPopover == PopoverKind.Layers;
            ActivePopover()?.Update();

            if (_settingsPopover.IsOpen) UpdateSettings();
            if (_layersPopover.IsOpen) UpdateLayers();
            if (_colorPopover.IsOpen) UpdateColorPicker();
            DismissPopoverOnOutsideClick();
        }

        private UIPopover ActivePopover() => _openPopover switch
        {
            PopoverKind.Help => _helpPopover,
            PopoverKind.Settings => _settingsPopover,
            PopoverKind.Color => _colorPopover,
            PopoverKind.Layers => _layersPopover,
            _ => null
        };

        private void UpdateSettings()
        {
            _onionSkin.Value = _animation.isOnionSkinEnabled;
            _onionSkin.Update();
            _fpsDown.Update();
            _fpsUp.Update();
            _save.Update();
        }

        private void UpdateLayers()
        {
            Rectangle panel = _layersPopover.Bounds;
            int rowHeight = panel.Height / 3;
            for (int i = 0; i < 3; i++)
            {
                Rectangle row = new(panel.X, panel.Y + i * rowHeight, panel.Width, rowHeight);
                if (UIPointerRouter.Clicked(row)) _animation.selectedLayer = $"_layer{i + 1}";
            }
        }

        private void UpdateColorPicker()
        {
            Rectangle bounds = _colorPopover.Bounds;
            if (!UIPointerRouter.Held(_colorPopover, bounds)) return;
            Vector2 local = GlobalParameters.GlobalMouse.newMousePos - new Vector2(bounds.X, bounds.Y);
            const int wheelDiameter = 200;
            const int stripX = 208;
            const int stripWidth = 28;
            float radius = (wheelDiameter - 1) / 2f;
            Vector2 offset = local - new Vector2(radius, radius);
            if (local.X >= 0 && local.Y >= 0 && local.X < wheelDiameter && local.Y < wheelDiameter && offset.Length() <= radius)
            {
                _selectedHue = MathHelper.ToDegrees((float)Math.Atan2(offset.Y, offset.X) + MathHelper.TwoPi) % 360;
                _selectedSaturation = Math.Clamp(offset.Length() / radius, 0f, 1f);
                _selectedValue = 1f;
                CommitColor();
            }
            else if (local.X >= stripX && local.X < stripX + stripWidth && local.Y >= 0 && local.Y < bounds.Height)
            {
                _selectedValue = Math.Clamp(1f - local.Y / Math.Max(1f, bounds.Height - 1f), 0f, 1f);
                CommitColor();
            }
        }

        private void DismissPopoverOnOutsideClick()
        {
            UIPopover active = ActivePopover();
            if (active == null || !GlobalParameters.GlobalMouse.LeftClick()) return;
            if (active.Bounds.Contains(GlobalParameters.GlobalMouse.newMousePos)) return;
            Rectangle anchor = _openPopover switch
            {
                PopoverKind.Help => _help.Bounds,
                PopoverKind.Settings => _settings.Bounds,
                PopoverKind.Color => _color.Bounds,
                PopoverKind.Layers => _layers.Bounds,
                _ => Rectangle.Empty
            };
            if (!anchor.Contains(GlobalParameters.GlobalMouse.newMousePos)) _openPopover = PopoverKind.None;
        }

        public void SetSelectedColor(Color color)
        {
            _selectedColor = color;
            RGBToHSV(color, out _selectedHue, out _selectedSaturation, out _selectedValue);
            UpdateColorSwatch();
        }

        private void CommitColor()
        {
            _selectedColor = HSVToRGB(_selectedHue, _selectedSaturation, _selectedValue);
            UpdateColorSwatch();
        }

        private void UpdateColorSwatch()
        {
            const int outer = 15 * 15;
            const int inner = 13 * 13;
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                int dx = x - 16;
                int dy = y - 16;
                int distance = dx * dx + dy * dy;
                _swatchPixels[x + y * 32] = distance > outer ? Color.Transparent : distance >= inner ? Color.Black : _selectedColor;
            }
            _swatchTexture.SetData(_swatchPixels);
        }

        public override void Draw()
        {
            UIRenderer.Fill(Bounds, UITheme.Surface);
            UIRenderer.Fill(new Rectangle(Bounds.X, Bounds.Bottom - 3, Bounds.Width, 3), UITheme.Primary);
            _home.Draw(true);
            _help.Draw();
            _settings.Draw();
            _color.Draw();
            _layers.Draw();
            for (int i = 0; i < _tools.Count; i++)
            {
                _tools[i].IsSelected = ((DrawingScene)GlobalParameters.Scenes[UIConstants.DRAWING_SCENE]).drawingTool == _toolKinds[i];
                _tools[i].Draw();
            }
            foreach (UIIconButton button in _playback) button.Draw();
            _brushSize.Draw();

            Rectangle frame = new(222, 12, 140, 40);
            UIRenderer.Fill(frame, UITheme.Primary);
            new UITextContainer { Bounds = frame, MaxLines = 1 }.Draw($"{_animation.CurrentFrameIndex + 1} / {_animation.TotalFrames}", Color.White, .78f);

            if (GlobalParameters.screenWidth >= 1200)
            {
                int labelX = _brushSize.Bounds.Right + 8;
                int labelWidth = Math.Max(1, _tools[0].Bounds.X - labelX - 8);
                new UITextContainer { Bounds = new Rectangle(labelX, 8, labelWidth, 48), MaxLines = 1 }
                    .Draw(_animation.projectName, UITheme.TextMuted, .72f);
            }

            DrawActivePopover();
        }

        private void DrawActivePopover()
        {
            switch (_openPopover)
            {
                case PopoverKind.Help: DrawHelp(); break;
                case PopoverKind.Settings: DrawSettings(); break;
                case PopoverKind.Color: DrawColorPicker(); break;
                case PopoverKind.Layers: DrawLayers(); break;
            }
        }

        private void DrawHelp()
        {
            _helpPopover.Draw();
            Rectangle panel = _helpPopover.Bounds;
            new UITextContainer { Bounds = new Rectangle(panel.X + 22, panel.Y + 18, panel.Width - 44, 48), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Keyboard shortcuts", UITheme.Primary, 1.05f);
            const string shortcuts = "P - Play or pause\nN / M - Previous or next frame\nB - Insert a frame\nDelete - Remove current frame\nO - Toggle onion skin\n[ / ] - Change brush size\nBackspace - Clear selected layer\nL - Save project\nEsc - Return home";
            new UITextContainer { Bounds = new Rectangle(panel.X + 22, panel.Y + 72, panel.Width - 44, panel.Height - 92), HorizontalAlignment = UIAlign.Start, VerticalAlignment = UIAlign.Start, Padding = 4, MaxLines = 12 }
                .Draw(shortcuts, UITheme.Text, .85f);
        }

        private void DrawSettings()
        {
            _settingsPopover.Draw();
            Rectangle panel = _settingsPopover.Bounds;
            new UITextContainer { Bounds = new Rectangle(panel.X + 24, panel.Y + 16, panel.Width - 48, 48), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Animation settings", UITheme.Primary, .72f);
            _onionSkin.Draw();
            new UITextContainer { Bounds = new Rectangle(panel.X + 88, panel.Y + 89, panel.Width - 112, 42), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw($"Onion skin: {(_animation.isOnionSkinEnabled ? "On" : "Off")}", UITheme.Text, .6f);
            new UITextContainer { Bounds = new Rectangle(panel.X + 28, panel.Y + 151, 160, 42), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Playback FPS", UITheme.Text, .6f);
            _fpsDown.Draw();
            _fpsUp.Draw();
            new UITextContainer { Bounds = new Rectangle(panel.X + 255, panel.Y + 151, 72, 42), MaxLines = 1 }
                .Draw(_animation.fps.ToString(), UITheme.Primary, .65f);
            _save.Draw(true);
        }

        private void DrawLayers()
        {
            _layersPopover.Draw();
            Rectangle panel = _layersPopover.Bounds;
            int rowHeight = panel.Height / 3;
            for (int i = 0; i < 3; i++)
            {
                string layer = $"_layer{i + 1}";
                Rectangle row = new(panel.X, panel.Y + i * rowHeight, panel.Width, rowHeight);
                bool selected = _animation.selectedLayer == layer;
                UIRenderer.Fill(row, selected ? UITheme.Primary : UITheme.SurfaceRaised);
                new UITextContainer { Bounds = row, MaxLines = 1 }.Draw(layer, selected ? Color.White : UITheme.Text, .75f);
            }
            UIRenderer.Border(panel, UITheme.Primary, 3);
        }

        private void DrawColorPicker()
        {
            _colorPopover.Draw();
            GlobalParameters.GlobalSpriteBatch.Draw(_colorWheelTexture, _colorPopover.Bounds, Color.White);
        }

        private static Texture2D GenerateColorPickerTexture(int width, int height)
        {
            const int wheelDiameter = 200;
            const int stripX = 208;
            const int stripWidth = 28;
            Texture2D texture = new(GlobalParameters.GlobalGraphics, width, height);
            Color[] colors = new Color[width * height];
            float radius = (wheelDiameter - 1) / 2f;
            Vector2 center = new(radius, radius);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = x + y * width;
                if (x < wheelDiameter)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    if (offset.Length() <= radius)
                    {
                        float hue = MathHelper.ToDegrees((float)Math.Atan2(offset.Y, offset.X) + MathHelper.TwoPi) % 360;
                        colors[index] = HSVToRGB(hue, Math.Clamp(offset.Length() / radius, 0f, 1f), 1f);
                    }
                }
                if (x >= stripX && x < stripX + stripWidth)
                {
                    float value = 1f - y / (float)Math.Max(1, height - 1);
                    colors[index] = new Color(value, value, value);
                }
            }
            texture.SetData(colors);
            return texture;
        }

        private static void RGBToHSV(Color color, out float hue, out float saturation, out float value)
        {
            float r = color.R / 255f, g = color.G / 255f, b = color.B / 255f;
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;
            hue = 0f;
            if (delta > 0f)
            {
                if (max == r) hue = 60f * (((g - b) / delta) % 6f);
                else if (max == g) hue = 60f * (((b - r) / delta) + 2f);
                else hue = 60f * (((r - g) / delta) + 4f);
                if (hue < 0f) hue += 360f;
            }
            saturation = max <= 0f ? 0f : delta / max;
            value = max;
        }

        private static Color HSVToRGB(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;
            float r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return new Color(r + m, g + m, b + m);
        }

        public void Dispose()
        {
            _colorWheelTexture.Dispose();
            _swatchTexture.Dispose();
        }
    }
}
