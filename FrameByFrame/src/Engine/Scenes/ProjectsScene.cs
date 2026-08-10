using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FrameByFrame.src.Engine.Scenes
{
    public class ProjectsScene : BaseScene
    {
        private readonly List<string> _projectFiles = new();
        private readonly List<Animation.Animation> _animations = new();
        private UIActionButton _back;
        private UIActionButton _previous;
        private UIActionButton _next;
        private UIActionButton _edit;
        private UIActionButton _export;
        private UIActionButton _rename;
        private UIActionButton _confirmRename;
        private UIActionButton _cancelRename;
        private UIActionButton _exportStartDown;
        private UIActionButton _exportStartUp;
        private UIActionButton _exportEndDown;
        private UIActionButton _exportEndUp;
        private UIActionButton _confirmExport;
        private UIActionButton _cancelExport;
        private UIActionButton _folder;
        private UIActionButton _create;
        private int _selected;
        private int _previewFrame;
        private double _previewTimer;
        private bool _isRenaming;
        private string _renameText = string.Empty;
        private string _renameError = string.Empty;
        private Rectangle _renameInputBounds;
        private bool _isSelectingExportRange;
        private int _exportStart;
        private int _exportEnd;

        public override void LoadContent()
        {
            Directory.CreateDirectory("Projects");
            _back = new UIActionButton("< Home", GoHome);
            _previous = new UIActionButton("<", () => SelectRelative(-1));
            _next = new UIActionButton(">", () => SelectRelative(1));
            _edit = new UIActionButton("Edit animation", OpenSelectedProject);
            _export = new UIActionButton("Export GIF", BeginExportRange);
            _rename = new UIActionButton("Rename project", BeginRename);
            _confirmRename = new UIActionButton("Save name", ConfirmRename);
            _cancelRename = new UIActionButton("Cancel", CancelRename);
            _exportStartDown = new UIActionButton("-", () => SetExportStart(_exportStart - 1));
            _exportStartUp = new UIActionButton("+", () => SetExportStart(_exportStart + 1));
            _exportEndDown = new UIActionButton("-", () => SetExportEnd(_exportEnd - 1));
            _exportEndUp = new UIActionButton("+", () => SetExportEnd(_exportEnd + 1));
            _confirmExport = new UIActionButton("Export range", ExportSelectedProject);
            _cancelExport = new UIActionButton("Cancel", () => _isSelectingExportRange = false);
            _folder = new UIActionButton("Open projects folder", OpenProjectFolder);
            _create = new UIActionButton("Create animation", CreateAnimation);
            LoadAnimations();
        }

        private void Layout()
        {
            int cx = GlobalParameters.screenWidth / 2;
            int cy = GlobalParameters.screenHeight / 2;
            int S(int value) => UILayoutEngine.Scale(value);
            _back.Bounds = new Rectangle(S(20), S(16), S(150), S(56));
            _folder.Bounds = new Rectangle(GlobalParameters.screenWidth - S(300), S(16), S(280), S(56));
            _previous.Bounds = new Rectangle(cx - S(270), cy - S(25), S(54), S(54));
            _next.Bounds = new Rectangle(cx + S(216), cy - S(25), S(54), S(54));
            _edit.Bounds = new Rectangle(cx - S(250), cy + S(225), S(240), S(60));
            _export.Bounds = new Rectangle(cx + S(10), cy + S(225), S(240), S(60));
            _rename.Bounds = new Rectangle(cx - S(120), cy + S(300), S(240), S(52));
            _renameInputBounds = new Rectangle(cx - S(220), cy - S(15), S(440), S(54));
            _confirmRename.Bounds = new Rectangle(cx - S(220), cy + S(60), S(210), S(52));
            _cancelRename.Bounds = new Rectangle(cx + S(10), cy + S(60), S(210), S(52));
            _create.Bounds = new Rectangle(cx - S(120), cy + S(30), S(240), S(48));
            _exportStartDown.Bounds = new Rectangle(cx - S(40), cy - S(70), S(46), S(42));
            _exportStartUp.Bounds = new Rectangle(cx + S(110), cy - S(70), S(46), S(42));
            _exportEndDown.Bounds = new Rectangle(cx - S(40), cy - S(15), S(46), S(42));
            _exportEndUp.Bounds = new Rectangle(cx + S(110), cy - S(15), S(46), S(42));
            _confirmExport.Bounds = new Rectangle(cx - S(220), cy + S(65), S(210), S(52));
            _cancelExport.Bounds = new Rectangle(cx + S(10), cy + S(65), S(210), S(52));
            bool hasProjects = _animations.Count > 0;
            _previous.IsEnabled = _next.IsEnabled = _edit.IsEnabled = _export.IsEnabled = _rename.IsEnabled = hasProjects;
        }

        public override void Update(GameTime gameTime)
        {
            Layout(); UIPointerRouter.BeginFrame();
            if (_isRenaming)
            {
                _confirmRename.Update(); _cancelRename.Update(); UpdateRenameText();
                return;
            }
            if (_isSelectingExportRange)
            {
                _exportStartDown.Update(); _exportStartUp.Update();
                _exportEndDown.Update(); _exportEndUp.Update();
                _confirmExport.Update(); _cancelExport.Update();
                if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC")) _isSelectingExportRange = false;
                return;
            }
            _back.Update(); _folder.Update();
            if (_animations.Count == 0) _create.Update();
            else { _previous.Update(); _next.Update(); _edit.Update(); _export.Update(); _rename.Update(); }
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC")) GoHome();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("ENTER")) OpenSelectedProject();
            if (_animations.Count > 0)
            {
                _previewTimer += gameTime.ElapsedGameTime.TotalSeconds;
                double frameDuration = 1d / Math.Max(1, _animations[_selected].fps);
                while (_previewTimer >= frameDuration)
                {
                    _previewTimer -= frameDuration;
                    _previewFrame = (_previewFrame + 1) % _animations[_selected].TotalFrames;
                }
            }
        }

        public override void Draw(Vector2 offset)
        {
            GlobalParameters.GlobalGraphics.Clear(UITheme.Background);
            int S(int value) => UILayoutEngine.Scale(value);
            UIRenderer.Fill(new Rectangle(0, 0, GlobalParameters.screenWidth, S(88)), UITheme.Surface);
            string heading = _animations.Count == 0 ? "Your animations - No saved projects yet" : $"Your animations - {_selected + 1} of {_animations.Count}";
            new UITextContainer
            {
                Bounds = new Rectangle(_back.Bounds.Right + S(16), S(10), Math.Max(1, _folder.Bounds.X - _back.Bounds.Right - S(32)), S(68)),
                HorizontalAlignment = UIAlign.Start,
                MaxLines = 2
            }.Draw(heading, UITheme.Text, 1f);
            _back.Draw(); _folder.Draw();

            if (_animations.Count == 0) DrawEmptyState(); else DrawSelectedProject();
            if (_isRenaming) DrawRenameDialog();
            if (_isSelectingExportRange) DrawExportRangeDialog();
        }

        private void DrawEmptyState()
        {
            int S(int value) => UILayoutEngine.Scale(value);
            Rectangle card = new(GlobalParameters.screenWidth / 2 - S(300), GlobalParameters.screenHeight / 2 - S(160), S(600), S(300));
            UIRenderer.Fill(card, UITheme.Surface); UIRenderer.Border(card, UITheme.Border, 2);
            new UITextContainer { Bounds = new Rectangle(card.X + S(24), card.Y + S(40), card.Width - S(48), S(64)), MaxLines = 2 }
                .Draw("Your first animation starts here", UITheme.Primary, 1.05f);
            new UITextContainer { Bounds = new Rectangle(card.X + S(32), card.Y + S(110), card.Width - S(64), S(72)), MaxLines = 2 }
                .Draw("Create a canvas, draw a frame, and save it to see it here.", UITheme.TextMuted, .9f);
            _create.Draw(true);
        }

        private void DrawSelectedProject()
        {
            int cx = GlobalParameters.screenWidth / 2;
            int cy = GlobalParameters.screenHeight / 2;
            int S(int value) => UILayoutEngine.Scale(value);
            Rectangle card = new(cx - S(210), cy - S(245), S(420), S(440));
            UIRenderer.Fill(card, UITheme.Surface); UIRenderer.Border(card, UITheme.Border, 2);
            Rectangle preview = new(cx - S(170), cy - S(205), S(340), S(260));
            _animations[_selected].GetFrameAtIndex(_previewFrame)?.DrawPreview(preview, 1f);
            UIRenderer.Border(preview, UITheme.Secondary, 2);
            string name = _animations[_selected].projectName;
            if (name.Length > 34) name = name[..31] + "...";
            new UITextContainer { Bounds = new Rectangle(card.X + S(20), cy + S(70), card.Width - S(40), S(64)), MaxLines = 2 }
                .Draw(name, UITheme.Text, 1f);
            new UITextContainer { Bounds = new Rectangle(card.X + S(20), cy + S(130), card.Width - S(40), S(46)), MaxLines = 1 }
                .Draw($"{_animations[_selected].TotalFrames} frames  |  {_animations[_selected].fps} fps", UITheme.TextMuted, .9f);
            _previous.Draw(); _next.Draw(); _edit.Draw(true); _export.Draw(); _rename.Draw();
        }

        private void BeginRename()
        {
            if (_animations.Count == 0) return;
            _renameText = _animations[_selected].projectName;
            _renameError = string.Empty;
            _isRenaming = true;
        }

        private void CancelRename() { _isRenaming = false; _renameError = string.Empty; }

        private void ConfirmRename()
        {
            try
            {
                _projectFiles[_selected] = SaveService.RenameAnimation(
                    _animations[_selected], _projectFiles[_selected], _renameText);
                _isRenaming = false;
                _renameError = string.Empty;
            }
            catch (Exception ex) { _renameError = ex.Message; }
        }

        private void UpdateRenameText()
        {
            bool shift = GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.LeftShift) ||
                GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.RightShift);
            foreach (Keys key in GlobalParameters.GlobalKeyboard.GetNewlyPressedKeys())
            {
                if (key == Keys.Enter) { ConfirmRename(); return; }
                if (key == Keys.Escape) { CancelRename(); return; }
                if (key == Keys.Back && _renameText.Length > 0) { _renameText = _renameText[..^1]; continue; }
                char? character = KeyToCharacter(key, shift);
                if (character.HasValue && _renameText.Length < 80) _renameText += character.Value;
            }
        }

        private static char? KeyToCharacter(Keys key, bool shift)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                char value = (char)('a' + (int)key - (int)Keys.A);
                return shift ? char.ToUpperInvariant(value) : value;
            }
            if (key >= Keys.D0 && key <= Keys.D9) return (char)('0' + (int)key - (int)Keys.D0);
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return (char)('0' + (int)key - (int)Keys.NumPad0);
            return key switch { Keys.Space => ' ', Keys.OemMinus => shift ? '_' : '-', _ => null };
        }

        private void DrawRenameDialog()
        {
            int S(int value) => UILayoutEngine.Scale(value);
            Rectangle dialog = new(GlobalParameters.screenWidth / 2 - S(260), GlobalParameters.screenHeight / 2 - S(120), S(520), S(270));
            UIRenderer.Fill(dialog, UITheme.SurfaceRaised); UIRenderer.Border(dialog, UITheme.Primary, 3);
            new UITextContainer { Bounds = new Rectangle(dialog.X + S(24), dialog.Y + S(18), dialog.Width - S(48), S(42)), MaxLines = 1 }
                .Draw("Rename project", UITheme.Primary, .9f);
            UIRenderer.Fill(_renameInputBounds, UITheme.Surface); UIRenderer.Border(_renameInputBounds, UITheme.Border, 2);
            string display = string.IsNullOrEmpty(_renameText) ? "Type a project name" : _renameText + "|";
            new UITextContainer { Bounds = _renameInputBounds, HorizontalAlignment = UIAlign.Start, Padding = S(12), MaxLines = 1 }
                .Draw(display, string.IsNullOrEmpty(_renameText) ? UITheme.TextMuted : UITheme.Text, .75f);
            if (!string.IsNullOrEmpty(_renameError))
                new UITextContainer { Bounds = new Rectangle(dialog.X + S(24), _renameInputBounds.Bottom + S(4), dialog.Width - S(48), S(34)), MaxLines = 1 }
                    .Draw(_renameError, Color.IndianRed, .55f);
            _confirmRename.Draw(true); _cancelRename.Draw();
        }

        public void LoadAnimations()
        {
            DisposeAnimations(); _projectFiles.Clear(); _selected = 0; _previewFrame = 0; _previewTimer = 0;
            foreach (string file in Directory.GetFiles("Projects", "*.fbf"))
            {
                try
                {
                    Animation.Animation animation = SaveService.LoadAnimation(file);
                    _projectFiles.Add(file);
                    _animations.Add(animation);
                }
                catch (Exception ex) { Debug.WriteLine($"Skipping invalid save '{file}': {ex.Message}"); }
            }
        }

        private void SelectRelative(int delta)
        {
            if (_animations.Count == 0) return;
            _selected = (_selected + delta + _animations.Count) % _animations.Count; _previewFrame = 0; _previewTimer = 0;
        }

        private void OpenSelectedProject()
        {
            if (_animations.Count == 0) return;
            try
            {
                Animation.Animation loaded = SaveService.LoadAnimation(_projectFiles[_selected]);
                DrawingScene drawing = (DrawingScene)GlobalParameters.Scenes[UIConstants.DRAWING_SCENE];
                drawing.LoadAnimation(loaded); GlobalParameters.CurrentScene = drawing;
            }
            catch (Exception ex) { Debug.WriteLine($"Unable to open project: {ex.Message}"); }
        }
        private void CreateAnimation()
        {
            MenuScene menu = (MenuScene)GlobalParameters.Scenes[UIConstants.MENU_SCENE];
            menu.BeginNewAnimationConfiguration();
            GlobalParameters.CurrentScene = menu;
        }

        private void BeginExportRange()
        {
            if (_animations.Count == 0) return;
            _exportStart = 0;
            _exportEnd = _animations[_selected].TotalFrames - 1;
            _isSelectingExportRange = true;
        }
        private void SetExportStart(int value) => _exportStart = Math.Clamp(value, 0, _exportEnd);
        private void SetExportEnd(int value) => _exportEnd = Math.Clamp(value, _exportStart, _animations[_selected].TotalFrames - 1);
        private void ExportSelectedProject()
        {
            if (_animations.Count == 0) return;
            SaveService.ExportAnimation(_animations[_selected], _exportStart, _exportEnd);
            _isSelectingExportRange = false;
        }
        private void DrawExportRangeDialog()
        {
            int S(int value) => UILayoutEngine.Scale(value);
            int cx = GlobalParameters.screenWidth / 2;
            int cy = GlobalParameters.screenHeight / 2;
            Rectangle dialog = new(cx - S(260), cy - S(145), S(520), S(300));
            UIRenderer.Fill(dialog, UITheme.SurfaceRaised); UIRenderer.Border(dialog, UITheme.Primary, 3);
            new UITextContainer { Bounds = new Rectangle(dialog.X + S(24), dialog.Y + S(18), dialog.Width - S(48), S(42)), MaxLines = 1 }
                .Draw("Export frame range", UITheme.Primary, .9f);
            new UITextContainer { Bounds = new Rectangle(cx - S(210), cy - S(70), S(150), S(42)), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("First frame", UITheme.Text, .7f);
            new UITextContainer { Bounds = new Rectangle(cx + S(10), cy - S(70), S(90), S(42)), MaxLines = 1 }
                .Draw((_exportStart + 1).ToString(), UITheme.Primary, .75f);
            _exportStartDown.Draw(); _exportStartUp.Draw();
            new UITextContainer { Bounds = new Rectangle(cx - S(210), cy - S(15), S(150), S(42)), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Last frame", UITheme.Text, .7f);
            new UITextContainer { Bounds = new Rectangle(cx + S(10), cy - S(15), S(90), S(42)), MaxLines = 1 }
                .Draw((_exportEnd + 1).ToString(), UITheme.Primary, .75f);
            _exportEndDown.Draw(); _exportEndUp.Draw();
            _confirmExport.Draw(true); _cancelExport.Draw();
        }
        private void OpenProjectFolder()
        {
            try { Process.Start(new ProcessStartInfo { FileName = Path.GetFullPath("Projects"), UseShellExecute = true, Verb = "open" }); }
            catch (Exception ex) { Debug.WriteLine(ex.Message); }
        }
        private void GoHome() { GlobalParameters.CurrentScene = GlobalParameters.Scenes[UIConstants.MENU_SCENE]; }
        private void DisposeAnimations() { foreach (Animation.Animation animation in _animations) animation.Dispose(); _animations.Clear(); }
        public override void Dispose() => DisposeAnimations();
    }
}
