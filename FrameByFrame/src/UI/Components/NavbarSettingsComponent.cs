using System;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.UI.Components
{
    internal sealed class NavbarSettingsComponent
    {
        public const int ContentHeight = 620;
        private const int MaxPlaybackFps = 60;
        private readonly Animation _animation;
        private readonly AutosaveService _autosave;
        private readonly NavbarSaveCoordinator _saveCoordinator;
        private readonly Func<Color> _selectedColor;
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
        private int _scroll;

        public UIIconButton Button { get; }
        public UIPopover Popover { get; } = new();

        public NavbarSettingsComponent(Animation animation, AutosaveService autosave,
            NavbarSaveCoordinator saveCoordinator, Func<Color> selectedColor,
            UIIconButton button)
        {
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));
            _autosave = autosave ?? throw new ArgumentNullException(nameof(autosave));
            _saveCoordinator = saveCoordinator ?? throw new ArgumentNullException(nameof(saveCoordinator));
            _selectedColor = selectedColor ?? throw new ArgumentNullException(nameof(selectedColor));
            Button = button ?? throw new ArgumentNullException(nameof(button));
            _onionSkin = new UIToggle(_animation.isOnionSkinEnabled,
                value => _animation.isOnionSkinEnabled = value);
            _autosaveToggle = new UIToggle(_autosave.IsEnabled, SetAutosave);
            _transparentBackgroundToggle = new UIToggle(_animation.IsCanvasBackgroundTransparent,
                _animation.SetCanvasBackgroundTransparent);
            _useBrushColorForBackground = new UIActionButton("Use brush color",
                () => _animation.SetCanvasBackgroundColor(_selectedColor()));
            _previousOnionDown = new UIActionButton("-", () => _animation.PreviousOnionFrames--);
            _previousOnionUp = new UIActionButton("+", () => _animation.PreviousOnionFrames++);
            _nextOnionDown = new UIActionButton("-", () => _animation.NextOnionFrames--);
            _nextOnionUp = new UIActionButton("+", () => _animation.NextOnionFrames++);
            _onionOpacity = new UISlider(0, 100, (int)Math.Round(_animation.OnionSkinOpacity * 100),
                value => _animation.OnionSkinOpacity = value / 100f);
            _fpsDown = new UIActionButton("-", () => _animation.fps = Math.Max(1, _animation.fps - 1));
            _fpsUp = new UIActionButton("+", () => _animation.fps = Math.Min(MaxPlaybackFps, _animation.fps + 1));
            _save = new UIActionButton("SAVE PROJECT", _saveCoordinator.Request);
        }

        public void ArrangePopover(Rectangle bounds)
        {
            Popover.Arrange(bounds);
            _scroll = Math.Clamp(_scroll, 0, Math.Max(0, ContentHeight - bounds.Height));
            int controlX = Math.Max(bounds.X + 150, bounds.Right - 245);
            int controlRight = bounds.Right - 28;
            _onionSkin.Arrange(new Rectangle(bounds.X + 28, bounds.Y + 92 - _scroll, 48, 28));
            _onionOpacity.Arrange(new Rectangle(controlX, bounds.Y + 130 - _scroll,
                Math.Max(70, controlRight - controlX), 44));
            _previousOnionDown.Arrange(new Rectangle(controlX, bounds.Y + 190 - _scroll, 42, 38));
            _previousOnionUp.Arrange(new Rectangle(controlRight - 42, bounds.Y + 190 - _scroll, 42, 38));
            _nextOnionDown.Arrange(new Rectangle(controlX, bounds.Y + 236 - _scroll, 42, 38));
            _nextOnionUp.Arrange(new Rectangle(controlRight - 42, bounds.Y + 236 - _scroll, 42, 38));
            _fpsDown.Arrange(new Rectangle(controlX, bounds.Y + 290 - _scroll, 48, 42));
            _fpsUp.Arrange(new Rectangle(controlRight - 48, bounds.Y + 290 - _scroll, 48, 42));
            _autosaveToggle.Arrange(new Rectangle(bounds.X + 28, bounds.Y + 342 - _scroll, 48, 28));
            _transparentBackgroundToggle.Arrange(new Rectangle(bounds.X + 28, bounds.Y + 390 - _scroll, 48, 28));
            _useBrushColorForBackground.Arrange(new Rectangle(controlX, bounds.Y + 432 - _scroll,
                Math.Max(120, controlRight - controlX), 42));
            int saveWidth = Math.Min(210, bounds.Width - 40);
            _save.Arrange(new Rectangle(bounds.Center.X - saveWidth / 2,
                bounds.Y + 552 - _scroll, saveWidth, 50));
        }

        public void UpdateContent()
        {
            Rectangle panel = Popover.Bounds;
            if (panel.Contains(GlobalParameters.GlobalMouse.newMousePos) && GlobalParameters.GlobalMouse.ScrollDelta != 0)
            {
                _scroll = Math.Clamp(_scroll - Math.Sign(GlobalParameters.GlobalMouse.ScrollDelta) * 36,
                    0, Math.Max(0, ContentHeight - panel.Height));
                ArrangePopover(panel);
            }
            _onionSkin.Value = _animation.isOnionSkinEnabled;
            _autosaveToggle.Value = _autosave.IsEnabled;
            _transparentBackgroundToggle.Value = _animation.IsCanvasBackgroundTransparent;
            Rectangle viewport = Viewport(panel);
            if (Visible(_onionSkin.Bounds, viewport)) _onionSkin.Update();
            if (Visible(_previousOnionDown.Bounds, viewport)) _previousOnionDown.Update();
            if (Visible(_previousOnionUp.Bounds, viewport)) _previousOnionUp.Update();
            if (Visible(_nextOnionDown.Bounds, viewport)) _nextOnionDown.Update();
            if (Visible(_nextOnionUp.Bounds, viewport)) _nextOnionUp.Update();
            _onionOpacity.SetValue((int)Math.Round(_animation.OnionSkinOpacity * 100));
            _onionOpacity.IsEnabled = _animation.isOnionSkinEnabled;
            if (Visible(_onionOpacity.Bounds, viewport)) _onionOpacity.Update();
            if (Visible(_fpsDown.Bounds, viewport)) _fpsDown.Update();
            if (Visible(_fpsUp.Bounds, viewport)) _fpsUp.Update();
            if (Visible(_autosaveToggle.Bounds, viewport)) _autosaveToggle.Update();
            if (Visible(_transparentBackgroundToggle.Bounds, viewport)) _transparentBackgroundToggle.Update();
            if (Visible(_useBrushColorForBackground.Bounds, viewport)) _useBrushColorForBackground.Update();
            if (Visible(_save.Bounds, viewport)) _save.Update();
        }

        private void SetAutosave(bool enabled)
        {
            try
            {
                _autosave.SetEnabled(enabled);
                _saveCoordinator.SetError(string.Empty);
            }
            catch (Exception ex)
            {
                _saveCoordinator.SetError($"Could not save autosave preference: {ex.Message}");
                _autosaveToggle.Value = _autosave.IsEnabled;
            }
        }

        public void DrawContent()
        {
            Popover.Draw();
            Rectangle panel = Popover.Bounds;
            Rectangle viewport = Viewport(panel);
            int scroll = _scroll;
            new UITextContainer { Bounds = new Rectangle(panel.X + 24, panel.Y + 16, panel.Width - 48, 48), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Animation settings", UITheme.Primary, UITextRole.Heading);
            void Text(Rectangle bounds, string value, Color color, UITextRole role, UIAlign alignment = UIAlign.Center)
            {
                if (Visible(bounds, viewport)) new UITextContainer { Bounds = bounds, HorizontalAlignment = alignment, MaxLines = 1 }
                    .Draw(value, color, role);
            }
            if (Visible(_onionSkin.Bounds, viewport)) _onionSkin.Draw();
            Text(new Rectangle(panel.X + 88, panel.Y + 89 - scroll, panel.Width - 112, 42),
                $"Onion skin: {(_animation.isOnionSkinEnabled ? "On" : "Off")}", UITheme.Text, UITextRole.Body, UIAlign.Start);
            Text(new Rectangle(panel.X + 28, panel.Y + 130 - scroll, 120, 44), "Onion opacity", UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (Visible(_onionOpacity.Bounds, viewport)) _onionOpacity.Draw();
            Text(new Rectangle(_onionOpacity.Bounds.X, panel.Y + 164 - scroll, _onionOpacity.Bounds.Width, 24),
                $"{(int)Math.Round(_animation.OnionSkinOpacity * 100)}%", UITheme.TextMuted, UITextRole.Caption);
            Text(new Rectangle(panel.X + 28, panel.Y + 190 - scroll, 120, 38), "Previous frames", UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (Visible(_previousOnionDown.Bounds, viewport)) _previousOnionDown.Draw();
            if (Visible(_previousOnionUp.Bounds, viewport)) _previousOnionUp.Draw();
            Text(new Rectangle(_previousOnionDown.Bounds.Right, panel.Y + 190 - scroll,
                Math.Max(1, _previousOnionUp.Bounds.X - _previousOnionDown.Bounds.Right), 38),
                _animation.PreviousOnionFrames.ToString(), UITheme.Primary, UITextRole.Label);
            Text(new Rectangle(panel.X + 28, panel.Y + 236 - scroll, 120, 38), "Next frames", UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (Visible(_nextOnionDown.Bounds, viewport)) _nextOnionDown.Draw();
            if (Visible(_nextOnionUp.Bounds, viewport)) _nextOnionUp.Draw();
            Text(new Rectangle(_nextOnionDown.Bounds.Right, panel.Y + 236 - scroll,
                Math.Max(1, _nextOnionUp.Bounds.X - _nextOnionDown.Bounds.Right), 38),
                _animation.NextOnionFrames.ToString(), UITheme.Primary, UITextRole.Label);
            Text(new Rectangle(panel.X + 28, panel.Y + 290 - scroll, 120, 42), "Playback FPS", UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (Visible(_fpsDown.Bounds, viewport)) _fpsDown.Draw();
            if (Visible(_fpsUp.Bounds, viewport)) _fpsUp.Draw();
            Text(new Rectangle(_fpsDown.Bounds.Right, panel.Y + 290 - scroll,
                Math.Max(1, _fpsUp.Bounds.X - _fpsDown.Bounds.Right), 42), _animation.fps.ToString(), UITheme.Primary, UITextRole.Label);
            if (Visible(_autosaveToggle.Bounds, viewport)) _autosaveToggle.Draw();
            Text(new Rectangle(panel.X + 88, panel.Y + 339 - scroll, panel.Width - 112, 42),
                $"Autosave: {(_autosave.IsEnabled ? "On" : "Off")}", UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (Visible(_transparentBackgroundToggle.Bounds, viewport)) _transparentBackgroundToggle.Draw();
            Text(new Rectangle(panel.X + 88, panel.Y + 387 - scroll, panel.Width - 112, 42),
                $"Canvas background: {(_animation.IsCanvasBackgroundTransparent ? "Transparent" : "Solid")}",
                UITheme.Text, UITextRole.Body, UIAlign.Start);
            if (Visible(_useBrushColorForBackground.Bounds, viewport))
            {
                _useBrushColorForBackground.Draw();
                Rectangle swatch = new(panel.X + 28, panel.Y + 437 - scroll, 32, 32);
                UIRenderer.Fill(swatch, _animation.CanvasBackgroundColor);
                UIRenderer.Border(swatch, UITheme.Border, 2);
            }
            Rectangle sizeBounds = new(panel.X + 28, panel.Y + 496 - scroll, panel.Width - 56, 28);
            if (Visible(sizeBounds, viewport)) DrawResourceBar(sizeBounds);
            string saveError = string.IsNullOrEmpty(_saveCoordinator.Error) ? _autosave.LastError : _saveCoordinator.Error;
            if (!string.IsNullOrEmpty(saveError))
            {
                Rectangle errorBounds = new(panel.X + 24, panel.Y + 522 - scroll, panel.Width - 48, 28);
                if (Visible(errorBounds, viewport)) new UITextContainer { Bounds = errorBounds, MaxLines = 2 }
                    .Draw(saveError, Color.IndianRed, UITextRole.Caption);
            }
            if (Visible(_save.Bounds, viewport)) _save.Draw(true);
        }

        public void DrawResourceBar(Rectangle bounds)
        {
            int percent = (int)Math.Ceiling(_animation.ResourceBudgetRemaining * 100);
            new UITextContainer { Bounds = new Rectangle(bounds.X, bounds.Y, 58, bounds.Height), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Size", UITheme.TextMuted, UITextRole.Caption);
            Rectangle track = new(bounds.X + 58, bounds.Y + (bounds.Height - 8) / 2,
                Math.Max(1, Math.Min(140, bounds.Width - 96)), 8);
            UIRenderer.Fill(track, UITheme.Surface);
            UIRenderer.Border(track, UITheme.Border);
            UIRenderer.Fill(new Rectangle(track.X + 1, track.Y + 1,
                (int)Math.Round(Math.Max(0, track.Width - 2) * _animation.ResourceBudgetRemaining), Math.Max(1, track.Height - 2)),
                percent <= 10 ? Color.IndianRed : percent <= 25 ? Color.Orange : UITheme.Primary);
            new UITextContainer { Bounds = new Rectangle(track.Right + 6, bounds.Y, 52, bounds.Height), Padding = 0, MaxLines = 1 }
                .Draw($"{percent}%", UITheme.TextMuted, UITextRole.Caption);
        }

        private static Rectangle Viewport(Rectangle panel) => new(panel.X + 4, panel.Y + 64,
            Math.Max(1, panel.Width - 8), Math.Max(1, panel.Height - 68));
        private static bool Visible(Rectangle bounds, Rectangle viewport) => bounds.X >= viewport.X &&
            bounds.Right <= viewport.Right && bounds.Y >= viewport.Y && bounds.Bottom <= viewport.Bottom;
    }
}
