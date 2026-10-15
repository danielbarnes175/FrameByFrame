using System;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.UI
{
    public enum UIFeedbackKind
    {
        Info,
        Success,
        Error
    }

    public sealed class UIFeedbackState
    {
        public bool IsVisible { get; private set; }
        public bool IsBusy { get; private set; }
        public UIFeedbackKind Kind { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Message { get; private set; } = string.Empty;
        public string ActionHint { get; private set; } = string.Empty;
        public float? Progress { get; private set; }
        public float SpinnerPhase { get; private set; }

        public void ShowBusy(string title, string message, float? progress = null)
        {
            Show(UIFeedbackKind.Info, title, message, string.Empty, isBusy: true, progress);
        }

        public void ShowSuccess(string title, string message)
        {
            Show(UIFeedbackKind.Success, title, message, string.Empty, isBusy: false, progress: null);
        }

        public void ShowError(string title, string message, string actionHint)
        {
            Show(UIFeedbackKind.Error, title, message, actionHint, isBusy: false, progress: null);
        }

        public void Advance(GameTime gameTime)
        {
            if (!IsVisible || !IsBusy || gameTime == null) return;
            SpinnerPhase = (SpinnerPhase + (float)gameTime.ElapsedGameTime.TotalSeconds * 2f) % 1f;
        }

        public void Dismiss()
        {
            if (IsBusy) return;
            IsVisible = false;
        }

        public void Clear()
        {
            IsVisible = false;
            IsBusy = false;
            Progress = null;
        }

        private void Show(UIFeedbackKind kind, string title, string message, string actionHint,
            bool isBusy, float? progress)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            ActionHint = actionHint ?? string.Empty;
            IsBusy = isBusy;
            Progress = progress.HasValue ? Math.Clamp(progress.Value, 0f, 1f) : null;
            IsVisible = true;
        }
    }

    public sealed class UIFeedbackOverlay : UIElement
    {
        private readonly UIFeedbackState _state;
        private readonly UIActionButton _dismiss;
        private Rectangle _dialog;

        public UIFeedbackOverlay(UIFeedbackState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _dismiss = new UIActionButton("Close", _state.Dismiss);
        }

        public override void Arrange(Rectangle bounds)
        {
            base.Arrange(bounds);
            int width = Math.Min(620, Math.Max(280, bounds.Width - 32));
            int height = Math.Min(340, Math.Max(240, bounds.Height - 32));
            _dialog = new Rectangle(bounds.Center.X - width / 2, bounds.Center.Y - height / 2, width, height);
            _dismiss.Arrange(new Rectangle(_dialog.Center.X - 90, _dialog.Bottom - 66, 180, 44));
        }

        public override void Update()
        {
            if (!_state.IsVisible) return;
            UIPointerRouter.Block(Bounds);
            if (!_state.IsBusy)
            {
                _dismiss.Update();
                if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC")) _state.Dismiss();
            }
        }

        public override void Draw()
        {
            if (!_state.IsVisible) return;
            UIRenderer.Fill(Bounds, new Color(0, 0, 0, 150));
            UIRenderer.Fill(_dialog, UITheme.SurfaceRaised);
            Color accent = _state.Kind == UIFeedbackKind.Error ? UITheme.Danger : UITheme.Primary;
            UIRenderer.Border(_dialog, accent, 3);
            new UITextContainer { Bounds = new Rectangle(_dialog.X + 28, _dialog.Y + 20,
                _dialog.Width - 56, 48), MaxLines = 1 }.Draw(_state.Title, accent, .95f);

            if (_state.IsBusy) DrawSpinner(accent);
            if (_state.Progress.HasValue) DrawProgress(accent, _state.Progress.Value);

            int messageY = _dialog.Y + (_state.IsBusy ? 142 : 86);
            new UITextContainer { Bounds = new Rectangle(_dialog.X + 34, messageY,
                _dialog.Width - 68, 76), MaxLines = 3 }.Draw(_state.Message, UITheme.Text, .68f);
            if (!string.IsNullOrWhiteSpace(_state.ActionHint))
                new UITextContainer { Bounds = new Rectangle(_dialog.X + 34, messageY + 76,
                    _dialog.Width - 68, 48), MaxLines = 2 }.Draw(_state.ActionHint, UITheme.TextMuted, .58f);
            if (!_state.IsBusy) _dismiss.Draw(true);
        }

        private void DrawSpinner(Color accent)
        {
            Vector2 center = new(_dialog.Center.X, _dialog.Y + 98);
            int active = (int)Math.Floor(_state.SpinnerPhase * 8f) % 8;
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                Vector2 point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 25f;
                UIRenderer.Fill(new Rectangle((int)point.X - 4, (int)point.Y - 4, 8, 8),
                    accent * (i == active ? 1f : .28f));
            }
        }

        private void DrawProgress(Color accent, float progress)
        {
            Rectangle track = new(_dialog.X + 46, _dialog.Y + 126, _dialog.Width - 92, 12);
            UIRenderer.Fill(track, UITheme.Surface);
            UIRenderer.Border(track, UITheme.Border);
            UIRenderer.Fill(new Rectangle(track.X + 1, track.Y + 1,
                (int)Math.Round((track.Width - 2) * progress), Math.Max(1, track.Height - 2)), accent);
        }
    }
}
