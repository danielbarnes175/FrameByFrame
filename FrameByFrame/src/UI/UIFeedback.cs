using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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
        public IReadOnlyList<string> Details { get; private set; } = Array.Empty<string>();
        public float? Progress { get; private set; }
        public float SpinnerPhase { get; private set; }
        public int Revision { get; private set; }

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

        public void ShowErrorList(string title, IEnumerable<string> details, string actionHint)
        {
            ArgumentNullException.ThrowIfNull(details);
            Show(UIFeedbackKind.Error, title, string.Empty, actionHint, isBusy: false, progress: null);
            Details = details.Where(detail => !string.IsNullOrWhiteSpace(detail)).ToArray();
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
            Details = Array.Empty<string>();
            IsBusy = isBusy;
            Progress = progress.HasValue ? Math.Clamp(progress.Value, 0f, 1f) : null;
            IsVisible = true;
            Revision++;
        }
    }

    public sealed class UIFeedbackOverlay : UIElement
    {
        private readonly UIFeedbackState _state;
        private readonly UIActionButton _dismiss;
        private Rectangle _dialog;
        private Rectangle _detailsViewport;
        private int _detailScrollIndex;
        private int _displayedRevision = -1;
        private const int DetailRowHeight = 52;

        public UIFeedbackOverlay(UIFeedbackState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _dismiss = new UIActionButton("Close", _state.Dismiss);
        }

        public override void Arrange(Rectangle bounds)
        {
            base.Arrange(bounds);
            int width = Math.Min(620, Math.Max(280, bounds.Width - 32));
            int preferredHeight = _state.Details.Count > 0 ? 520 : 340;
            int height = Math.Min(preferredHeight, Math.Max(240, bounds.Height - 32));
            _dialog = new Rectangle(bounds.Center.X - width / 2, bounds.Center.Y - height / 2, width, height);
            _dismiss.Arrange(new Rectangle(_dialog.Center.X - 90, _dialog.Bottom - 66, 180, 44));
            int detailsBottom = string.IsNullOrWhiteSpace(_state.ActionHint)
                ? _dismiss.Bounds.Y - 10
                : _dialog.Bottom - 130;
            _detailsViewport = new Rectangle(_dialog.X + 28, _dialog.Y + 78,
                _dialog.Width - 56, Math.Max(DetailRowHeight, detailsBottom - (_dialog.Y + 78)));
        }

        public override void Update()
        {
            if (!_state.IsVisible) return;
            UIPointerRouter.Block(Bounds);
            if (_displayedRevision != _state.Revision)
            {
                _displayedRevision = _state.Revision;
                _detailScrollIndex = 0;
            }
            if (_state.Details.Count > 0) UpdateDetailScroll();
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
                _dialog.Width - 56, 48), MaxLines = 1 }.Draw(_state.Title, accent, UITextRole.Title);

            if (_state.IsBusy) DrawSpinner(accent);
            if (_state.Progress.HasValue) DrawProgress(accent, _state.Progress.Value);

            int messageY = _dialog.Y + (_state.IsBusy ? 142 : 86);
            if (_state.Details.Count > 0)
                DrawDetails();
            else
            {
                new UITextContainer { Bounds = new Rectangle(_dialog.X + 34, messageY,
                    _dialog.Width - 68, 76), MaxLines = 3 }.Draw(_state.Message, UITheme.Text, UITextRole.Body);
            }
            if (!string.IsNullOrWhiteSpace(_state.ActionHint))
            {
                int hintY = _state.Details.Count > 0 ? _dialog.Bottom - 124 : messageY + 76;
                new UITextContainer { Bounds = new Rectangle(_dialog.X + 34, hintY,
                    _dialog.Width - 68, 48), MaxLines = 2 }.Draw(_state.ActionHint, UITheme.TextMuted, UITextRole.Caption);
            }
            if (!_state.IsBusy) _dismiss.Draw(true);
        }

        private void UpdateDetailScroll()
        {
            int visibleRows = VisibleDetailRows();
            int maxScroll = Math.Max(0, _state.Details.Count - visibleRows);
            int delta = 0;
            if (_detailsViewport.Contains(GlobalParameters.GlobalMouse.newMousePos) &&
                GlobalParameters.GlobalMouse.ScrollDelta != 0)
                delta = -Math.Sign(GlobalParameters.GlobalMouse.ScrollDelta);
            if (GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.Up)) delta = -1;
            if (GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.Down)) delta = 1;
            if (GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.PageUp)) delta = -visibleRows;
            if (GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.PageDown)) delta = visibleRows;
            _detailScrollIndex = Math.Clamp(_detailScrollIndex + delta, 0, maxScroll);
        }

        private void DrawDetails()
        {
            int visibleRows = VisibleDetailRows();
            int end = Math.Min(_state.Details.Count, _detailScrollIndex + visibleRows);
            int textWidth = _detailsViewport.Width - 18;
            for (int index = _detailScrollIndex; index < end; index++)
            {
                int slot = index - _detailScrollIndex;
                Rectangle row = new(_detailsViewport.X, _detailsViewport.Y + slot * DetailRowHeight,
                    textWidth, DetailRowHeight - 4);
                UIRenderer.Fill(row, slot % 2 == 0 ? UITheme.Surface : UITheme.SurfaceRaised);
                new UITextContainer { Bounds = row, HorizontalAlignment = UIAlign.Start,
                    Padding = 8, MaxLines = 2 }.Draw($"{index + 1}. {_state.Details[index]}", UITheme.Text, UITextRole.Caption);
            }

            if (_state.Details.Count <= visibleRows) return;
            Rectangle track = new(_detailsViewport.Right - 7, _detailsViewport.Y, 6,
                visibleRows * DetailRowHeight - 4);
            UIRenderer.Fill(track, UITheme.Surface);
            int thumbHeight = Math.Max(24, track.Height * visibleRows / _state.Details.Count);
            int maxScroll = _state.Details.Count - visibleRows;
            int thumbY = track.Y + (track.Height - thumbHeight) * _detailScrollIndex / maxScroll;
            UIRenderer.Fill(new Rectangle(track.X, thumbY, track.Width, thumbHeight), UITheme.Primary);
            new UITextContainer { Bounds = new Rectangle(_detailsViewport.X, _detailsViewport.Bottom - 22,
                textWidth - 8, 24), HorizontalAlignment = UIAlign.End, Padding = 0, MaxLines = 1 }
                .Draw($"{_detailScrollIndex + 1}-{end} of {_state.Details.Count}", UITheme.TextMuted, UITextRole.Caption);
        }

        private int VisibleDetailRows() => Math.Max(1,
            (_detailsViewport.Height - 22) / DetailRowHeight);

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
