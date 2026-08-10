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
        private static class Layout
        {
            public const int OuterPadding = 12;
            public const int IconSize = 32;
            public const int ItemGap = 8;
            public const int IconStep = IconSize + ItemGap;
            public const int PrimaryControlGap = 10;
            public const int FrameCounterGap = 14;
            public const int TrailingControlGap = 12;
            public const int ToolGroupGap = 24;
            public const int HomeWidth = 112;
            public const int ControlHeight = 40;
            public const int FrameCounterWidth = 140;
            public const int BrushSliderWidth = 140;
            public const int PopoverTop = UITheme.AppBarHeight + ItemGap;
            public const int PopoverEdge = UITheme.SpaceSm;
            public const int HelpWidth = 520;
            public const int HelpContentHeight = 410;
            public const int SettingsWidth = 450;
            public const int SettingsContentHeight = 430;
            public const int ColorWidth = 236;
            public const int ColorHeight = 200;
            public const int LayersWidth = 260;
            public const int LayersHeight = 300;
            public const int LayerToolbarHeight = 44;
            public const int LayerRowHeight = 42;
            public const int MaxPlaybackFps = 60;
            public const int SwatchSize = IconSize;
            public const int ColorWheelDiameter = 200;
            public const int ValueStripX = 208;
            public const int ValueStripWidth = 28;
            public const int ProjectLabelMinScreenWidth = 1200;
            public const int HomeCompactWidth = 76;
            public const int FrameCounterCompactWidth = 100;
            public const int BrushSliderCompactWidth = 96;
            public const int CompactGap = 6;
        }

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
        private readonly UIActionButton _previousOnionDown;
        private readonly UIActionButton _previousOnionUp;
        private readonly UIActionButton _nextOnionDown;
        private readonly UIActionButton _nextOnionUp;
        private readonly UISlider _onionOpacity;
        private readonly UIActionButton _fpsDown;
        private readonly UIActionButton _fpsUp;
        private readonly UIActionButton _save;
        private readonly UIPopover _helpPopover = new();
        private readonly UIPopover _settingsPopover = new();
        private readonly UIPopover _colorPopover = new() { ShowOutline = false };
        private readonly UIPopover _layersPopover = new();
        private readonly Texture2D _colorWheelTexture;
        private readonly Texture2D _swatchTexture;
        private readonly Color[] _swatchPixels = new Color[Layout.SwatchSize * Layout.SwatchSize];
        private Rectangle _frameCounterBounds;
        private PopoverKind _openPopover;
        private Color _selectedColor = Color.Black;
        private float _selectedHue;
        private float _selectedSaturation;
        private float _selectedValue;
        private string _saveError = string.Empty;
        private int _settingsScroll;

        public Color SelectedColor => _selectedColor;
        public bool HasOpenPopover => _openPopover != PopoverKind.None;
        public static int PreferredHeight(int width) => width < 560 ? 160 : width < 960 ? 112 : UITheme.AppBarHeight;

        public DrawingNavbarComponent(Animation animation)
        {
            _animation = animation;
            _home = new UIActionButton("HOME", GoHome);
            _help = Icon("Static\\DrawingScene/help", () => Toggle(PopoverKind.Help), "Help");
            _settings = Icon("Static\\DrawingScene/gear", () => Toggle(PopoverKind.Settings), "Animation settings");
            _layers = Icon("Static\\DrawingScene/layers", () => Toggle(PopoverKind.Layers), "Layers");

            _swatchTexture = new Texture2D(GlobalParameters.GlobalGraphics, Layout.SwatchSize, Layout.SwatchSize);
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
            _previousOnionDown = new UIActionButton("-", () => _animation.PreviousOnionFrames--);
            _previousOnionUp = new UIActionButton("+", () => _animation.PreviousOnionFrames++);
            _nextOnionDown = new UIActionButton("-", () => _animation.NextOnionFrames--);
            _nextOnionUp = new UIActionButton("+", () => _animation.NextOnionFrames++);
            _onionOpacity = new UISlider(0, 100, (int)Math.Round(_animation.OnionSkinOpacity * 100),
                value => _animation.OnionSkinOpacity = value / 100f);
            _fpsDown = new UIActionButton("-", () => _animation.fps = Math.Max(1, _animation.fps - 1));
            _fpsUp = new UIActionButton("+", () => _animation.fps = Math.Min(Layout.MaxPlaybackFps, _animation.fps + 1));
            _save = new UIActionButton("SAVE PROJECT", SaveAnimation);
            _colorWheelTexture = GenerateColorPickerTexture(Layout.ColorWidth, Layout.ColorHeight);
        }

        private UIIconButton Icon(string asset, Action action, string tooltip) =>
            new(GlobalParameters.GlobalContent.Load<Texture2D>(asset), action) { Tooltip = tooltip };

        public void SaveAnimation()
        {
            try
            {
                SaveService.SaveAnimation(_animation);
                _saveError = string.Empty;
            }
            catch (Exception ex)
            {
                _saveError = ex.Message;
                _openPopover = PopoverKind.Settings;
            }
        }

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
            bool narrow = bounds.Width < 960;
            bool compact = bounds.Width < Layout.ProjectLabelMinScreenWidth;
            int homeWidth = compact ? Layout.HomeCompactWidth : Layout.HomeWidth;
            int primaryGap = compact ? Layout.CompactGap : Layout.PrimaryControlGap;
            int iconStep = Layout.IconSize + (compact ? Layout.CompactGap : Layout.ItemGap);
            int frameCounterWidth = compact ? Layout.FrameCounterCompactWidth : Layout.FrameCounterWidth;
            int brushSliderWidth = compact ? Layout.BrushSliderCompactWidth : Layout.BrushSliderWidth;
            int trailingGap = compact ? Layout.CompactGap : Layout.TrailingControlGap;
            int toolGroupGap = compact ? Layout.ItemGap : Layout.ToolGroupGap;
            int iconY = bounds.Y + (bounds.Height - Layout.IconSize) / 2;
            int controlY = bounds.Y + (bounds.Height - Layout.ControlHeight) / 2;
            int x = bounds.X + Layout.OuterPadding;
            if (narrow)
            {
                int gap = bounds.Width < 560 ? 5 : 8;
                int rowHeight = bounds.Height / (bounds.Width < 560 ? 3 : 2);
                int row = 0;
                x = bounds.X + gap;
                int y = bounds.Y + (rowHeight - Layout.IconSize) / 2;
                void NextRow() { row++; x = bounds.X + gap; y = bounds.Y + row * rowHeight + (rowHeight - Layout.IconSize) / 2; }
                void Place(UIElement element, int width = Layout.IconSize)
                {
                    if (x + width > bounds.Right - gap) NextRow();
                    element.Arrange(new Rectangle(x, y, width, Layout.IconSize));
                    x += width + gap;
                }
                Place(_home, Math.Min(Layout.HomeCompactWidth, Math.Max(52, bounds.Width / 5)));
                Place(_help); Place(_settings);
                _frameCounterBounds = new Rectangle(x, y, Math.Min(Layout.FrameCounterCompactWidth, Math.Max(72, bounds.Right - gap - x)), Layout.IconSize);
                x = _frameCounterBounds.Right + gap;
                foreach (UIIconButton button in _playback) Place(button);
                Place(_brushSize, Math.Min(Layout.BrushSliderCompactWidth, Math.Max(72, bounds.Width / 4)));
                foreach (UIIconButton tool in _tools) Place(tool);
                Place(_layers); Place(_color);
                goto Popovers;
            }
            _home.Arrange(new Rectangle(x, controlY, homeWidth, Layout.ControlHeight));
            x = _home.Bounds.Right + primaryGap;
            _help.Arrange(new Rectangle(x, iconY, Layout.IconSize, Layout.IconSize));
            x = _help.Bounds.Right + primaryGap;
            _settings.Arrange(new Rectangle(x, iconY, Layout.IconSize, Layout.IconSize));

            x = _settings.Bounds.Right + (compact ? Layout.ItemGap : Layout.FrameCounterGap);
            _frameCounterBounds = new Rectangle(x, controlY, frameCounterWidth, Layout.ControlHeight);
            x = _frameCounterBounds.Right + Layout.OuterPadding;
            foreach (UIIconButton button in _playback)
            {
                button.Arrange(new Rectangle(x, iconY, Layout.IconSize, Layout.IconSize));
                x += iconStep;
            }
            _brushSize.Arrange(new Rectangle(x + Layout.ItemGap, controlY, brushSliderWidth, Layout.ControlHeight));

            _color.Arrange(new Rectangle(bounds.Right - Layout.OuterPadding - Layout.IconSize, iconY, Layout.IconSize, Layout.IconSize));
            _layers.Arrange(new Rectangle(_color.Bounds.X - Layout.IconSize - trailingGap, iconY, Layout.IconSize, Layout.IconSize));
            int toolGroupWidth = _tools.Count * Layout.IconSize + Math.Max(0, _tools.Count - 1) * Layout.ItemGap;
            int toolX = _layers.Bounds.X - toolGroupGap - toolGroupWidth;
            foreach (UIIconButton tool in _tools)
            {
                tool.Arrange(new Rectangle(toolX, iconY, Layout.IconSize, Layout.IconSize));
                toolX += Layout.IconStep;
            }

        Popovers:
            int popoverTop = bounds.Bottom + Layout.ItemGap;
            int availableHeight = Math.Max(80, GlobalParameters.screenHeight - popoverTop - Layout.PopoverEdge);
            int helpWidth = Math.Min(Layout.HelpWidth, GlobalParameters.screenWidth - Layout.PopoverEdge * 2);
            int settingsWidth = Math.Min(Layout.SettingsWidth, GlobalParameters.screenWidth - Layout.PopoverEdge * 2);
            _helpPopover.Arrange(ClampPopover(new Rectangle(_help.Bounds.X, popoverTop, helpWidth,
                Math.Min(Layout.HelpContentHeight, availableHeight)), popoverTop));
            _settingsPopover.Arrange(ClampPopover(new Rectangle(_settings.Bounds.X, popoverTop, settingsWidth,
                Math.Min(Layout.SettingsContentHeight, availableHeight)), popoverTop));
            _colorPopover.Arrange(ClampPopover(new Rectangle(_color.Bounds.Right - Layout.ColorWidth, popoverTop, Layout.ColorWidth, Layout.ColorHeight), popoverTop));
            _layersPopover.Arrange(ClampPopover(new Rectangle(_layers.Bounds.Right - Layout.LayersWidth, popoverTop, Layout.LayersWidth,
                Math.Min(Layout.LayersHeight, availableHeight)), popoverTop));

            Rectangle settings = _settingsPopover.Bounds;
            _settingsScroll = Math.Clamp(_settingsScroll, 0, Math.Max(0, Layout.SettingsContentHeight - settings.Height));
            int scroll = _settingsScroll;
            _onionSkin.Arrange(new Rectangle(settings.X + 28, settings.Y + 92 - scroll, 48, 28));
            int controlX = Math.Max(settings.X + 150, settings.Right - 245);
            int controlRight = settings.Right - 28;
            _onionOpacity.Arrange(new Rectangle(controlX, settings.Y + 130 - scroll, Math.Max(70, controlRight - controlX), 44));
            _previousOnionDown.Arrange(new Rectangle(controlX, settings.Y + 190 - scroll, 42, 38));
            _previousOnionUp.Arrange(new Rectangle(controlRight - 42, settings.Y + 190 - scroll, 42, 38));
            _nextOnionDown.Arrange(new Rectangle(controlX, settings.Y + 236 - scroll, 42, 38));
            _nextOnionUp.Arrange(new Rectangle(controlRight - 42, settings.Y + 236 - scroll, 42, 38));
            _fpsDown.Arrange(new Rectangle(controlX, settings.Y + 290 - scroll, 48, 42));
            _fpsUp.Arrange(new Rectangle(controlRight - 48, settings.Y + 290 - scroll, 48, 42));
            int saveWidth = Math.Min(210, settings.Width - 40);
            _save.Arrange(new Rectangle(settings.Center.X - saveWidth / 2, settings.Y + 370 - scroll, saveWidth, 50));
        }

        private static Rectangle ClampPopover(Rectangle bounds, int minimumTop = Layout.PopoverTop)
        {
            bounds.Width = Math.Min(bounds.Width, Math.Max(1, GlobalParameters.screenWidth - Layout.PopoverEdge * 2));
            bounds.Height = Math.Min(bounds.Height, Math.Max(1, GlobalParameters.screenHeight - minimumTop - Layout.PopoverEdge));
            bounds.X = Math.Clamp(bounds.X, Layout.PopoverEdge, Math.Max(Layout.PopoverEdge, GlobalParameters.screenWidth - bounds.Width - Layout.PopoverEdge));
            bounds.Y = Math.Clamp(bounds.Y, minimumTop,
                Math.Max(minimumTop, GlobalParameters.screenHeight - bounds.Height - Layout.PopoverEdge));
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
            Rectangle panel = _settingsPopover.Bounds;
            if (panel.Contains(GlobalParameters.GlobalMouse.newMousePos) && GlobalParameters.GlobalMouse.ScrollDelta != 0)
            {
                int maxScroll = Math.Max(0, Layout.SettingsContentHeight - panel.Height);
                _settingsScroll = Math.Clamp(_settingsScroll - Math.Sign(GlobalParameters.GlobalMouse.ScrollDelta) * 36, 0, maxScroll);
                Arrange(Bounds);
            }
            _onionSkin.Value = _animation.isOnionSkinEnabled;
            Rectangle viewport = SettingsViewport(panel);
            if (FullyVisible(_onionSkin.Bounds, viewport)) _onionSkin.Update();
            if (FullyVisible(_previousOnionDown.Bounds, viewport)) _previousOnionDown.Update();
            if (FullyVisible(_previousOnionUp.Bounds, viewport)) _previousOnionUp.Update();
            if (FullyVisible(_nextOnionDown.Bounds, viewport)) _nextOnionDown.Update();
            if (FullyVisible(_nextOnionUp.Bounds, viewport)) _nextOnionUp.Update();
            _onionOpacity.SetValue((int)Math.Round(_animation.OnionSkinOpacity * 100));
            _onionOpacity.IsEnabled = _animation.isOnionSkinEnabled;
            if (FullyVisible(_onionOpacity.Bounds, viewport)) _onionOpacity.Update();
            if (FullyVisible(_fpsDown.Bounds, viewport)) _fpsDown.Update();
            if (FullyVisible(_fpsUp.Bounds, viewport)) _fpsUp.Update();
            if (FullyVisible(_save.Bounds, viewport)) _save.Update();
        }

        private static Rectangle SettingsViewport(Rectangle panel) => new(
            panel.X + 4, panel.Y + 64, Math.Max(1, panel.Width - 8), Math.Max(1, panel.Height - 68));

        private static bool FullyVisible(Rectangle bounds, Rectangle viewport) =>
            bounds.X >= viewport.X && bounds.Right <= viewport.Right &&
            bounds.Y >= viewport.Y && bounds.Bottom <= viewport.Bottom;

        private void UpdateLayers()
        {
            Rectangle panel = _layersPopover.Bounds;
            Rectangle toolbar = new(panel.X + 8, panel.Bottom - Layout.LayerToolbarHeight, panel.Width - 16, 36);
            IReadOnlyList<Rectangle> actions = UILayoutEngine.Stack(toolbar, UIAxis.Horizontal, 4, 6);
            if (UIPointerRouter.Clicked(actions[0]))
                _animation.AddLayer($"Layer {_animation.Layers.Count + 1}");
            else if (UIPointerRouter.Clicked(actions[1]))
                _animation.RemoveLayer(_animation.SelectedLayerId);
            else if (UIPointerRouter.Clicked(actions[2]))
            {
                int selected = FindSelectedLayerIndex();
                if (selected > 0) _animation.MoveLayer(_animation.SelectedLayerId, selected - 1);
            }
            else if (UIPointerRouter.Clicked(actions[3]))
            {
                int selected = FindSelectedLayerIndex();
                if (selected >= 0 && selected < _animation.Layers.Count - 1)
                    _animation.MoveLayer(_animation.SelectedLayerId, selected + 1);
            }

            int visibleRows = Math.Max(1, (panel.Height - Layout.LayerToolbarHeight) / Layout.LayerRowHeight);
            int start = Math.Max(0, FindSelectedLayerIndex() - visibleRows + 1);
            for (int i = start; i < Math.Min(_animation.Layers.Count, start + visibleRows); i++)
            {
                Rectangle row = new(panel.X + 4, panel.Y + (i - start) * Layout.LayerRowHeight,
                    panel.Width - 8, Layout.LayerRowHeight);
                Rectangle visibility = new(row.X, row.Y, 44, row.Height);
                if (UIPointerRouter.Clicked(visibility))
                    _animation.SetLayerVisibility(_animation.Layers[i].Id, !_animation.Layers[i].IsVisible);
                else if (UIPointerRouter.Clicked(row))
                    _animation.SelectLayer(_animation.Layers[i].Id);
            }
        }

        private int FindSelectedLayerIndex()
        {
            for (int i = 0; i < _animation.Layers.Count; i++)
                if (_animation.Layers[i].Id == _animation.SelectedLayerId) return i;
            return -1;
        }

        private void UpdateColorPicker()
        {
            Rectangle bounds = _colorPopover.Bounds;
            if (!UIPointerRouter.Held(_colorPopover, bounds)) return;
            Vector2 local = GlobalParameters.GlobalMouse.newMousePos - new Vector2(bounds.X, bounds.Y);
            const int wheelDiameter = Layout.ColorWheelDiameter;
            const int stripX = Layout.ValueStripX;
            const int stripWidth = Layout.ValueStripWidth;
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
            int center = Layout.SwatchSize / 2;
            for (int y = 0; y < Layout.SwatchSize; y++)
            for (int x = 0; x < Layout.SwatchSize; x++)
            {
                int dx = x - center;
                int dy = y - center;
                int distance = dx * dx + dy * dy;
                _swatchPixels[x + y * Layout.SwatchSize] = distance > outer ? Color.Transparent : distance >= inner ? Color.Black : _selectedColor;
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

            UIRenderer.Fill(_frameCounterBounds, UITheme.Primary);
            new UITextContainer { Bounds = _frameCounterBounds, MaxLines = 1 }.Draw($"{_animation.CurrentFrameIndex + 1} / {_animation.TotalFrames}", Color.White, .78f);

            if (GlobalParameters.screenWidth >= Layout.ProjectLabelMinScreenWidth)
            {
                int labelX = _brushSize.Bounds.Right + 8;
                int labelWidth = Math.Max(1, _tools[0].Bounds.X - labelX - 8);
                new UITextContainer { Bounds = new Rectangle(labelX, 4, labelWidth, 28), MaxLines = 1, HorizontalAlignment = UIAlign.Start }
                    .Draw(_animation.projectName, UITheme.TextMuted, .72f);
                DrawSizeBar(new Rectangle(labelX, 34, labelWidth, 20));
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
            const string shortcuts = "Ctrl+Z / Ctrl+Y - Undo or redo\nP - Play or pause\nN / M - Previous or next frame\nB - Insert a frame\nDelete - Remove current frame\nO - Toggle onion skin\n[ / ] - Change brush size\nBackspace - Clear selected layer\nL - Save project\nEsc - Return home";
            new UITextContainer { Bounds = new Rectangle(panel.X + 22, panel.Y + 72, panel.Width - 44, panel.Height - 92), HorizontalAlignment = UIAlign.Start, VerticalAlignment = UIAlign.Start, Padding = 4, MaxLines = 12 }
                .Draw(shortcuts, UITheme.Text, .85f);
        }

        private void DrawSettings()
        {
            _settingsPopover.Draw();
            Rectangle panel = _settingsPopover.Bounds;
            Rectangle viewport = SettingsViewport(panel);
            int scroll = _settingsScroll;
            new UITextContainer { Bounds = new Rectangle(panel.X + 24, panel.Y + 16, panel.Width - 48, 48), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Animation settings", UITheme.Primary, .72f);
            void TextIfVisible(Rectangle bounds, string text, Color color, float scale, UIAlign alignment = UIAlign.Center)
            {
                if (FullyVisible(bounds, viewport))
                    new UITextContainer { Bounds = bounds, HorizontalAlignment = alignment, MaxLines = 1 }.Draw(text, color, scale);
            }
            if (FullyVisible(_onionSkin.Bounds, viewport)) _onionSkin.Draw();
            TextIfVisible(new Rectangle(panel.X + 88, panel.Y + 89 - scroll, panel.Width - 112, 42),
                $"Onion skin: {(_animation.isOnionSkinEnabled ? "On" : "Off")}", UITheme.Text, .6f, UIAlign.Start);
            TextIfVisible(new Rectangle(panel.X + 28, panel.Y + 130 - scroll, 120, 44), "Onion opacity", UITheme.Text, .6f, UIAlign.Start);
            if (FullyVisible(_onionOpacity.Bounds, viewport)) _onionOpacity.Draw();
            TextIfVisible(new Rectangle(_onionOpacity.Bounds.X, panel.Y + 164 - scroll, _onionOpacity.Bounds.Width, 24),
                $"{(int)Math.Round(_animation.OnionSkinOpacity * 100)}%", UITheme.TextMuted, .55f);
            TextIfVisible(new Rectangle(panel.X + 28, panel.Y + 190 - scroll, 120, 38), "Previous frames", UITheme.Text, .6f, UIAlign.Start);
            if (FullyVisible(_previousOnionDown.Bounds, viewport)) _previousOnionDown.Draw();
            if (FullyVisible(_previousOnionUp.Bounds, viewport)) _previousOnionUp.Draw();
            TextIfVisible(new Rectangle(_previousOnionDown.Bounds.Right, panel.Y + 190 - scroll,
                Math.Max(1, _previousOnionUp.Bounds.X - _previousOnionDown.Bounds.Right), 38), _animation.PreviousOnionFrames.ToString(), UITheme.Primary, .65f);
            TextIfVisible(new Rectangle(panel.X + 28, panel.Y + 236 - scroll, 120, 38), "Next frames", UITheme.Text, .6f, UIAlign.Start);
            if (FullyVisible(_nextOnionDown.Bounds, viewport)) _nextOnionDown.Draw();
            if (FullyVisible(_nextOnionUp.Bounds, viewport)) _nextOnionUp.Draw();
            TextIfVisible(new Rectangle(_nextOnionDown.Bounds.Right, panel.Y + 236 - scroll,
                Math.Max(1, _nextOnionUp.Bounds.X - _nextOnionDown.Bounds.Right), 38), _animation.NextOnionFrames.ToString(), UITheme.Primary, .65f);
            TextIfVisible(new Rectangle(panel.X + 28, panel.Y + 290 - scroll, 120, 42), "Playback FPS", UITheme.Text, .6f, UIAlign.Start);
            if (FullyVisible(_fpsDown.Bounds, viewport)) _fpsDown.Draw();
            if (FullyVisible(_fpsUp.Bounds, viewport)) _fpsUp.Draw();
            TextIfVisible(new Rectangle(_fpsDown.Bounds.Right, panel.Y + 290 - scroll,
                Math.Max(1, _fpsUp.Bounds.X - _fpsDown.Bounds.Right), 42), _animation.fps.ToString(), UITheme.Primary, .65f);
            Rectangle sizeBounds = new(panel.X + 28, panel.Y + 342 - scroll, panel.Width - 56, 28);
            if (FullyVisible(sizeBounds, viewport)) DrawSizeBar(sizeBounds);
            if (!string.IsNullOrEmpty(_saveError))
            {
                Rectangle errorBounds = new(panel.X + 24, panel.Y + 374 - scroll, panel.Width - 48, 32);
                if (FullyVisible(errorBounds, viewport))
                    new UITextContainer { Bounds = errorBounds, MaxLines = 2 }.Draw(_saveError, Color.IndianRed, .52f);
            }
            if (FullyVisible(_save.Bounds, viewport)) _save.Draw(true);
        }

        private void DrawSizeBar(Rectangle bounds)
        {
            int percent = (int)Math.Ceiling(_animation.ResourceBudgetRemaining * 100);
            const int labelWidth = 58;
            const int maxTrackWidth = 140;
            new UITextContainer { Bounds = new Rectangle(bounds.X, bounds.Y, labelWidth, bounds.Height), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Size", UITheme.TextMuted, .55f);
            Rectangle track = new(bounds.X + labelWidth, bounds.Y + (bounds.Height - 8) / 2,
                Math.Max(1, Math.Min(maxTrackWidth, bounds.Width - labelWidth - 38)), 8);
            UIRenderer.Fill(track, UITheme.Surface);
            UIRenderer.Border(track, UITheme.Border);
            Rectangle remaining = new(track.X + 1, track.Y + 1,
                (int)Math.Round(Math.Max(0, track.Width - 2) * _animation.ResourceBudgetRemaining), Math.Max(1, track.Height - 2));
            UIRenderer.Fill(remaining, percent <= 10 ? Color.IndianRed : percent <= 25 ? Color.Orange : UITheme.Primary);
            new UITextContainer { Bounds = new Rectangle(track.Right + 6, bounds.Y, 52, bounds.Height), Padding = 0, MaxLines = 1 }
                .Draw($"{percent}%", UITheme.TextMuted, .5f);
        }

        private void DrawLayers()
        {
            _layersPopover.Draw();
            Rectangle panel = _layersPopover.Bounds;
            int visibleRows = Math.Max(1, (panel.Height - Layout.LayerToolbarHeight) / Layout.LayerRowHeight);
            int start = Math.Max(0, FindSelectedLayerIndex() - visibleRows + 1);
            for (int i = start; i < Math.Min(_animation.Layers.Count, start + visibleRows); i++)
            {
                AnimationLayer layer = _animation.Layers[i];
                Rectangle row = new(panel.X + 4, panel.Y + (i - start) * Layout.LayerRowHeight,
                    panel.Width - 8, Layout.LayerRowHeight);
                bool selected = _animation.SelectedLayerId == layer.Id;
                UIRenderer.Fill(row, selected ? UITheme.Primary : UITheme.SurfaceRaised);
                Rectangle visibility = new(row.X, row.Y, 44, row.Height);
                Rectangle visibilityBox = new(visibility.Center.X - 9, visibility.Center.Y - 9, 18, 18);
                UIRenderer.Fill(visibilityBox, layer.IsVisible ? Color.White : Color.Black);
                UIRenderer.Border(visibilityBox, layer.IsVisible ? Color.Black : Color.White, 2);
                Rectangle nameBounds = new(row.X + 44, row.Y, row.Width - 44, row.Height);
                string status = $"{(layer.IsLocked ? "Locked - " : "")}{layer.Name}";
                new UITextContainer { Bounds = nameBounds, HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                    .Draw(status, selected ? Color.White : UITheme.Text, .75f);
            }
            Rectangle toolbar = new(panel.X + 8, panel.Bottom - Layout.LayerToolbarHeight, panel.Width - 16, 36);
            IReadOnlyList<Rectangle> actions = UILayoutEngine.Stack(toolbar, UIAxis.Horizontal, 4, 6);
            string[] labels = { "+", "-", "^", "v" };
            for (int i = 0; i < actions.Count; i++)
            {
                UIRenderer.Fill(actions[i], UITheme.SurfaceRaised);
                UIRenderer.Border(actions[i], UITheme.Border, 2);
                UIRenderer.CenteredText(labels[i], actions[i], i == 1 ? UITheme.Danger : UITheme.Text, .8f);
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
            const int wheelDiameter = Layout.ColorWheelDiameter;
            const int stripX = Layout.ValueStripX;
            const int stripWidth = Layout.ValueStripWidth;
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
