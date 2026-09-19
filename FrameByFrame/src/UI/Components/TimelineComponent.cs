using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Audio;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogNET;

namespace FrameByFrame.src.UI.Components
{
    public sealed class TimelineComponent : UIElement
    {
        private const int NavigationWidth = 44;
        private const int Gap = 8;
        private const int ToolbarHeight = 58;
        private const int ActionSize = 44;
        private const int DragThreshold = 5;
        private const int AudioRowHeight = 34;
        private int ThumbnailWidth => Math.Clamp(Bounds.Width / 4, 64, 112);
        private readonly Animation _animation;
        private readonly AudioPlaybackController _audioPlayback;
        private readonly UIActionButton _previousPage;
        private readonly UIActionButton _nextPage;
        private readonly UIActionButton _duplicate;
        private readonly UIActionButton _insert;
        private readonly UIActionButton _delete;
        private readonly UIActionButton _copy;
        private readonly UIActionButton _paste;
        private readonly UIActionButton _mode;
        private readonly UIActionButton _addAudio;
        private readonly UIActionButton _removeAudio;
        private readonly UIActionButton _muteAudio;
        private readonly UIActionButton _startDown;
        private readonly UIActionButton _startUp;
        private readonly UIActionButton _volumeDown;
        private readonly UIActionButton _volumeUp;
        private int _firstVisibleFrame;
        private int _firstVisibleAudioTrack;
        private Guid? _selectedAudioTrack;
        private Guid? _audioDragTrack;
        private int _audioDragStartFrame;
        private float _audioDragStartX;
        private bool _showAudio;
        private Task<AudioTrack> _importTask;
        private string _audioStatus = string.Empty;
        private int _dragSource = -1;
        private int _dropTarget = -1;
        private Vector2 _dragStart;
        private bool _isDragging;

        public TimelineComponent(Animation animation, AudioPlaybackController audioPlayback = null)
        {
            _animation = animation;
            _audioPlayback = audioPlayback;
            _previousPage = new UIActionButton("<", () => Scroll(-1));
            _nextPage = new UIActionButton(">", () => Scroll(1));
            _duplicate = new UIActionButton("D", _animation.DuplicateCurrentFrame)
            {
                Tooltip = "Duplicate selected frame - Ctrl+D"
            };
            _insert = new UIActionButton("+", _animation.InsertFrame)
            {
                Tooltip = "Insert a blank frame - B"
            };
            _delete = new UIActionButton("X", _animation.DeleteFrame)
            {
                Tooltip = "Delete selected frame - Delete"
            };
            _copy = new UIActionButton("C", _animation.CopyCurrentFrame)
            {
                Tooltip = "Copy selected frame - Ctrl+C"
            };
            _paste = new UIActionButton("P", () => _animation.PasteFrame())
            {
                Tooltip = "Paste frame after selection - Ctrl+V"
            };
            _mode = new UIActionButton("Audio", ToggleMode) { Tooltip = "Switch timeline mode" };
            _addAudio = new UIActionButton("+", BeginAudioImport) { Tooltip = "Import audio at the selected frame" };
            _removeAudio = new UIActionButton("X", RemoveSelectedAudio) { Tooltip = "Remove selected audio track" };
            _muteAudio = new UIActionButton("M", ToggleSelectedAudioMute) { Tooltip = "Mute or unmute selected audio" };
            _startDown = new UIActionButton("<-", () => MoveSelectedAudio(-1)) { Tooltip = "Move audio one frame earlier" };
            _startUp = new UIActionButton("->", () => MoveSelectedAudio(1)) { Tooltip = "Move audio one frame later" };
            _volumeDown = new UIActionButton("V-", () => ChangeSelectedAudioVolume(-.1f)) { Tooltip = "Lower audio volume" };
            _volumeUp = new UIActionButton("V+", () => ChangeSelectedAudioVolume(.1f)) { Tooltip = "Raise audio volume" };
        }

        private int VisibleFrameCount => Math.Max(1,
            (Bounds.Width - NavigationWidth * 2 - Gap * 4) / (ThumbnailWidth + Gap));

