using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;

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
        private UIActionButton _folder;
        private int _selected;
        private int _previewFrame;
        private double _previewTimer;

        public override void LoadContent()
        {
            Directory.CreateDirectory("Projects");
            _back = new UIActionButton("< Home", GoHome);
            _previous = new UIActionButton("<", () => SelectRelative(-1));
            _next = new UIActionButton(">", () => SelectRelative(1));
            _edit = new UIActionButton("Edit animation", OpenSelectedProject);
            _export = new UIActionButton("Export GIF", ExportSelectedProject);
            _folder = new UIActionButton("Open projects folder", OpenProjectFolder);
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
            bool hasProjects = _animations.Count > 0;
            _previous.IsEnabled = _next.IsEnabled = _edit.IsEnabled = _export.IsEnabled = hasProjects;
        }

        public override void Update(GameTime gameTime)
        {
            Layout(); UIPointerRouter.BeginFrame();
            _back.Update(); _folder.Update(); _previous.Update(); _next.Update(); _edit.Update(); _export.Update();
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
            Rectangle create = new(card.Center.X - S(120), card.Y + S(190), S(240), S(48));
            bool hover = create.Contains(GlobalParameters.GlobalMouse.newMousePos);
            UIRenderer.Fill(create, hover ? UITheme.PrimaryHover : UITheme.Primary);
            new UITextContainer { Bounds = create, Padding = 10, MaxLines = 2 }.Draw("Create animation", UITheme.Text, .9f);
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
            _previous.Draw(); _next.Draw(); _edit.Draw(true); _export.Draw();
        }

        public void LoadAnimations()
        {
            DisposeAnimations(); _projectFiles.Clear(); _selected = 0; _previewFrame = 0; _previewTimer = 0;
            foreach (string file in Directory.GetFiles("Projects", "*.fbf"))
            {
                try { _projectFiles.Add(file); _animations.Add(SaveService.LoadAnimation(file)); }
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

        private void ExportSelectedProject() { if (_animations.Count > 0) SaveService.ExportAnimation(_animations[_selected]); }
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
