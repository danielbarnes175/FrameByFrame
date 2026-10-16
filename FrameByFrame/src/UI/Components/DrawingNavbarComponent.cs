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
            public const int HelpContentHeight = 470;
            public const int SettingsWidth = 450;
            public const int SettingsContentHeight = 620;
            public const int ColorWidth = 236;
            public const int ColorHeight = 200;
            public const int LayersWidth = 260;
            public const int LayersHeight = 300;
            public const int BrushesWidth = 560;
            public const int BrushesHeight = 490;
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

        private enum PopoverKind { None, Help, Settings, Color, Layers, Brushes }

        private readonly Animation _animation;
        private readonly DrawingTools _drawingTools;
        private readonly AutosaveService _autosave;
        private readonly UIFeedbackState? _feedback;
        private readonly UIActionButton _home;
        private readonly UIIconButton _help;
        private readonly UIIconButton _settings;
        private readonly UIIconButton _color;
        private readonly UIIconButton _layers;
        private readonly List<UIIconButton> _tools = new();
        private readonly List<DrawingTool> _toolKinds = new();
        private readonly List<UIIconButton> _playback = new();
        private readonly UISlider _brushSize;
        private readonly UIIconButton _brushEditor;
        private readonly UIActionButton _previousBrush;
        private readonly UIActionButton _nextBrush;
        private readonly UIActionButton _newBrush;
        private readonly UIActionButton _deleteBrush;
        private readonly UIActionButton _clearBrushTip;
        private readonly UIActionButton _fillBrushTip;
        private readonly UISlider _brushTipAlpha;
        private readonly UIToggle _onionSkin;
        private readonly UIToggle _autosaveToggle;
        private readonly UIToggle _transparentBackgroundToggle;
        private readonly UIActionButton _useBrushColorForBackground;
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
        private readonly UIPopover _brushesPopover = new();
        private readonly Texture2D _colorWheelTexture;
        private readonly Texture2D _swatchTexture;
        private readonly Texture2D _brushPresetIcon;
        private readonly Texture2D _selectionIcon;
        private readonly Color[] _swatchPixels = new Color[Layout.SwatchSize * Layout.SwatchSize];
        private Rectangle _frameCounterBounds;
        private PopoverKind _openPopover;
        private Color _selectedColor = Color.Black;
        private float _selectedHue;
        private float _selectedSaturation;
        private float _selectedValue;
        private string _saveError = string.Empty;
        private bool _savePending;
        private int _helpScroll;
        private int _settingsScroll;
        private int _brushTipAlphaPercent = 100;
        private Rectangle _brushTipBounds;

        private static readonly string[] HelpShortcuts =
        {
            "Ctrl+Z / Ctrl+Y - Undo or redo",
            "Ctrl+D - Duplicate selected frame",
            "Ctrl+C / Ctrl+V - Copy or paste frame/selection",
            "Drag timeline frames - Reorder frames",
            "P - Play or pause",
            "N / M - Previous or next frame",
            "B - Insert a frame",
            "Delete - Remove current frame",
            "O - Toggle onion skin",
            "[ / ] - Change brush size",
            "Middle mouse drag - Pan canvas view",
            "Right mouse drag - Rotate canvas view",
            "Mouse wheel - Zoom canvas view",
            "Backspace - Clear selected layer",
            "L - Save project",
            "Esc - Return home"
        };

        public Color SelectedColor => _selectedColor;
        public bool HasOpenPopover => _openPopover != PopoverKind.None;
        public bool HasPendingSave => _savePending;
        public static int PreferredHeight(int width) => width < 560 ? 160 : width < 960 ? 112 : UITheme.AppBarHeight;

        public DrawingNavbarComponent(Animation animation, DrawingTools drawingTools, AutosaveService autosave,
            UIFeedbackState? feedback = null)
        {
            _animation = animation;
            _drawingTools = drawingTools;
            _autosave = autosave ?? throw new ArgumentNullException(nameof(autosave));
            _feedback = feedback;
            _home = new UIActionButton("HOME", GoHome);
            _help = Icon("help", () => Toggle(PopoverKind.Help), "Help");
            _settings = Icon("gear", () => Toggle(PopoverKind.Settings), "Animation settings");
            _layers = Icon("layers", () => Toggle(PopoverKind.Layers), "Layers");

            _swatchTexture = new Texture2D(GlobalParameters.GlobalGraphics, Layout.SwatchSize, Layout.SwatchSize);
            UpdateColorSwatch();
            _color = new UIIconButton(_swatchTexture, () => Toggle(PopoverKind.Color)) { Tooltip = "Brush color" };

            AddTool(DrawingTool.Draw, "brush", "Paintbrush");
            AddTool(DrawingTool.Eraser, "eraser", "Eraser");
            AddTool(DrawingTool.Fill, "bucket_tool", "Bucket fill");
            AddTool(DrawingTool.ColorPicker, "eyedropper_tool", "Color picker");
            _selectionIcon = CreateSelectionIcon();
            AddTool(DrawingTool.Selection, _selectionIcon, "Selection tool");

            _playback.Add(Icon("first_frame", _animation.FirstFrame, "First frame"));
            _playback.Add(Icon("previous_frame", _animation.PreviousFrame, "Previous frame - N"));
            _playback.Add(Icon("play", _animation.TogglePlaying, "Play / pause - P"));
            _playback.Add(Icon("next_frame", _animation.NextFrame, "Next frame - M"));
            _playback.Add(Icon("last_frame", _animation.LastFrame, "Last frame"));

            _brushSize = new UISlider(UIConstants.MIN_BRUSH_SIZE, UIConstants.MAX_BRUSH_SIZE,
                _drawingTools.BrushSize, value => _drawingTools.BrushSize = value);
            _brushPresetIcon = new Texture2D(GlobalParameters.GlobalGraphics, Layout.IconSize, Layout.IconSize);
            UpdateBrushPresetIcon();
            _brushEditor = new UIIconButton(_brushPresetIcon, () => Toggle(PopoverKind.Brushes))
                { Tooltip = "Brush presets", UseToolSelectionStyle = true };
            _previousBrush = new UIActionButton("<", () => SelectBrush(-1));
            _nextBrush = new UIActionButton(">", () => SelectBrush(1));
            _newBrush = new UIActionButton("New", _drawingTools.CreateBrush);
            _deleteBrush = new UIActionButton("Delete", _drawingTools.DeleteSelectedBrush);
            _clearBrushTip = new UIActionButton("Clear tip", () => _drawingTools.FillBrushTip(0));
            _fillBrushTip = new UIActionButton("Fill tip", () => _drawingTools.FillBrushTip(byte.MaxValue));
            _brushTipAlpha = new UISlider(0, 100, _brushTipAlphaPercent,
                value => _brushTipAlphaPercent = value);
            _onionSkin = new UIToggle(_animation.isOnionSkinEnabled,
                value => _animation.isOnionSkinEnabled = value);
            _autosaveToggle = new UIToggle(_autosave.IsEnabled, SetAutosave);
            _transparentBackgroundToggle = new UIToggle(_animation.IsCanvasBackgroundTransparent,
                _animation.SetCanvasBackgroundTransparent);
            _useBrushColorForBackground = new UIActionButton("Use brush color",
                () => _animation.SetCanvasBackgroundColor(_selectedColor));
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
            if (_savePending) return;
            _savePending = true;
            _feedback?.ShowBusy("Saving project", $"Saving {_animation.projectName}...");
            if (_feedback == null) ProcessPendingSave();
        }

        public void ProcessPendingSave()
        {
            if (!_savePending) return;
            _savePending = false;
            int previousThumbnailFrameIndex = _animation.ThumbnailFrameIndex;
            try
            {
                _animation.CaptureThumbnailFrame();
                SaveService.SaveAnimation(_animation);
                _saveError = string.Empty;
                _feedback?.ShowSuccess("Project saved", $"{_animation.projectName} was saved successfully.");
            }
            catch (Exception ex)
            {
                _animation.RestoreThumbnailFrame(previousThumbnailFrameIndex);
                _saveError = ex.Message;
                _openPopover = PopoverKind.Settings;
                _feedback?.ShowError("Project could not be saved", ex.Message,
                    "Check the project name, available disk space, and Projects folder permissions, then try again.");
            }
        }

        private void AddTool(DrawingTool tool, string asset, string tooltip)
        {
            AddTool(tool, GlobalParameters.GlobalContent.Load<Texture2D>(asset), tooltip);
        }

        private void AddTool(DrawingTool tool, Texture2D icon, string tooltip)
        {
            UIIconButton button = new(icon, () => SelectTool(tool)) { Tooltip = tooltip };
            button.UseToolSelectionStyle = true;
            _tools.Add(button);
            _toolKinds.Add(tool);
        }

        private static Texture2D CreateSelectionIcon()
        {
            const int size = 32;
            Texture2D texture = new(GlobalParameters.GlobalGraphics, size, size);
            Color[] pixels = new Color[size * size];
            for (int i = 6; i <= 25; i++)
            {
                bool dash = ((i - 6) / 3) % 2 == 0;
                if (!dash) continue;
                pixels[i + 6 * size] = UITheme.Text;
                pixels[i + 25 * size] = UITheme.Text;
                pixels[6 + i * size] = UITheme.Text;
                pixels[25 + i * size] = UITheme.Text;
            }
            texture.SetData(pixels);
            return texture;
        }

        private void SelectTool(DrawingTool tool) => _drawingTools.SelectedTool = tool;

        private void SelectBrush(int delta)
        {
            int count = _drawingTools.Brushes.Count;
            _drawingTools.SelectBrush((_drawingTools.SelectedBrushIndex + delta + count) % count);
        }

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
                Place(_layers); Place(_brushEditor); Place(_color);
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
            _brushEditor.Arrange(new Rectangle(_color.Bounds.X - Layout.IconSize - trailingGap,
                iconY, Layout.IconSize, Layout.IconSize));
            _layers.Arrange(new Rectangle(_brushEditor.Bounds.X - Layout.IconSize - trailingGap,
                iconY, Layout.IconSize, Layout.IconSize));
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
            _brushesPopover.Arrange(ClampPopover(new Rectangle(_brushEditor.Bounds.X, popoverTop,
                Layout.BrushesWidth, Math.Min(Layout.BrushesHeight, availableHeight)), popoverTop));

            Rectangle brushes = _brushesPopover.Bounds;
            int brushLeft = brushes.X + 20;
            int brushWidth = brushes.Width - 40;
            _previousBrush.Arrange(new Rectangle(brushLeft, brushes.Y + 34, 40, 28));
            _nextBrush.Arrange(new Rectangle(brushes.Right - 60, brushes.Y + 34, 40, 28));
            var presetActions = UILayoutEngine.Stack(new Rectangle(brushLeft, brushes.Y + 182,
                brushWidth, 30), UIAxis.Horizontal, 2, 8);
            _newBrush.Arrange(presetActions[0]);
            _deleteBrush.Arrange(presetActions[1]);
            int editorSize = Math.Min(220, Math.Max(64, brushes.Height - 250));
            Rectangle editorArea = new(brushLeft, brushes.Y + 220, editorSize, editorSize);
            _brushTipBounds = FitBrushTip(editorArea, _drawingTools.SelectedBrush);
            int tipControlX = editorArea.Right + 16;
            int tipControlWidth = Math.Max(80, brushes.Right - 20 - tipControlX);
            _brushTipAlpha.Arrange(new Rectangle(tipControlX, brushes.Y + 248, tipControlWidth, 34));
            _fillBrushTip.Arrange(new Rectangle(tipControlX, brushes.Y + 306, tipControlWidth, 34));
            _clearBrushTip.Arrange(new Rectangle(tipControlX, brushes.Y + 348, tipControlWidth, 34));

            _helpScroll = Math.Clamp(_helpScroll, 0,
                Math.Max(0, Layout.HelpContentHeight - _helpPopover.Bounds.Height));
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
            _autosaveToggle.Arrange(new Rectangle(settings.X + 28, settings.Y + 342 - scroll, 48, 28));
            _transparentBackgroundToggle.Arrange(new Rectangle(
                settings.X + 28, settings.Y + 390 - scroll, 48, 28));
            _useBrushColorForBackground.Arrange(new Rectangle(
                controlX, settings.Y + 432 - scroll, Math.Max(120, controlRight - controlX), 42));
            int saveWidth = Math.Min(210, settings.Width - 40);
            _save.Arrange(new Rectangle(settings.Center.X - saveWidth / 2, settings.Y + 552 - scroll, saveWidth, 50));
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
            _brushEditor.Update();
            foreach (UIIconButton button in _tools) button.Update();
            foreach (UIIconButton button in _playback) button.Update();
            _brushSize.SetValue(_drawingTools.BrushSize);
            _brushSize.Update();

            _helpPopover.IsOpen = _openPopover == PopoverKind.Help;
            _settingsPopover.IsOpen = _openPopover == PopoverKind.Settings;
            _colorPopover.IsOpen = _openPopover == PopoverKind.Color;
            _layersPopover.IsOpen = _openPopover == PopoverKind.Layers;
            _brushesPopover.IsOpen = _openPopover == PopoverKind.Brushes;
            ActivePopover()?.Update();

            if (_helpPopover.IsOpen) UpdateHelp();
            if (_settingsPopover.IsOpen) UpdateSettings();
            if (_layersPopover.IsOpen) UpdateLayers();
            if (_colorPopover.IsOpen) UpdateColorPicker();
            if (_brushesPopover.IsOpen) UpdateBrushes();
            DismissPopoverOnOutsideClick();
        }

        private UIPopover? ActivePopover() => _openPopover switch
        {
            PopoverKind.Help => _helpPopover,
            PopoverKind.Settings => _settingsPopover,
            PopoverKind.Color => _colorPopover,
            PopoverKind.Layers => _layersPopover,
            PopoverKind.Brushes => _brushesPopover,
            _ => null
        };

        private void UpdateBrushes()
        {
            BrushPreset brush = _drawingTools.SelectedBrush;
            Vector2 pointer = GlobalParameters.GlobalMouse.newMousePos;
            if (GlobalParameters.GlobalMouse.LeftClick())
            {
                foreach ((int index, Rectangle bounds) in VisibleBrushPresetBounds())
                    if (bounds.Contains(pointer))
                    {
                        _drawingTools.SelectBrush(index);
                        brush = _drawingTools.SelectedBrush;
                        break;
                    }
            }

            bool canEdit = _drawingTools.CanEditSelectedBrush;
            _deleteBrush.IsEnabled = canEdit && _drawingTools.Brushes.Count > 1;
            _brushTipAlpha.IsEnabled = canEdit;
            _fillBrushTip.IsEnabled = canEdit;
            _clearBrushTip.IsEnabled = canEdit;
            _previousBrush.Update();
            _nextBrush.Update();
            _newBrush.Update();
            _deleteBrush.Update();
            _brushTipAlpha.SetValue(_brushTipAlphaPercent);
            _brushTipAlpha.Update();
            _fillBrushTip.Update();
            _clearBrushTip.Update();

            int editorSize = Math.Min(220, Math.Max(64, _brushesPopover.Bounds.Height - 250));
            Rectangle editorArea = new(_brushesPopover.Bounds.X + 20, _brushesPopover.Bounds.Y + 220,
                editorSize, editorSize);
            _brushTipBounds = FitBrushTip(editorArea, brush);
            UpdateBrushPresetIcon();
            if (!canEdit || !_brushTipBounds.Contains(pointer) ||
                (!GlobalParameters.GlobalMouse.LeftClickHold() && !GlobalParameters.GlobalMouse.RightClickHold())) return;
            int tipX = Math.Clamp((int)((pointer.X - _brushTipBounds.X) * brush.TipWidth / _brushTipBounds.Width),
                0, brush.TipWidth - 1);
            int tipY = Math.Clamp((int)((pointer.Y - _brushTipBounds.Y) * brush.TipHeight / _brushTipBounds.Height),
                0, brush.TipHeight - 1);
            byte alpha = GlobalParameters.GlobalMouse.RightClickHold()
                ? (byte)0
                : (byte)Math.Clamp((int)Math.Round(_brushTipAlphaPercent / 100f * 255f), 0, 255);
            _drawingTools.SetBrushTipAlpha(tipX, tipY, alpha);
            UpdateBrushPresetIcon();
        }

        private void UpdateBrushPresetIcon()
        {
            BrushPreset brush = _drawingTools.SelectedBrush;
            Color[] pixels = new Color[Layout.IconSize * Layout.IconSize];
            const int padding = 4;
            int size = Layout.IconSize - padding * 2;
            for (int y = padding; y < Layout.IconSize - padding; y++)
            for (int x = padding; x < Layout.IconSize - padding; x++)
            {
                int tipX = (x - padding) * brush.TipWidth / size;
                int tipY = (y - padding) * brush.TipHeight / size;
                byte alpha = brush.AlphaAt(tipX, tipY);
                if (alpha > 0) pixels[x + y * Layout.IconSize] = UITheme.Text * (alpha / 255f);
            }
            _brushPresetIcon.SetData(pixels);
        }

        private IEnumerable<(int Index, Rectangle Bounds)> VisibleBrushPresetBounds()
        {
            const int columns = 5;
            const int visibleCount = 10;
            const int gap = 6;
            Rectangle panel = _brushesPopover.Bounds;
            int start = _drawingTools.SelectedBrushIndex / visibleCount * visibleCount;
            int width = (panel.Width - 40 - gap * (columns - 1)) / columns;
            for (int index = start; index < Math.Min(_drawingTools.Brushes.Count, start + visibleCount); index++)
            {
                int slot = index - start;
                yield return (index, new Rectangle(panel.X + 20 + slot % columns * (width + gap),
                    panel.Y + 66 + slot / columns * 56, width, 50));
            }
        }

        private static Rectangle FitBrushTip(Rectangle area, BrushPreset brush)
        {
            int cell = Math.Max(1, Math.Min(area.Width / brush.TipWidth, area.Height / brush.TipHeight));
            int width = cell * brush.TipWidth;
            int height = cell * brush.TipHeight;
            return new Rectangle(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2,
                width, height);
        }

        private void UpdateHelp()
        {
            Rectangle panel = _helpPopover.Bounds;
            if (panel.Contains(GlobalParameters.GlobalMouse.newMousePos) && GlobalParameters.GlobalMouse.ScrollDelta != 0)
            {
                int maxScroll = Math.Max(0, Layout.HelpContentHeight - panel.Height);
                _helpScroll = Math.Clamp(_helpScroll - Math.Sign(GlobalParameters.GlobalMouse.ScrollDelta) * 36, 0, maxScroll);
            }
        }

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
            _autosaveToggle.Value = _autosave.IsEnabled;
            _transparentBackgroundToggle.Value = _animation.IsCanvasBackgroundTransparent;
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
            if (FullyVisible(_autosaveToggle.Bounds, viewport)) _autosaveToggle.Update();
            if (FullyVisible(_transparentBackgroundToggle.Bounds, viewport))
                _transparentBackgroundToggle.Update();
            if (FullyVisible(_useBrushColorForBackground.Bounds, viewport))
                _useBrushColorForBackground.Update();
            if (FullyVisible(_save.Bounds, viewport)) _save.Update();
        }

        private void SetAutosave(bool enabled)
        {
            try
            {
                _autosave.SetEnabled(enabled);
                _saveError = string.Empty;
            }
            catch (Exception ex)
            {
                _saveError = $"Could not save autosave preference: {ex.Message}";
                _autosaveToggle.Value = _autosave.IsEnabled;
            }
        }

        private static Rectangle SettingsViewport(Rectangle panel) => new(
            panel.X + 4, panel.Y + 64, Math.Max(1, panel.Width - 8), Math.Max(1, panel.Height - 68));

        private static Rectangle HelpViewport(Rectangle panel) => new(
            panel.X + 4, panel.Y + 64, Math.Max(1, panel.Width - 8), Math.Max(1, panel.Height - 68));

        private static bool FullyVisible(Rectangle bounds, Rectangle viewport) =>
            bounds.X >= viewport.X && bounds.Right <= viewport.Right &&
            bounds.Y >= viewport.Y && bounds.Bottom <= viewport.Bottom;

        private void UpdateLayers()
        {
            Rectangle panel = _layersPopover.Bounds;
            Rectangle toolbar = new(panel.X + 8, panel.Bottom - Layout.LayerToolbarHeight, panel.Width - 16, 36);
            IReadOnlyList<Rectangle> actions = UILayoutEngine.Stack(toolbar, UIAxis.Horizontal, 4, 6);
            if (_animation.CanAddLayer && UIPointerRouter.Clicked(actions[0]))
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
            UIPopover? active = ActivePopover();
            if (active == null || !GlobalParameters.GlobalMouse.LeftClick()) return;
            if (active.Bounds.Contains(GlobalParameters.GlobalMouse.newMousePos)) return;
            Rectangle anchor = _openPopover switch
            {
                PopoverKind.Help => _help.Bounds,
                PopoverKind.Settings => _settings.Bounds,
                PopoverKind.Color => _color.Bounds,
                PopoverKind.Layers => _layers.Bounds,
                PopoverKind.Brushes => _brushEditor.Bounds,
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
            _help.Draw(drawTooltip: false);
            _settings.Draw(drawTooltip: false);
            _color.Draw(drawTooltip: false);
            _layers.Draw(drawTooltip: false);
            for (int i = 0; i < _tools.Count; i++)
            {
                _tools[i].IsSelected = _drawingTools.SelectedTool == _toolKinds[i];
                _tools[i].Draw(drawTooltip: false);
            }
            foreach (UIIconButton button in _playback) button.Draw(drawTooltip: false);
            _brushSize.Draw();
            _brushEditor.IsSelected = _openPopover == PopoverKind.Brushes;
            _brushEditor.Draw(drawTooltip: false);

            UIRenderer.Fill(_frameCounterBounds, UITheme.Primary);
            new UITextContainer { Bounds = _frameCounterBounds, MaxLines = 1 }.Draw(
                $"{_animation.CurrentFrameIndex + 1} / {_animation.TotalFrames}", Color.White, UITextRole.Label);

            if (GlobalParameters.screenWidth >= Layout.ProjectLabelMinScreenWidth)
            {
                int labelX = _brushSize.Bounds.Right + 8;
                int labelWidth = Math.Max(1, _tools[0].Bounds.X - labelX - 8);
                new UITextContainer { Bounds = new Rectangle(labelX, 4, labelWidth, 28), MaxLines = 1, HorizontalAlignment = UIAlign.Start }
                    .Draw(_animation.projectName, UITheme.TextMuted, UITextRole.Label);
                DrawSizeBar(new Rectangle(labelX, 34, labelWidth, 20));
            }

            DrawActivePopover();

            // All navbar tooltips render last so open popovers cannot cover them.
            _help.DrawTooltip();
            _settings.DrawTooltip();
            _color.DrawTooltip();
            _layers.DrawTooltip();
            foreach (UIIconButton tool in _tools) tool.DrawTooltip();
            foreach (UIIconButton button in _playback) button.DrawTooltip();
            _brushEditor.DrawTooltip();
        }

        private void DrawActivePopover()
        {
            switch (_openPopover)
            {
                case PopoverKind.Help: DrawHelp(); break;
                case PopoverKind.Settings: DrawSettings(); break;
                case PopoverKind.Color: DrawColorPicker(); break;
                case PopoverKind.Layers: DrawLayers(); break;
                case PopoverKind.Brushes: DrawBrushes(); break;
            }
        }

        private void DrawBrushes()
        {
            _brushesPopover.Draw();
            Rectangle panel = _brushesPopover.Bounds;
            BrushPreset brush = _drawingTools.SelectedBrush;
            new UITextContainer { Bounds = new Rectangle(panel.X + 20, panel.Y + 4, panel.Width - 40, 28), MaxLines = 1 }
                .Draw("Custom brush editor", UITheme.Primary, UITextRole.Heading);
            new UITextContainer { Bounds = new Rectangle(_previousBrush.Bounds.Right, panel.Y + 34,
                Math.Max(1, _nextBrush.Bounds.X - _previousBrush.Bounds.Right), 28), MaxLines = 1 }
                .Draw($"{brush.Name} ({_drawingTools.SelectedBrushIndex + 1}/{_drawingTools.Brushes.Count})",
                    UITheme.Text, UITextRole.Label);
            _previousBrush.Draw();
            _nextBrush.Draw();
            foreach ((int index, Rectangle bounds) in VisibleBrushPresetBounds())
                DrawBrushPresetCard(_drawingTools.Brushes[index], bounds,
                    index == _drawingTools.SelectedBrushIndex);
            _newBrush.Draw();
            _deleteBrush.Draw();
            DrawBrushTip(brush);
            DrawBrushSettingLabel(_brushTipAlpha.Bounds, $"Paint alpha: {_brushTipAlphaPercent}%");
            _brushTipAlpha.Draw();
            _fillBrushTip.Draw();
            _clearBrushTip.Draw();
            new UITextContainer { Bounds = new Rectangle(_brushTipAlpha.Bounds.X, _clearBrushTip.Bounds.Bottom + 8,
                _brushTipAlpha.Bounds.Width, 58), Padding = 0, HorizontalAlignment = UIAlign.Start, MaxLines = 3 }
                .Draw(brush.IsBuiltIn ? "Preset is read-only\nChoose New to edit a copy"
                    : "Left drag paints\nRight drag erases", UITheme.TextMuted, UITextRole.Caption);
            if (!string.IsNullOrWhiteSpace(_drawingTools.BrushStoreError))
                new UITextContainer { Bounds = new Rectangle(panel.X + 16, panel.Bottom - 22, panel.Width - 32, 18), MaxLines = 1 }
                    .Draw(_drawingTools.BrushStoreError, UITheme.Danger, UITextRole.Caption);
        }

        private static void DrawBrushPresetCard(BrushPreset brush, Rectangle bounds, bool selected)
        {
            UIRenderer.Fill(bounds, selected ? UITheme.SurfaceRaised : UITheme.Surface);
            UIRenderer.Border(bounds, selected ? UITheme.Primary : UITheme.Border, selected ? 2 : 1);
            Rectangle preview = new(bounds.X + 6, bounds.Y + 5, 40, 40);
            UIRenderer.Fill(preview, UITheme.CanvasStage);
            DrawBrushPreview(brush, preview, selected ? UITheme.Primary : UITheme.Text);
            new UITextContainer
            {
                Bounds = new Rectangle(preview.Right + 5, bounds.Y + 2,
                    Math.Max(1, bounds.Right - preview.Right - 8), bounds.Height - 4),
                Padding = 0,
                HorizontalAlignment = UIAlign.Start,
                MaxLines = 2
            }.Draw(brush.Name, selected ? UITheme.Primary : UITheme.Text, UITextRole.Caption);
        }

        private static void DrawBrushPreview(BrushPreset brush, Rectangle bounds, Color color)
        {
            for (int y = 0; y < brush.TipHeight; y++)
            for (int x = 0; x < brush.TipWidth; x++)
            {
                byte alpha = brush.AlphaAt(x, y);
                if (alpha == 0) continue;
                int left = bounds.X + x * bounds.Width / brush.TipWidth;
                int top = bounds.Y + y * bounds.Height / brush.TipHeight;
                int right = bounds.X + (x + 1) * bounds.Width / brush.TipWidth;
                int bottom = bounds.Y + (y + 1) * bounds.Height / brush.TipHeight;
                UIRenderer.Fill(new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top)),
                    color * (alpha / 255f));
            }
        }

        private void DrawBrushTip(BrushPreset brush)
        {
            UIRenderer.Fill(_brushTipBounds, UITheme.CanvasStage);
            for (int y = 0; y < brush.TipHeight; y++)
            for (int x = 0; x < brush.TipWidth; x++)
            {
                int left = _brushTipBounds.X + x * _brushTipBounds.Width / brush.TipWidth;
                int top = _brushTipBounds.Y + y * _brushTipBounds.Height / brush.TipHeight;
                int right = _brushTipBounds.X + (x + 1) * _brushTipBounds.Width / brush.TipWidth;
                int bottom = _brushTipBounds.Y + (y + 1) * _brushTipBounds.Height / brush.TipHeight;
                Rectangle cell = new(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
                Color checker = (x + y) % 2 == 0 ? UITheme.Surface : UITheme.SurfaceRaised;
                UIRenderer.Fill(cell, checker);
                byte alpha = brush.AlphaAt(x, y);
                if (alpha > 0) UIRenderer.Fill(cell, UITheme.Text * (alpha / 255f));
                if (_brushTipBounds.Width / brush.TipWidth >= 5)
                    UIRenderer.Border(cell, UITheme.Border * .45f);
            }
            UIRenderer.Border(_brushTipBounds, UITheme.Primary, 2);
        }

        private static void DrawBrushSettingLabel(Rectangle slider, string label) =>
            new UITextContainer { Bounds = new Rectangle(slider.X, slider.Y - 20, slider.Width, 20),
                Padding = 0, HorizontalAlignment = UIAlign.Start, MaxLines = 1 }.Draw(
                    label, UITheme.TextMuted, UITextRole.Caption);

        private void DrawHelp()
        {
            _helpPopover.Draw();
            Rectangle panel = _helpPopover.Bounds;
            Rectangle viewport = HelpViewport(panel);
            new UITextContainer { Bounds = new Rectangle(panel.X + 22, panel.Y + 18, panel.Width - 44, 48), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Keyboard shortcuts", UITheme.Primary, UITextRole.Title);
            for (int i = 0; i < HelpShortcuts.Length; i++)
            {
                Rectangle row = new(panel.X + 22, panel.Y + 72 + i * 24 - _helpScroll, panel.Width - 44, 24);
                if (FullyVisible(row, viewport))
                    new UITextContainer { Bounds = row, HorizontalAlignment = UIAlign.Start, Padding = 4, MaxLines = 1 }
                        .Draw(HelpShortcuts[i], UITheme.Text, UITextRole.Body);
            }
        }

        private void DrawSettings()
        {
            _settingsPopover.Draw();
            Rectangle panel = _settingsPopover.Bounds;
            Rectangle viewport = SettingsViewport(panel);
            int scroll = _settingsScroll;
            new UITextContainer { Bounds = new Rectangle(panel.X + 24, panel.Y + 16, panel.Width - 48, 48), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Animation settings", UITheme.Primary, UITextRole.Heading);
            void TextIfVisible(Rectangle bounds, string text, Color color, UITextRole role,
                UIAlign alignment = UIAlign.Center)
            {
                if (FullyVisible(bounds, viewport))
                    new UITextContainer { Bounds = bounds, HorizontalAlignment = alignment, MaxLines = 1 }
                        .Draw(text, color, role);
            }
            if (FullyVisible(_onionSkin.Bounds, viewport)) _onionSkin.Draw();
            TextIfVisible(new Rectangle(panel.X + 88, panel.Y + 89 - scroll, panel.Width - 112, 42),
                $"Onion skin: {(_animation.isOnionSkinEnabled ? "On" : "Off")}", UITheme.Text,
                UITextRole.Body, UIAlign.Start);
            TextIfVisible(new Rectangle(panel.X + 28, panel.Y + 130 - scroll, 120, 44),
                "Onion opacity", UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (FullyVisible(_onionOpacity.Bounds, viewport)) _onionOpacity.Draw();
            TextIfVisible(new Rectangle(_onionOpacity.Bounds.X, panel.Y + 164 - scroll, _onionOpacity.Bounds.Width, 24),
                $"{(int)Math.Round(_animation.OnionSkinOpacity * 100)}%", UITheme.TextMuted, UITextRole.Caption);
            TextIfVisible(new Rectangle(panel.X + 28, panel.Y + 190 - scroll, 120, 38),
                "Previous frames", UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (FullyVisible(_previousOnionDown.Bounds, viewport)) _previousOnionDown.Draw();
            if (FullyVisible(_previousOnionUp.Bounds, viewport)) _previousOnionUp.Draw();
            TextIfVisible(new Rectangle(_previousOnionDown.Bounds.Right, panel.Y + 190 - scroll,
                Math.Max(1, _previousOnionUp.Bounds.X - _previousOnionDown.Bounds.Right), 38),
                _animation.PreviousOnionFrames.ToString(), UITheme.Primary, UITextRole.Label);
            TextIfVisible(new Rectangle(panel.X + 28, panel.Y + 236 - scroll, 120, 38),
                "Next frames", UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (FullyVisible(_nextOnionDown.Bounds, viewport)) _nextOnionDown.Draw();
            if (FullyVisible(_nextOnionUp.Bounds, viewport)) _nextOnionUp.Draw();
            TextIfVisible(new Rectangle(_nextOnionDown.Bounds.Right, panel.Y + 236 - scroll,
                Math.Max(1, _nextOnionUp.Bounds.X - _nextOnionDown.Bounds.Right), 38),
                _animation.NextOnionFrames.ToString(), UITheme.Primary, UITextRole.Label);
            TextIfVisible(new Rectangle(panel.X + 28, panel.Y + 290 - scroll, 120, 42),
                "Playback FPS", UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (FullyVisible(_fpsDown.Bounds, viewport)) _fpsDown.Draw();
            if (FullyVisible(_fpsUp.Bounds, viewport)) _fpsUp.Draw();
            TextIfVisible(new Rectangle(_fpsDown.Bounds.Right, panel.Y + 290 - scroll,
                Math.Max(1, _fpsUp.Bounds.X - _fpsDown.Bounds.Right), 42),
                _animation.fps.ToString(), UITheme.Primary, UITextRole.Label);
            if (FullyVisible(_autosaveToggle.Bounds, viewport)) _autosaveToggle.Draw();
            TextIfVisible(new Rectangle(panel.X + 88, panel.Y + 339 - scroll, panel.Width - 112, 42),
                $"Autosave: {(_autosave.IsEnabled ? "On" : "Off")}", UITheme.Text,
                UITextRole.Body, UIAlign.Start);
            if (FullyVisible(_transparentBackgroundToggle.Bounds, viewport))
                _transparentBackgroundToggle.Draw();
            TextIfVisible(new Rectangle(panel.X + 88, panel.Y + 387 - scroll, panel.Width - 112, 42),
                $"Canvas background: {(_animation.IsCanvasBackgroundTransparent ? "Transparent" : "Solid")}",
                UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (FullyVisible(_useBrushColorForBackground.Bounds, viewport))
            {
                _useBrushColorForBackground.Draw();
                Rectangle swatch = new(panel.X + 28, panel.Y + 437 - scroll, 32, 32);
                UIRenderer.Fill(swatch, _animation.CanvasBackgroundColor);
                UIRenderer.Border(swatch, UITheme.Border, 2);
            }
            Rectangle sizeBounds = new(panel.X + 28, panel.Y + 496 - scroll, panel.Width - 56, 28);
            if (FullyVisible(sizeBounds, viewport)) DrawSizeBar(sizeBounds);
            string saveError = string.IsNullOrEmpty(_saveError) ? _autosave.LastError : _saveError;
            if (!string.IsNullOrEmpty(saveError))
            {
                Rectangle errorBounds = new(panel.X + 24, panel.Y + 522 - scroll, panel.Width - 48, 28);
                if (FullyVisible(errorBounds, viewport))
                    new UITextContainer { Bounds = errorBounds, MaxLines = 2 }.Draw(
                        saveError, Color.IndianRed, UITextRole.Caption);
            }
            if (FullyVisible(_save.Bounds, viewport)) _save.Draw(true);
        }

        private void DrawSizeBar(Rectangle bounds)
        {
            int percent = (int)Math.Ceiling(_animation.ResourceBudgetRemaining * 100);
            const int labelWidth = 58;
            const int maxTrackWidth = 140;
            new UITextContainer { Bounds = new Rectangle(bounds.X, bounds.Y, labelWidth, bounds.Height), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Size", UITheme.TextMuted, UITextRole.Caption);
            Rectangle track = new(bounds.X + labelWidth, bounds.Y + (bounds.Height - 8) / 2,
                Math.Max(1, Math.Min(maxTrackWidth, bounds.Width - labelWidth - 38)), 8);
            UIRenderer.Fill(track, UITheme.Surface);
            UIRenderer.Border(track, UITheme.Border);
            Rectangle remaining = new(track.X + 1, track.Y + 1,
                (int)Math.Round(Math.Max(0, track.Width - 2) * _animation.ResourceBudgetRemaining), Math.Max(1, track.Height - 2));
            UIRenderer.Fill(remaining, percent <= 10 ? Color.IndianRed : percent <= 25 ? Color.Orange : UITheme.Primary);
            new UITextContainer { Bounds = new Rectangle(track.Right + 6, bounds.Y, 52, bounds.Height), Padding = 0, MaxLines = 1 }
                .Draw($"{percent}%", UITheme.TextMuted, UITextRole.Caption);
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
                    .Draw(status, selected ? Color.White : UITheme.Text, UITextRole.Label);
            }
            Rectangle toolbar = new(panel.X + 8, panel.Bottom - Layout.LayerToolbarHeight, panel.Width - 16, 36);
            IReadOnlyList<Rectangle> actions = UILayoutEngine.Stack(toolbar, UIAxis.Horizontal, 4, 6);
            string[] labels = { "+", "-", "^", "v" };
            for (int i = 0; i < actions.Count; i++)
            {
                bool enabled = i != 0 || _animation.CanAddLayer;
                UIRenderer.Fill(actions[i], enabled ? UITheme.SurfaceRaised : UITheme.Surface);
                UIRenderer.Border(actions[i], UITheme.Border, 2);
                UIRenderer.CenteredText(labels[i], actions[i], !enabled ? UITheme.TextMuted : i == 1 ? UITheme.Danger : UITheme.Text,
                    UITextRole.Button);
            }
            if (!_animation.CanAddLayer && UIPointerRouter.ContainsPointer(actions[0]))
                UIRenderer.Tooltip(_animation.LayerLimitMessage, actions[0], preferBelow: false, border: UITheme.Border);
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
            _brushPresetIcon.Dispose();
            _selectionIcon.Dispose();
        }
    }
}