        public override void Arrange(Rectangle bounds)
        {
            base.Arrange(bounds);
            int buttonY = bounds.Y + ToolbarHeight +
                Math.Max(0, (bounds.Height - ToolbarHeight - NavigationWidth) / 2);
            _previousPage.Arrange(new Rectangle(bounds.X + Gap, buttonY, NavigationWidth, 44));
            _nextPage.Arrange(new Rectangle(bounds.Right - Gap - NavigationWidth, buttonY, NavigationWidth, 44));
            int actionY = bounds.Y + (ToolbarHeight - ActionSize) / 2;
            _mode.Arrange(new Rectangle(bounds.Right - Gap - 68, actionY, 68, ActionSize));
            int toolbarRight = _mode.Bounds.X - Gap;
            int frameActionWidth = Math.Max(24, Math.Min(ActionSize,
                (toolbarRight - (bounds.X + Gap) - Gap * 4) / 5));
            UIActionButton[] frameActions = [_duplicate, _insert, _delete, _copy, _paste];
            int frameX = bounds.X + Gap;
            foreach (UIActionButton button in frameActions)
            {
                button.Arrange(new Rectangle(frameX, actionY, frameActionWidth, ActionSize));
                frameX += frameActionWidth + Gap;
            }

            const int compactGap = 4;
            int audioWidth = Math.Max(20, Math.Min(42,
                (toolbarRight - (bounds.X + Gap) - compactGap * 6) / 7));
            UIActionButton[] audioActions = [_addAudio, _removeAudio, _muteAudio, _startDown, _startUp, _volumeDown, _volumeUp];
            int audioX = bounds.X + Gap;
            foreach (UIActionButton button in audioActions)
            {
                button.Arrange(new Rectangle(audioX, actionY, audioWidth, ActionSize));
                audioX += audioWidth + compactGap;
            }
            EnsureSelectionVisible();
        }

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            _previousPage.IsEnabled = _firstVisibleFrame > 0;
            _nextPage.IsEnabled = _firstVisibleFrame + VisibleFrameCount < _animation.TotalFrames;
            _mode.Text = _showAudio ? "Frames" : "Audio";
            _mode.Update();
            PollAudioImport();
            if (_showAudio)
            {
                UpdateAudioMode();
                return;
            }
            _delete.IsEnabled = _animation.TotalFrames > 1;
            _previousPage.Update();
            _nextPage.Update();
            _duplicate.Update();
            _insert.Update();
            _delete.Update();
            _copy.Update();
            _paste.Update();

            int startX = _previousPage.Bounds.Right + Gap;
            for (int slot = 0; slot < VisibleFrameCount; slot++)
            {
                int frameIndex = _firstVisibleFrame + slot;
                if (frameIndex >= _animation.TotalFrames) break;
                Rectangle thumbnail = ThumbnailBounds(startX, slot);
                UpdateThumbnailDrag(thumbnail, frameIndex);
            }
            FinishDragIfReleased();
            EnsureSelectionVisible();
        }

        public override void Draw()
        {
            UIRenderer.Fill(Bounds, UITheme.Surface);
            UIRenderer.Fill(new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 3), UITheme.Primary);
            UIRenderer.Fill(new Rectangle(Bounds.X, Bounds.Y + 3, Bounds.Width, ToolbarHeight - 3), UITheme.SurfaceRaised);
            UIRenderer.Fill(new Rectangle(Bounds.X, Bounds.Y + ToolbarHeight - 1, Bounds.Width, 1), UITheme.Border);
            _previousPage.Draw();
            _nextPage.Draw();
            _mode.Draw(drawTooltip: false);
            if (_showAudio)
            {
                DrawAudioMode();
                _mode.DrawTooltip();
                return;
            }
            _duplicate.Draw(drawTooltip: false);
            _insert.Draw(drawTooltip: false);
            _delete.Draw(drawTooltip: false);
            _copy.Draw(drawTooltip: false);
            _paste.Draw(drawTooltip: false);

