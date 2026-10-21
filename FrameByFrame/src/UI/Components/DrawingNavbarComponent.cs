using System;
using System.Collections.Generic;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
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
        private readonly IApplicationContext _app;
        private readonly DrawingTools _drawingTools;
        private readonly NavbarSaveCoordinator _saveCoordinator;
        private readonly UIActionButton _home;
        private readonly NavbarHelpComponent _helpComponent;
        private readonly NavbarSettingsComponent _settingsComponent;
        private readonly NavbarColorComponent _colorComponent;
        private readonly NavbarLayersComponent _layersComponent;
        private readonly NavbarToolsComponent _toolsComponent;
        private readonly List<UIIconButton> _playback = new();
        private readonly UISlider _brushSize;
        private readonly NavbarBrushesComponent _brushesComponent;
        private Rectangle _frameCounterBounds;
        private PopoverKind _openPopover;

        public Color SelectedColor => _colorComponent.SelectedColor;
        public bool HasOpenPopover => _openPopover != PopoverKind.None;
        public bool HasPendingSave => _saveCoordinator.IsPending;
        public static int PreferredHeight(int width) => width < 560 ? 160 : width < 960 ? 112 : UITheme.AppBarHeight;

        public DrawingNavbarComponent(IApplicationContext app, Animation animation, DrawingTools drawingTools, AutosaveService autosave,
            UIFeedbackState? feedback = null, Action? requestHome = null)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _animation = animation;
            _drawingTools = drawingTools;
            ArgumentNullException.ThrowIfNull(autosave);
            _saveCoordinator = new NavbarSaveCoordinator(animation, autosave, feedback);
            _home = new UIActionButton("HOME", requestHome ?? throw new ArgumentNullException(nameof(requestHome)));
            _helpComponent = new NavbarHelpComponent(app, Icon("help", () => Toggle(PopoverKind.Help), "Help"));
            _layersComponent = new NavbarLayersComponent(app, animation,
                Icon("layers", () => Toggle(PopoverKind.Layers), "Layers"));

            _colorComponent = new NavbarColorComponent(app, () => Toggle(PopoverKind.Color));

            _toolsComponent = new NavbarToolsComponent(app, drawingTools,
                asset => _app.Content.Load<Texture2D>(asset));

            _playback.Add(Icon("first_frame", _animation.FirstFrame, "First frame"));
            _playback.Add(Icon("previous_frame", _animation.PreviousFrame, "Previous frame - N"));
            _playback.Add(Icon("play", _animation.TogglePlaying, "Play / pause - P"));
            _playback.Add(Icon("next_frame", _animation.NextFrame, "Next frame - M"));
            _playback.Add(Icon("last_frame", _animation.LastFrame, "Last frame"));

            _brushSize = new UISlider(UIConstants.MIN_BRUSH_SIZE, UIConstants.MAX_BRUSH_SIZE,
                _drawingTools.BrushSize, value => _drawingTools.BrushSize = value);
            _brushesComponent = new NavbarBrushesComponent(app, drawingTools, () => Toggle(PopoverKind.Brushes));
            _settingsComponent = new NavbarSettingsComponent(app, animation, autosave, _saveCoordinator,
                () => SelectedColor, Icon("gear", () => Toggle(PopoverKind.Settings), "Animation settings"));
        }

        private UIIconButton Icon(string asset, Action action, string tooltip) =>
            new(_app.Content.Load<Texture2D>(asset), action) { Tooltip = tooltip };

        public void SaveAnimation()
        {
            _saveCoordinator.Request();
        }

        public void ProcessPendingSave()
        {
            if (!_saveCoordinator.Process()) _openPopover = PopoverKind.Settings;
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
                Place(_helpComponent.Button); Place(_settingsComponent.Button);
                _frameCounterBounds = new Rectangle(x, y, Math.Min(Layout.FrameCounterCompactWidth, Math.Max(72, bounds.Right - gap - x)), Layout.IconSize);
                x = _frameCounterBounds.Right + gap;
                foreach (UIIconButton button in _playback) Place(button);
                Place(_brushSize, Math.Min(Layout.BrushSliderCompactWidth, Math.Max(72, bounds.Width / 4)));
                foreach (UIIconButton tool in _toolsComponent.Buttons) Place(tool);
                Place(_layersComponent.Button); Place(_brushesComponent.Button); Place(_colorComponent.Button);
                goto Popovers;
            }
            _home.Arrange(new Rectangle(x, controlY, homeWidth, Layout.ControlHeight));
            x = _home.Bounds.Right + primaryGap;
            _helpComponent.Button.Arrange(new Rectangle(x, iconY, Layout.IconSize, Layout.IconSize));
            x = _helpComponent.Button.Bounds.Right + primaryGap;
            _settingsComponent.Button.Arrange(new Rectangle(x, iconY, Layout.IconSize, Layout.IconSize));

            x = _settingsComponent.Button.Bounds.Right + (compact ? Layout.ItemGap : Layout.FrameCounterGap);
            _frameCounterBounds = new Rectangle(x, controlY, frameCounterWidth, Layout.ControlHeight);
            x = _frameCounterBounds.Right + Layout.OuterPadding;
            foreach (UIIconButton button in _playback)
            {
                button.Arrange(new Rectangle(x, iconY, Layout.IconSize, Layout.IconSize));
                x += iconStep;
            }
            _brushSize.Arrange(new Rectangle(x + Layout.ItemGap, controlY, brushSliderWidth, Layout.ControlHeight));
            _colorComponent.Button.Arrange(new Rectangle(bounds.Right - Layout.OuterPadding - Layout.IconSize, iconY, Layout.IconSize, Layout.IconSize));
            _brushesComponent.Button.Arrange(new Rectangle(_colorComponent.Button.Bounds.X - Layout.IconSize - trailingGap,
                iconY, Layout.IconSize, Layout.IconSize));
            _layersComponent.Button.Arrange(new Rectangle(_brushesComponent.Button.Bounds.X - Layout.IconSize - trailingGap,
                iconY, Layout.IconSize, Layout.IconSize));
            int toolGroupWidth = _toolsComponent.Buttons.Count * Layout.IconSize + Math.Max(0, _toolsComponent.Buttons.Count - 1) * Layout.ItemGap;
            int toolX = _layersComponent.Button.Bounds.X - toolGroupGap - toolGroupWidth;
            foreach (UIIconButton tool in _toolsComponent.Buttons)
            {
                tool.Arrange(new Rectangle(toolX, iconY, Layout.IconSize, Layout.IconSize));
                toolX += Layout.IconStep;
            }

        Popovers:
            int popoverTop = bounds.Bottom + Layout.ItemGap;
            int availableHeight = Math.Max(80, _app.ScreenHeight - popoverTop - Layout.PopoverEdge);
            int helpWidth = Math.Min(Layout.HelpWidth, _app.ScreenWidth - Layout.PopoverEdge * 2);
            int settingsWidth = Math.Min(Layout.SettingsWidth, _app.ScreenWidth - Layout.PopoverEdge * 2);
            _helpComponent.ArrangePopover(ClampPopover(new Rectangle(_helpComponent.Button.Bounds.X, popoverTop, helpWidth,
                Math.Min(Layout.HelpContentHeight, availableHeight)), popoverTop));
            _settingsComponent.ArrangePopover(ClampPopover(new Rectangle(_settingsComponent.Button.Bounds.X, popoverTop, settingsWidth,
                Math.Min(Layout.SettingsContentHeight, availableHeight)), popoverTop));
            _colorComponent.Popover.Arrange(ClampPopover(new Rectangle(_colorComponent.Button.Bounds.Right - Layout.ColorWidth, popoverTop, Layout.ColorWidth, Layout.ColorHeight), popoverTop));
            _layersComponent.Popover.Arrange(ClampPopover(new Rectangle(_layersComponent.Button.Bounds.Right - Layout.LayersWidth, popoverTop, Layout.LayersWidth,
                Math.Min(Layout.LayersHeight, availableHeight)), popoverTop));
            _brushesComponent.ArrangePopover(ClampPopover(new Rectangle(_brushesComponent.Button.Bounds.X, popoverTop,
                Layout.BrushesWidth, Math.Min(Layout.BrushesHeight, availableHeight)), popoverTop));

        }

        private Rectangle ClampPopover(Rectangle bounds, int minimumTop = Layout.PopoverTop)
        {
            bounds.Width = Math.Min(bounds.Width, Math.Max(1, _app.ScreenWidth - Layout.PopoverEdge * 2));
            bounds.Height = Math.Min(bounds.Height, Math.Max(1, _app.ScreenHeight - minimumTop - Layout.PopoverEdge));
            bounds.X = Math.Clamp(bounds.X, Layout.PopoverEdge, Math.Max(Layout.PopoverEdge, _app.ScreenWidth - bounds.Width - Layout.PopoverEdge));
            bounds.Y = Math.Clamp(bounds.Y, minimumTop,
                Math.Max(minimumTop, _app.ScreenHeight - bounds.Height - Layout.PopoverEdge));
            return bounds;
        }

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            _home.Update();
            _helpComponent.Button.Update();
            _settingsComponent.Button.Update();
            _colorComponent.Button.Update();
            _layersComponent.Button.Update();
            _brushesComponent.Button.Update();
            _toolsComponent.Update();
            foreach (UIIconButton button in _playback) button.Update();
            _brushSize.SetValue(_drawingTools.BrushSize);
            _brushSize.Update();

            _helpComponent.Popover.IsOpen = _openPopover == PopoverKind.Help;
            _settingsComponent.Popover.IsOpen = _openPopover == PopoverKind.Settings;
            _colorComponent.Popover.IsOpen = _openPopover == PopoverKind.Color;
            _layersComponent.Popover.IsOpen = _openPopover == PopoverKind.Layers;
            _brushesComponent.Popover.IsOpen = _openPopover == PopoverKind.Brushes;
            ActivePopover()?.Update();

            if (_helpComponent.Popover.IsOpen) _helpComponent.UpdateContent();
            if (_settingsComponent.Popover.IsOpen) _settingsComponent.UpdateContent();
            if (_layersComponent.Popover.IsOpen) _layersComponent.UpdateContent();
            if (_colorComponent.Popover.IsOpen) _colorComponent.UpdateContent();
            if (_brushesComponent.Popover.IsOpen) _brushesComponent.UpdateContent();
            DismissPopoverOnOutsideClick();
        }

        private UIPopover? ActivePopover() => _openPopover switch
        {
            PopoverKind.Help => _helpComponent.Popover,
            PopoverKind.Settings => _settingsComponent.Popover,
            PopoverKind.Color => _colorComponent.Popover,
            PopoverKind.Layers => _layersComponent.Popover,
            PopoverKind.Brushes => _brushesComponent.Popover,
            _ => null
        };

        private void DismissPopoverOnOutsideClick()
        {
            UIPopover? active = ActivePopover();
            if (active == null || !_app.Mouse.LeftClick()) return;
            if (active.Bounds.Contains(_app.Mouse.newMousePos)) return;
            Rectangle anchor = _openPopover switch
            {
                PopoverKind.Help => _helpComponent.Button.Bounds,
                PopoverKind.Settings => _settingsComponent.Button.Bounds,
                PopoverKind.Color => _colorComponent.Button.Bounds,
                PopoverKind.Layers => _layersComponent.Button.Bounds,
                PopoverKind.Brushes => _brushesComponent.Button.Bounds,
                _ => Rectangle.Empty
            };
            if (!anchor.Contains(_app.Mouse.newMousePos)) _openPopover = PopoverKind.None;
        }

        public void SetSelectedColor(Color color)
        {
            _colorComponent.SetSelectedColor(color);
        }

        public override void Draw()
        {
            UIRenderer.Fill(Bounds, UITheme.Surface);
            UIRenderer.Fill(new Rectangle(Bounds.X, Bounds.Bottom - 3, Bounds.Width, 3), UITheme.Primary);
            _home.Draw(true);
            _helpComponent.Button.Draw(drawTooltip: false);
            _settingsComponent.Button.Draw(drawTooltip: false);
            _colorComponent.Button.Draw(drawTooltip: false);
            _layersComponent.Button.Draw(drawTooltip: false);
            _toolsComponent.Draw();
            foreach (UIIconButton button in _playback) button.Draw(drawTooltip: false);
            _brushSize.Draw();
            _brushesComponent.Button.IsSelected = _openPopover == PopoverKind.Brushes;
            _brushesComponent.Button.Draw(drawTooltip: false);

            UIRenderer.Fill(_frameCounterBounds, UITheme.Primary);
            new UITextContainer { Bounds = _frameCounterBounds, MaxLines = 1 }.Draw(
                $"{_animation.CurrentFrameIndex + 1} / {_animation.TotalFrames}", Color.White, UITextRole.Label);

            if (_app.ScreenWidth >= Layout.ProjectLabelMinScreenWidth)
            {
                int labelX = _brushSize.Bounds.Right + 8;
                int labelWidth = Math.Max(1, _toolsComponent.Buttons[0].Bounds.X - labelX - 8);
                new UITextContainer { Bounds = new Rectangle(labelX, 4, labelWidth, 28), MaxLines = 1, HorizontalAlignment = UIAlign.Start }
                    .Draw(_animation.projectName, UITheme.TextMuted, UITextRole.Label);
                _settingsComponent.DrawResourceBar(new Rectangle(labelX, 34, labelWidth, 20));
            }

            DrawActivePopover();

            // All navbar tooltips render last so open popovers cannot cover them.
            _helpComponent.Button.DrawTooltip();
            _settingsComponent.Button.DrawTooltip();
            _colorComponent.Button.DrawTooltip();
            _layersComponent.Button.DrawTooltip();
            _toolsComponent.DrawTooltips();
            foreach (UIIconButton button in _playback) button.DrawTooltip();
            _brushesComponent.Button.DrawTooltip();
        }

        private void DrawActivePopover()
        {
            switch (_openPopover)
            {
                case PopoverKind.Help: _helpComponent.DrawContent(); break;
                case PopoverKind.Settings: _settingsComponent.DrawContent(); break;
                case PopoverKind.Color: _colorComponent.DrawContent(); break;
                case PopoverKind.Layers: _layersComponent.DrawContent(); break;
                case PopoverKind.Brushes: _brushesComponent.DrawContent(); break;
            }
        }

        public void Dispose()
        {
            _colorComponent.Dispose();
            _brushesComponent.Dispose();
            _toolsComponent.Dispose();
        }
    }
}
