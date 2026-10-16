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
        private string _lastHistoryError = string.Empty;
        public bool loadedScene;

        public DrawingScene() => InitializeDefaults();

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
            _navbar = new DrawingNavbarComponent(animation, drawingTools, _autosave, _feedback);
            _audioPlayback = new AudioPlaybackController(animation);
            _timeline = new TimelineComponent(animation, _audioPlayback);
            _selectionControls = new SelectionControlsComponent(drawingTools);
            _feedbackOverlay ??= new UIFeedbackOverlay(_feedback);
            _navbar.Arrange(new Rectangle(0, 0, GlobalParameters.screenWidth, UITheme.AppBarHeight));
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
            int navbarHeight = DrawingNavbarComponent.PreferredHeight(GlobalParameters.screenWidth);
            int timelineHeight = Math.Min(UITheme.TimelineHeight,
                Math.Max(72, (GlobalParameters.screenHeight - navbarHeight) / 3));
            _navbar.Arrange(new Rectangle(0, 0, GlobalParameters.screenWidth, navbarHeight));
            _timeline.Arrange(new Rectangle(0, GlobalParameters.screenHeight - timelineHeight,
                GlobalParameters.screenWidth, timelineHeight));
            Rectangle stage = CanvasStageBounds(navbarHeight, timelineHeight);
            _canvasView.Arrange(stage, (int)animation.frameSize.X, (int)animation.frameSize.Y);
            int selectionWidth = Math.Max(1, Math.Min(500, GlobalParameters.screenWidth - 16));
            _selectionControls.Arrange(new Rectangle(GlobalParameters.screenWidth - selectionWidth - 8,
                _timeline.Bounds.Y - 70, selectionWidth, 62));
            _feedbackOverlay.Arrange(new Rectangle(0, 0,
                GlobalParameters.screenWidth, GlobalParameters.screenHeight));
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
            GlobalParameters.GlobalGraphics.Clear(UITheme.CanvasStage);
            int navbarHeight = _navbar.Bounds.Height;
            int timelineHeight = _timeline.Bounds.Height;
            Rectangle stage = CanvasStageBounds(navbarHeight, timelineHeight);
            _canvasView.Arrange(stage, (int)animation.frameSize.X, (int)animation.frameSize.Y);
            animation.DrawCurrentFrame(_canvasView);
            if (drawingTools.SelectedTool == DrawingTool.Selection) drawingTools.Selection.DrawOverlay();
            _timeline.Draw();
            _navbar.Draw();
            _selectionControls.Draw();
            MemoryMonitor.DrawMemoryOverlay(new Vector2(10, GlobalParameters.screenHeight - 30), UIConstants.DEBUG_MEMORY, animation);
            _feedbackOverlay.Draw();
        }

        private static Rectangle CanvasStageBounds(int navbarHeight, int timelineHeight)
        {
            int margin = Math.Min(24, Math.Max(4,
                Math.Min(GlobalParameters.screenWidth, GlobalParameters.screenHeight) / 30));
            return new Rectangle(margin, navbarHeight + margin,
                Math.Max(1, GlobalParameters.screenWidth - margin * 2),
                Math.Max(1, GlobalParameters.screenHeight - navbarHeight - timelineHeight - margin * 2));
        }

        private void HandleCanvasViewInput(Rectangle stage)
        {
            var mouse = GlobalParameters.GlobalMouse;
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
            if (!GlobalParameters.GlobalMouse.LeftClickHold() && !loadedScene) loadedScene = true;
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
            bool control = GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.LeftControl)
                || GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.RightControl);
            if (control && GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.Z) && animation.Undo())
                drawingTools.Selection.Clear();
            if (control && GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.Y) && animation.Redo())
                drawingTools.Selection.Clear();
            if (control && GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.D)) animation.DuplicateCurrentFrame();
            bool selectionActive = drawingTools.SelectedTool == DrawingTool.Selection && drawingTools.Selection.HasSelection;
            if (control && GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.C))
            {
                if (selectionActive) drawingTools.Selection.Copy(); else animation.CopyCurrentFrame();
            }
            if (control && GlobalParameters.GlobalKeyboard.OnKeyPress(Keys.V))
            {
                if (drawingTools.SelectedTool == DrawingTool.Selection && drawingTools.Selection.CanPaste)
                    drawingTools.Selection.Paste();
                else animation.PasteFrame();
            }
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC"))
            {
                animation.Stop();
                GlobalParameters.CurrentScene = GlobalParameters.Scenes[UIConstants.MENU_SCENE];
            }
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("DELETE"))
            {
                if (selectionActive) drawingTools.Selection.Delete(); else animation.DeleteFrame();
            }
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("P")) animation.TogglePlaying();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("BACKSPACE"))
            {
                animation.EraseCurrentLayer();
                drawingTools.Selection.Clear();
            }
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("O")) animation.isOnionSkinEnabled = !animation.isOnionSkinEnabled;
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("M")) animation.NextFrame();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("N")) animation.PreviousFrame();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("B")) animation.InsertFrame();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("L")) _navbar.SaveAnimation();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("[")) drawingTools.BrushSize--;
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("]")) drawingTools.BrushSize++;
        }
    }
}