            int startX = _previousPage.Bounds.Right + Gap;
            for (int slot = 0; slot < VisibleFrameCount; slot++)
            {
                int frameIndex = _firstVisibleFrame + slot;
                if (frameIndex >= _animation.TotalFrames) break;
                Rectangle thumbnail = ThumbnailBounds(startX, slot);
                Rectangle preview = new(thumbnail.X + 4, thumbnail.Y + 4, thumbnail.Width - 8, thumbnail.Height - 28);
                _animation.GetFrameAtIndex(frameIndex)?.DrawPreview(preview, 1f,
                    _animation.IsCanvasBackgroundTransparent, _animation.CanvasBackgroundColor);
                bool selected = frameIndex == _animation.CurrentFrameIndex;
                UIRenderer.Border(thumbnail, selected ? UITheme.Primary : UITheme.Border, selected ? 3 : 1);
                if (_isDragging && frameIndex == _dropTarget)
                    UIRenderer.Border(thumbnail, UITheme.ToolSelected, 3);
                new UITextContainer { Bounds = new Rectangle(thumbnail.X, thumbnail.Bottom - 24, thumbnail.Width, 22), MaxLines = 1 }
                    .Draw((frameIndex + 1).ToString(), selected ? UITheme.Primary : UITheme.TextMuted, .65f);
            }

