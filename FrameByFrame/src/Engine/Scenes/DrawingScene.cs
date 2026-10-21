using System;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.Engine.Audio;
using FrameByFrame.src.UI;
using FrameByFrame.src.UI.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FrameByFrame.src.Engine.Scenes
{
    public class DrawingScene : BaseScene
    {
        private const float RotationDegreesPerPixel = .5f;

        public Animation.Animation animation = null!;
        public DrawingTools drawingTools = null!;
        private DrawingNavbarComponent _navbar = null!;
        private TimelineComponent _timeline = null!;
        private SelectionControlsComponent _selectionControls = null!;
        private AudioPlaybackController _audioPlayback = null!;
        private readonly AutosaveService _autosave = new();
        private readonly CanvasView _canvasView = new();
        private bool _isPanning;
        private bool _isRotating;
        private readonly UIFeedbackState _feedback = new();
        private UIFeedbackOverlay _feedbackOverlay = null!;
        private string _lastAutosaveError = string.Empty;
        private readonly UIActionButton _saveAndLeave;
        private readonly UIActionButton _discardAndLeave;
        private readonly UIActionButton _cancelLeave;
        private Action? _pendingNavigation;
        private Rectangle _leaveDialog;
        private string _lastHistoryError = string.Empty;
        public bool loadedScene;

        public DrawingScene(IApplicationContext app, INavigationService navigation)
            : base(app, navigation)
        {
            _saveAndLeave = new UIActionButton("Save", SaveAndContinue);
            _discardAndLeave = new UIActionButton("Discard", DiscardAndContinue);
            _cancelLeave = new UIActionButton("Cancel", CancelNavigation);
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            animation = new Animation.Animation($"Project_{DateTime.Now:yyyyMMdd_HHmmss}");
            drawingTools = new DrawingTools(animation);
            loadedScene = false;
        }

        public override void LoadContent()
        {
            SetupUI();
            if (animation.TotalFrames == 0) animation.InitializeFrames();
        }

        public Color GetSelectedColorFromColorWheel() => _navbar?.SelectedColor ?? Color.Black;

        private void SetSelectedColor(Color color)
        {
            _navbar?.SetSelectedColor(color);
        }

        private void SetupUI()
        {
            _navbar?.Dispose();
            _audioPlayback?.Dispose();
            _navbar = new DrawingNavbarComponent(App, animation, drawingTools, _autosave, _feedback, RequestHome);
            _audioPlayback = new AudioPlaybackController(animation);
            _timeline = new TimelineComponent(animation, _audioPlayback);
            _selectionControls = new SelectionControlsComponent(drawingTools);
            _feedbackOverlay ??= new UIFeedbackOverlay(_feedback);
            _navbar.Arrange(new Rectangle(0, 0, App.ScreenWidth, UITheme.AppBarHeight));
        }

        public override void Update(GameTime gameTime)
        {
            UIPointerRouter.BeginFrame();
            _feedback.Advance(gameTime);
            if (_navbar.HasPendingSave)
            {
                _navbar.ProcessPendingSave();
                return;
            }
            int navbarHeight = DrawingNavbarComponent.PreferredHeight(App.ScreenWidth);
            int timelineHeight = Math.Min(UITheme.TimelineHeight,
                Math.Max(72, (App.ScreenHeight - navbarHeight) / 3));
            _navbar.Arrange(new Rectangle(0, 0, App.ScreenWidth, navbarHeight));
            _timeline.Arrange(new Rectangle(0, App.ScreenHeight - timelineHeight,
                App.ScreenWidth, timelineHeight));
            Rectangle stage = CanvasStageBounds(navbarHeight, timelineHeight);
            _canvasView.Arrange(stage, (int)animation.frameSize.X, (int)animation.frameSize.Y);
            int selectionWidth = Math.Max(1, Math.Min(500, App.ScreenWidth - 16));
            _selectionControls.Arrange(new Rectangle(App.ScreenWidth - selectionWidth - 8,
                _timeline.Bounds.Y - 70, selectionWidth, 62));
            _feedbackOverlay.Arrange(new Rectangle(0, 0,
                App.ScreenWidth, App.ScreenHeight));
            ArrangeLeaveDialog();
            if (_pendingNavigation != null)
            {
                UpdateLeaveDialog();
                return;
            }
            if (_feedback.IsVisible)
            {
                _feedbackOverlay.Update();
                return;
            }
            HandleKeyboardShortcuts();
            if (!_navbar.HasOpenPopover) _timeline.Update();
            _navbar.Update();
            if (!_navbar.HasOpenPopover) _selectionControls.Update();
            HandleCanvasViewInput(stage);
            HandleMouseShortcuts();
            animation.Animate(gameTime);
            _audioPlayback.Update();
            _autosave.Update(gameTime, animation);
            if (!string.IsNullOrWhiteSpace(_autosave.LastError) && _autosave.LastError != _lastAutosaveError)
                _feedback.ShowError("Autosave failed", _autosave.LastError,
                    "Check available disk space and Projects folder permissions. You can retry with Save Project.");
            _lastAutosaveError = _autosave.LastError;
            if (!string.IsNullOrWhiteSpace(animation.LastHistoryError) &&
                animation.LastHistoryError != _lastHistoryError)
                _feedback.ShowError("Edit could not be recorded", animation.LastHistoryError,
                    "Use a smaller fill, selection, or brush stroke and try again.");
            _lastHistoryError = animation.LastHistoryError;
        }

        public override void Draw(Vector2 offset)
        {
            App.Graphics.Clear(UITheme.CanvasStage);
            int navbarHeight = _navbar.Bounds.Height;
            int timelineHeight = _timeline.Bounds.Height;
            Rectangle stage = CanvasStageBounds(navbarHeight, timelineHeight);
            _canvasView.Arrange(stage, (int)animation.frameSize.X, (int)animation.frameSize.Y);
            animation.DrawCurrentFrame(_canvasView);
            if (drawingTools.SelectedTool == DrawingTool.Selection) drawingTools.Selection.DrawOverlay();
            _timeline.Draw();
            _navbar.Draw();
            _selectionControls.Draw();
            MemoryMonitor.DrawMemoryOverlay(new Vector2(10, App.ScreenHeight - 30), UIConstants.DEBUG_MEMORY, animation);
            _feedbackOverlay.Draw();
            DrawLeaveDialog();
        }

        private Rectangle CanvasStageBounds(int navbarHeight, int timelineHeight)
        {
            int margin = Math.Min(24, Math.Max(4,
                Math.Min(App.ScreenWidth, App.ScreenHeight) / 30));
            return new Rectangle(margin, navbarHeight + margin,
                Math.Max(1, App.ScreenWidth - margin * 2),
                Math.Max(1, App.ScreenHeight - navbarHeight - timelineHeight - margin * 2));
        }

        private void HandleCanvasViewInput(Rectangle stage)
        {
            var mouse = App.Mouse;
            if (!mouse.MiddleClickHold()) _isPanning = false;
            if (!mouse.RightClickHold()) _isRotating = false;

            bool canStartGesture = !_navbar.HasOpenPopover &&
                stage.Contains(mouse.newMousePos) && !UIPointerRouter.IsPointerBlocked();
            if (canStartGesture)
            {
                if (mouse.MiddleClick())
                {
                    _isPanning = true;
                    _isRotating = false;
                }
                else if (mouse.RightClick())
                {
                    _isRotating = true;
                    _isPanning = false;
                }

                int scroll = mouse.ScrollDelta;
                if (scroll != 0)
                    _canvasView.ZoomAt(mouse.newMousePos, Math.Sign(scroll) * CanvasView.ZoomStep);
            }

            Vector2 mouseDelta = mouse.newMousePos - mouse.oldMousePos;
            if (_isPanning && mouse.MiddleClickHold()) _canvasView.PanBy(mouseDelta);
            if (_isRotating && mouse.RightClickHold())
                _canvasView.RotateBy(mouseDelta.X * RotationDegreesPerPixel);
        }

        private void HandleMouseShortcuts()
        {
            drawingTools.Update(GetSelectedColorFromColorWheel(), SetSelectedColor, loadedScene);
            if (!App.Mouse.LeftClickHold() && !loadedScene) loadedScene = true;
        }

        public void BeginNewAnimation(int width, int height)
        {
            animation?.Dispose();
            InitializeDefaults();
            animation!.InitializeFrames(width, height);
            _canvasView.Reset();
            SetupUI();
        }

        public void LoadAnimation(Animation.Animation loadedAnimation)
        {
            ArgumentNullException.ThrowIfNull(loadedAnimation);
            animation?.Dispose();
            animation = loadedAnimation;
            drawingTools = new DrawingTools(animation);
            loadedScene = false;
            _canvasView.Reset();
            SetupUI();
        }

        public override void Dispose()
        {
            _autosave.Flush(animation);
            _autosave.Dispose();
            _navbar?.Dispose();
            _audioPlayback?.Dispose();
            _navbar = null!;
            _timeline = null!;
            _audioPlayback = null!;
            animation?.Dispose();
            animation = null!;
        }

        private void HandleKeyboardShortcuts()
        {
            bool control = App.Keyboard.IsKeyHeldDown(Keys.LeftControl)
                || App.Keyboard.IsKeyHeldDown(Keys.RightControl);
            if (control && App.Keyboard.OnKeyPress(Keys.Z) && animation.Undo())
                drawingTools.Selection.Clear();
            if (control && App.Keyboard.OnKeyPress(Keys.Y) && animation.Redo())
                drawingTools.Selection.Clear();
            if (control && App.Keyboard.OnKeyPress(Keys.D)) animation.DuplicateCurrentFrame();
            bool selectionActive = drawingTools.SelectedTool == DrawingTool.Selection && drawingTools.Selection.HasSelection;
            if (control && App.Keyboard.OnKeyPress(Keys.C))
            {
                if (selectionActive) drawingTools.Selection.Copy(); else animation.CopyCurrentFrame();
            }
            if (control && App.Keyboard.OnKeyPress(Keys.V))
            {
                if (drawingTools.SelectedTool == DrawingTool.Selection && drawingTools.Selection.CanPaste)
                    drawingTools.Selection.Paste();
                else animation.PasteFrame();
            }
            if (App.Keyboard.GetPressSingle("ESC"))
                RequestHome();
            if (App.Keyboard.GetPressSingle("DELETE"))
            {
                if (selectionActive) drawingTools.Selection.Delete(); else animation.DeleteFrame();
            }
            if (App.Keyboard.GetPressSingle("P")) animation.TogglePlaying();
            if (App.Keyboard.GetPressSingle("BACKSPACE"))
            {
                animation.EraseCurrentLayer();
                drawingTools.Selection.Clear();
            }
            if (App.Keyboard.GetPressSingle("O")) animation.isOnionSkinEnabled = !animation.isOnionSkinEnabled;
            if (App.Keyboard.GetPressSingle("M")) animation.NextFrame();
            if (App.Keyboard.GetPressSingle("N")) animation.PreviousFrame();
            if (App.Keyboard.GetPressSingle("B")) animation.InsertFrame();
            if (App.Keyboard.GetPressSingle("L")) _navbar.SaveAnimation();
            if (App.Keyboard.GetPressSingle("[")) drawingTools.BrushSize--;
            if (App.Keyboard.GetPressSingle("]")) drawingTools.BrushSize++;
        }

        public void PrepareForShutdown()
        {
            if (animation == null || animation.TotalFrames == 0) return;
            _autosave.Flush(animation);
            if (animation.IsDirty) SaveService.SaveAnimation(animation);
        }

        private void RequestHome() => RequestNavigation(() =>
        {
            animation.Stop();
            Navigation.Navigate(UIConstants.MENU_SCENE);
        });

        private void RequestNavigation(Action navigation)
        {
            if (!animation.IsDirty) { navigation(); return; }
            animation.Stop();
            _pendingNavigation = navigation;
        }

        private void ArrangeLeaveDialog()
        {
            int width = Math.Min(620, Math.Max(300, App.ScreenWidth - 32));
            int height = 250;
            _leaveDialog = new Rectangle((App.ScreenWidth - width) / 2,
                (App.ScreenHeight - height) / 2, width, height);
            int buttonWidth = Math.Min(150, (width - 64) / 3);
            int gap = 12;
            int totalWidth = buttonWidth * 3 + gap * 2;
            int x = _leaveDialog.Center.X - totalWidth / 2;
            int y = _leaveDialog.Bottom - 68;
            _saveAndLeave.Arrange(new Rectangle(x, y, buttonWidth, 44));
            _discardAndLeave.Arrange(new Rectangle(x + buttonWidth + gap, y, buttonWidth, 44));
            _cancelLeave.Arrange(new Rectangle(x + (buttonWidth + gap) * 2, y, buttonWidth, 44));
        }

        private void UpdateLeaveDialog()
        {
            UIPointerRouter.Block(new Rectangle(0, 0, App.ScreenWidth, App.ScreenHeight));
            _saveAndLeave.Update();
            _discardAndLeave.Update();
            _cancelLeave.Update();
            if (App.Keyboard.GetPressSingle("ESC")) CancelNavigation();
        }

        private void DrawLeaveDialog()
        {
            if (_pendingNavigation == null) return;
            UIRenderer.Fill(new Rectangle(0, 0, App.ScreenWidth, App.ScreenHeight),
                new Color(0, 0, 0, 150));
            UIRenderer.Fill(_leaveDialog, UITheme.SurfaceRaised);
            UIRenderer.Border(_leaveDialog, UITheme.Primary, 3);
            new UITextContainer { Bounds = new Rectangle(_leaveDialog.X + 28, _leaveDialog.Y + 24,
                _leaveDialog.Width - 56, 48), MaxLines = 1 }.Draw("Save changes?", UITheme.Primary, UITextRole.Title);
            new UITextContainer { Bounds = new Rectangle(_leaveDialog.X + 28, _leaveDialog.Y + 82,
                _leaveDialog.Width - 56, 58), MaxLines = 2 }.Draw(
                "This project has changes that have not been saved.", UITheme.Text, UITextRole.Body);
            _saveAndLeave.Draw(true);
            _discardAndLeave.Draw();
            _cancelLeave.Draw();
        }

        private void SaveAndContinue()
        {
            try
            {
                _autosave.Flush();
                SaveService.SaveAnimation(animation);
                Action? navigation = _pendingNavigation;
                _pendingNavigation = null;
                navigation?.Invoke();
            }
            catch (Exception ex)
            {
                _pendingNavigation = null;
                _feedback.ShowError("Project could not be saved", ex.Message,
                    "The project remains open. Check disk space and folder permissions, then retry.");
            }
        }

        private void DiscardAndContinue()
        {
            Action? navigation = _pendingNavigation;
            _pendingNavigation = null;
            navigation?.Invoke();
        }

        private void CancelNavigation() => _pendingNavigation = null;
    }
}