            // Tooltips render last so neighboring controls cannot cover them.
            _duplicate.DrawTooltip();
            _insert.DrawTooltip();
            _delete.DrawTooltip();
            _copy.DrawTooltip();
            _paste.DrawTooltip();
            _mode.DrawTooltip();
        }

        private int VisibleAudioTrackCount => Math.Max(1,
            (Bounds.Height - ToolbarHeight - Gap * 2) / AudioRowHeight);

        private Rectangle AudioRowsBounds => new(
            _previousPage.Bounds.Right + Gap, Bounds.Y + ToolbarHeight + Gap,
            Math.Max(1, _nextPage.Bounds.X - Gap - (_previousPage.Bounds.Right + Gap)),
            Math.Max(1, Bounds.Height - ToolbarHeight - Gap * 2));

        private void ToggleMode()
        {
            _showAudio = !_showAudio;
            _audioStatus = string.Empty;
        }

        private void UpdateAudioMode()
        {
            _previousPage.Update();
            _nextPage.Update();
            AudioTrack selected = SelectedAudioTrack;
            bool hasSelection = selected != null;
            _addAudio.IsEnabled = _importTask == null;
            _removeAudio.IsEnabled = _muteAudio.IsEnabled = _startDown.IsEnabled =
                _startUp.IsEnabled = _volumeDown.IsEnabled = _volumeUp.IsEnabled = hasSelection;
            _muteAudio.IsSelected = selected?.IsMuted == true;
            _addAudio.Update(); _removeAudio.Update(); _muteAudio.Update();
            _startDown.Update(); _startUp.Update(); _volumeDown.Update(); _volumeUp.Update();

            Rectangle rows = AudioRowsBounds;
            if (UIPointerRouter.ContainsPointer(rows) && GlobalParameters.GlobalMouse.ScrollDelta != 0)
            {
                int direction = Math.Sign(GlobalParameters.GlobalMouse.ScrollDelta);
                _firstVisibleAudioTrack = Math.Clamp(_firstVisibleAudioTrack - direction,
                    0, Math.Max(0, _animation.AudioTracks.Count - VisibleAudioTrackCount));
            }

            int count = Math.Min(VisibleAudioTrackCount,
                Math.Max(0, _animation.AudioTracks.Count - _firstVisibleAudioTrack));
            for (int slot = 0; slot < count; slot++)
            {
                AudioTrack track = _animation.AudioTracks[_firstVisibleAudioTrack + slot];
                Rectangle row = new(rows.X, rows.Y + slot * AudioRowHeight, rows.Width, AudioRowHeight - 3);
                if (!UIPointerRouter.Held(this, row)) continue;
                if (GlobalParameters.GlobalMouse.LeftClick())
                {
                    _selectedAudioTrack = track.Id;
                    _audioDragTrack = track.Id;
                    _audioDragStartFrame = track.StartFrame;
                    _audioDragStartX = GlobalParameters.GlobalMouse.newMousePos.X;
                }
                if (_audioDragTrack == track.Id)
                {
                    float pixelsPerFrame = rows.Width / (float)Math.Max(1, VisibleFrameCount);
                    int delta = (int)Math.Round((GlobalParameters.GlobalMouse.newMousePos.X - _audioDragStartX) / pixelsPerFrame);
                    _animation.SetAudioTrackStart(track.Id, _audioDragStartFrame + delta);
                }
            }
            if (!GlobalParameters.GlobalMouse.LeftClickHold()) _audioDragTrack = null;
            EnsureSelectedAudioVisible();
        }

        private void DrawAudioMode()
        {
            _addAudio.Draw(drawTooltip: false); _removeAudio.Draw(drawTooltip: false);
            _muteAudio.Draw(drawTooltip: false); _startDown.Draw(drawTooltip: false);
            _startUp.Draw(drawTooltip: false); _volumeDown.Draw(drawTooltip: false); _volumeUp.Draw(drawTooltip: false);
            Rectangle rows = AudioRowsBounds;
            UIRenderer.Fill(rows, UITheme.Background);
            int count = Math.Min(VisibleAudioTrackCount,
                Math.Max(0, _animation.AudioTracks.Count - _firstVisibleAudioTrack));
            float pixelsPerFrame = rows.Width / (float)Math.Max(1, VisibleFrameCount);
            for (int slot = 0; slot < count; slot++)
            {
                AudioTrack track = _animation.AudioTracks[_firstVisibleAudioTrack + slot];
                Rectangle row = new(rows.X, rows.Y + slot * AudioRowHeight, rows.Width, AudioRowHeight - 3);
                bool selected = track.Id == _selectedAudioTrack;
                UIRenderer.Fill(row, selected ? UITheme.ToolSelectedSurface : UITheme.Surface);
                UIRenderer.Border(row, selected ? UITheme.ToolSelected : UITheme.Border, selected ? 2 : 1);

                int start = track.StartFrame - _firstVisibleFrame;
                int durationFrames = Math.Max(1, (int)Math.Ceiling(track.DurationSeconds * Math.Max(1, _animation.fps)));
                int visibleStart = Math.Max(0, start);
                int visibleEnd = Math.Min(VisibleFrameCount, start + durationFrames);
                if (visibleEnd > visibleStart)
                {
                    Rectangle clip = new(row.X + (int)(visibleStart * pixelsPerFrame), row.Y + 3,
                        Math.Max(2, (int)((visibleEnd - visibleStart) * pixelsPerFrame)), row.Height - 6);
                    UIRenderer.Fill(clip, track.IsMuted ? UITheme.Border : UITheme.Secondary);
                }
                string label = $"{track.SourceName} | frame {track.StartFrame + 1} | {track.DurationSeconds:0.0}s | {(track.IsMuted ? "muted" : $"{track.Volume:P0}")}";
                new UITextContainer { Bounds = row, HorizontalAlignment = UIAlign.Start, Padding = 6, MaxLines = 1 }
                    .Draw(label, UITheme.Text, .58f);
            }
            string status = _importTask != null ? "Importing audio..." :
                !string.IsNullOrWhiteSpace(_audioPlayback?.LastError) ? _audioPlayback.LastError : _audioStatus;
            if (_animation.AudioTracks.Count == 0 && string.IsNullOrWhiteSpace(status)) status = "Import audio to add a timeline track.";
            if (!string.IsNullOrWhiteSpace(status))
                new UITextContainer { Bounds = rows, MaxLines = 2 }.Draw(status, UITheme.TextMuted, .62f);

            _addAudio.DrawTooltip(); _removeAudio.DrawTooltip(); _muteAudio.DrawTooltip();
            _startDown.DrawTooltip(); _startUp.DrawTooltip(); _volumeDown.DrawTooltip(); _volumeUp.DrawTooltip();
        }

        private AudioTrack SelectedAudioTrack => _selectedAudioTrack.HasValue
            ? _animation.AudioTracks.FirstOrDefault(track => track.Id == _selectedAudioTrack.Value)
            : null;

        private void BeginAudioImport()
        {
            try
            {
                using var dialog = new NativeFileDialog().SelectFile()
                    .AddFilter("Audio files", "*.wav,*.mp3,*.m4a,*.aac,*.ogg,*.opus,*.flac");
                DialogResult result = dialog.Open(out string output, Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
                if (result != DialogResult.Okay || string.IsNullOrWhiteSpace(output)) return;
                _audioStatus = string.Empty;
                _importTask = AudioService.ImportAsync(output, _animation.CurrentFrameIndex);
            }
            catch (Exception ex) { _audioStatus = $"Audio import failed: {ex.Message}"; }
        }

        private void PollAudioImport()
        {
            if (_importTask == null || !_importTask.IsCompleted) return;
            try
            {
                AudioTrack track = _importTask.GetAwaiter().GetResult();
                _animation.AddAudioTrack(track);
                _selectedAudioTrack = track.Id;
                _audioStatus = string.Empty;
            }
            catch (Exception ex) { _audioStatus = $"Audio import failed: {ex.Message}"; }
            finally { _importTask = null; }
        }

        private void RemoveSelectedAudio()
        {
            if (!_selectedAudioTrack.HasValue) return;
            _animation.RemoveAudioTrack(_selectedAudioTrack.Value);
            _selectedAudioTrack = _animation.AudioTracks.FirstOrDefault()?.Id;
        }

        private void ToggleSelectedAudioMute()
        {
            AudioTrack track = SelectedAudioTrack;
            if (track != null) _animation.SetAudioTrackMuted(track.Id, !track.IsMuted);
        }

        private void MoveSelectedAudio(int delta)
        {
            AudioTrack track = SelectedAudioTrack;
            if (track != null) _animation.SetAudioTrackStart(track.Id, track.StartFrame + delta);
        }

        private void ChangeSelectedAudioVolume(float delta)
        {
            AudioTrack track = SelectedAudioTrack;
            if (track != null) _animation.SetAudioTrackVolume(track.Id, track.Volume + delta);
        }

        private void EnsureSelectedAudioVisible()
        {
            AudioTrack selected = SelectedAudioTrack;
            if (selected == null) return;
            int index = _animation.AudioTracks.ToList().FindIndex(track => track.Id == selected.Id);
            if (index < _firstVisibleAudioTrack) _firstVisibleAudioTrack = index;
            else if (index >= _firstVisibleAudioTrack + VisibleAudioTrackCount)
                _firstVisibleAudioTrack = index - VisibleAudioTrackCount + 1;
        }

        private Rectangle ThumbnailBounds(int startX, int slot) => new(
            startX + slot * (ThumbnailWidth + Gap), Bounds.Y + ToolbarHeight + Gap,
            ThumbnailWidth, Math.Max(1, Bounds.Height - ToolbarHeight - Gap * 2));

        private void UpdateThumbnailDrag(Rectangle thumbnail, int frameIndex)
        {
            UIPointerRouter.Block(thumbnail);
            if (GlobalParameters.GlobalMouse.LeftClick() && UIPointerRouter.ContainsPointer(thumbnail))
            {
                _animation.SelectFrame(frameIndex);
                _dragSource = frameIndex;
                _dropTarget = frameIndex;
                _dragStart = GlobalParameters.GlobalMouse.newMousePos;
            }

            if (_dragSource < 0 || !GlobalParameters.GlobalMouse.LeftClickHold()) return;
            UIPointerRouter.Held(this, thumbnail);
            if (!_isDragging && Vector2.Distance(_dragStart, GlobalParameters.GlobalMouse.newMousePos) >= DragThreshold)
                _isDragging = true;
            if (_isDragging && UIPointerRouter.ContainsPointer(thumbnail)) _dropTarget = frameIndex;
        }

        private void FinishDragIfReleased()
        {
            bool released = GlobalParameters.GlobalMouse.oldMouse.LeftButton == ButtonState.Pressed &&
                GlobalParameters.GlobalMouse.newMouse.LeftButton == ButtonState.Released;
            if (!released) return;
            if (_isDragging && _dragSource >= 0 && _dropTarget >= 0)
                _animation.MoveFrame(_dragSource, _dropTarget);
            _dragSource = -1;
            _dropTarget = -1;
            _isDragging = false;
        }

        private void Scroll(int direction)
        {
            int page = VisibleFrameCount;
            _firstVisibleFrame = Math.Clamp(_firstVisibleFrame + direction * page, 0,
                Math.Max(0, _animation.TotalFrames - page));
        }

        private void EnsureSelectionVisible()
        {
            int count = VisibleFrameCount;
            if (_animation.CurrentFrameIndex < _firstVisibleFrame)
                _firstVisibleFrame = _animation.CurrentFrameIndex;
            else if (_animation.CurrentFrameIndex >= _firstVisibleFrame + count)
                _firstVisibleFrame = _animation.CurrentFrameIndex - count + 1;
            _firstVisibleFrame = Math.Clamp(_firstVisibleFrame, 0, Math.Max(0, _animation.TotalFrames - count));
        }
    }
}
