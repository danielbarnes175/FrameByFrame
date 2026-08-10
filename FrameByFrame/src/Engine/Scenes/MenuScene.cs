using System;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Scenes
{
    public class MenuScene : BaseScene
    {
        private UIActionButton _newButton;
        private UIActionButton _projectsButton;
        private UIActionButton _widthDown;
        private UIActionButton _widthUp;
        private UIActionButton _heightDown;
        private UIActionButton _heightUp;
        private UIActionButton _createButton;
        private UIActionButton _cancelButton;
        private bool _isConfiguringNewAnimation;
        private Rectangle _cardBounds;
        private Rectangle _dialogBounds;
        private int _canvasWidth = 1200;
        private int _canvasHeight = 800;

        public override void LoadContent()
        {
            _newButton = new UIActionButton("Create new animation", BeginNewAnimationConfiguration);
            _projectsButton = new UIActionButton("Open a project", OpenProjects);
            _widthDown = new UIActionButton("-", () => ChangeWidth(-64));
            _widthUp = new UIActionButton("+", () => ChangeWidth(64));
            _heightDown = new UIActionButton("-", () => ChangeHeight(-64));
            _heightUp = new UIActionButton("+", () => ChangeHeight(64));
            _createButton = new UIActionButton("Create animation", CreateAnimation);
            _cancelButton = new UIActionButton("Cancel", () => _isConfiguringNewAnimation = false);
        }

        private void Layout()
        {
            int S(int value) => UILayoutEngine.Scale(value);
            bool narrow = GlobalParameters.screenWidth < 600;
            _cardBounds = UILayoutEngine.CenteredInScreen(S(820), Math.Min(S(440), GlobalParameters.screenHeight - S(24)), S(16));
            int gap = S(12);
            int buttonHeight = S(58);
            int actionBottom = _cardBounds.Bottom - S(28);
            if (narrow)
            {
                int width = _cardBounds.Width - S(40);
                _projectsButton.Bounds = new Rectangle(_cardBounds.X + S(20), actionBottom - buttonHeight, width, buttonHeight);
                _newButton.Bounds = new Rectangle(_cardBounds.X + S(20), _projectsButton.Bounds.Y - gap - buttonHeight, width, buttonHeight);
            }
            else
            {
                int width = (_cardBounds.Width - S(116)) / 2;
                _newButton.Bounds = new Rectangle(_cardBounds.X + S(50), actionBottom - buttonHeight, width, buttonHeight);
                _projectsButton.Bounds = new Rectangle(_newButton.Bounds.Right + S(16), _newButton.Bounds.Y, width, buttonHeight);
            }

            _dialogBounds = UILayoutEngine.CenteredInScreen(S(560), S(narrow ? 430 : 360), S(12));
            int controlWidth = Math.Max(36, S(48));
            int valueWidth = Math.Max(72, Math.Min(S(100), _dialogBounds.Width - controlWidth * 2 - S(56)));
            int valueX = _dialogBounds.Right - S(28) - controlWidth * 2 - valueWidth;
            int firstRowY = _dialogBounds.Y + S(100);
            int secondRowY = firstRowY + S(60);
            _widthDown.Bounds = new Rectangle(valueX, firstRowY, controlWidth, S(44));
            _widthUp.Bounds = new Rectangle(valueX + controlWidth + valueWidth, firstRowY, controlWidth, S(44));
            _heightDown.Bounds = new Rectangle(valueX, secondRowY, controlWidth, S(44));
            _heightUp.Bounds = new Rectangle(valueX + controlWidth + valueWidth, secondRowY, controlWidth, S(44));
            int actionY = _dialogBounds.Bottom - S(narrow ? 124 : 90);
            int actionWidth = narrow ? _dialogBounds.Width - S(40) : (_dialogBounds.Width - S(86)) / 2;
            _createButton.Bounds = new Rectangle(_dialogBounds.X + S(20), actionY, actionWidth, S(52));
            _cancelButton.Bounds = narrow
                ? new Rectangle(_dialogBounds.X + S(20), _createButton.Bounds.Bottom + S(8), actionWidth, S(52))
                : new Rectangle(_createButton.Bounds.Right + S(16), actionY, actionWidth, S(52));
        }

        public override void Update(GameTime gameTime)
        {
            Layout(); UIPointerRouter.BeginFrame();
            if (_isConfiguringNewAnimation)
            {
                _widthDown.Update(); _widthUp.Update(); _heightDown.Update(); _heightUp.Update();
                _createButton.Update(); _cancelButton.Update();
                if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC")) _isConfiguringNewAnimation = false;
                return;
            }
            _newButton.Update(); _projectsButton.Update();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("O")) BeginNewAnimationConfiguration();
        }

        public void BeginNewAnimationConfiguration()
        {
            _canvasWidth = 1200;
            _canvasHeight = 800;
            _isConfiguringNewAnimation = true;
        }

        private void ChangeWidth(int delta) => _canvasWidth = Math.Clamp(
            _canvasWidth + delta, Animation.Animation.MinCanvasDimension, Animation.Animation.MaxCanvasDimension);

        private void ChangeHeight(int delta) => _canvasHeight = Math.Clamp(
            _canvasHeight + delta, Animation.Animation.MinCanvasDimension, Animation.Animation.MaxCanvasDimension);

        private void CreateAnimation()
        {
            DrawingScene drawing = (DrawingScene)GlobalParameters.Scenes[UIConstants.DRAWING_SCENE];
            drawing.BeginNewAnimation(_canvasWidth, _canvasHeight);
            _isConfiguringNewAnimation = false;
            GlobalParameters.CurrentScene = drawing;
        }

        private void OpenProjects()
        {
            ProjectsScene projects = (ProjectsScene)GlobalParameters.Scenes[UIConstants.PROJECTS_SCENE];
            projects.LoadAnimations();
            GlobalParameters.CurrentScene = projects;
        }

        public override void Draw(Vector2 offset)
        {
            GlobalParameters.GlobalGraphics.Clear(UITheme.Background);
            Rectangle glow = _cardBounds;
            UIRenderer.Fill(glow, UITheme.Surface);
            UIRenderer.Border(glow, UITheme.Border, 2);
            new UITextContainer { Bounds = new Rectangle(glow.X + UILayoutEngine.Scale(24), glow.Y + UILayoutEngine.Scale(40), glow.Width - UILayoutEngine.Scale(48), UILayoutEngine.Scale(80)), MaxLines = 1 }
                .Draw("FRAME BY FRAME", UITheme.Primary, 1.55f);
            new UITextContainer { Bounds = new Rectangle(glow.X + UILayoutEngine.Scale(40), glow.Y + UILayoutEngine.Scale(125), glow.Width - UILayoutEngine.Scale(80), UILayoutEngine.Scale(58)), MaxLines = 2 }
                .Draw("Bring your ideas to life", UITheme.Text, 1f);
            _newButton.Draw(true); _projectsButton.Draw();
            if (_isConfiguringNewAnimation) DrawNewAnimationDialog();
        }

        private void DrawNewAnimationDialog()
        {
            int S(int value) => UILayoutEngine.Scale(value);
            UIRenderer.Fill(new Rectangle(0, 0, GlobalParameters.screenWidth, GlobalParameters.screenHeight), new Color(0, 0, 0, 170));
            Rectangle dialog = _dialogBounds;
            UIRenderer.Fill(dialog, UITheme.SurfaceRaised);
            UIRenderer.Border(dialog, UITheme.Primary, 3);
            new UITextContainer { Bounds = new Rectangle(dialog.X + S(28), dialog.Y + S(20), dialog.Width - S(56), S(48)), MaxLines = 1 }
                .Draw("New animation", UITheme.Primary, 1f);
            new UITextContainer { Bounds = new Rectangle(dialog.X + S(28), dialog.Y + S(94), S(190), S(48)), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Canvas width", UITheme.Text, .75f);
            new UITextContainer { Bounds = new Rectangle(_widthDown.Bounds.Right, _widthDown.Bounds.Y, S(100), _widthDown.Bounds.Height), MaxLines = 1 }
                .Draw(_canvasWidth.ToString(), UITheme.Text, .72f);
            new UITextContainer { Bounds = new Rectangle(dialog.X + S(28), dialog.Y + S(154), S(190), S(48)), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Canvas height", UITheme.Text, .75f);
            new UITextContainer { Bounds = new Rectangle(_heightDown.Bounds.Right, _heightDown.Bounds.Y, S(100), _heightDown.Bounds.Height), MaxLines = 1 }
                .Draw(_canvasHeight.ToString(), UITheme.Text, .72f);
            _widthDown.Draw(); _widthUp.Draw(); _heightDown.Draw(); _heightUp.Draw();
            _createButton.Draw(true); _cancelButton.Draw();
        }
    }
}
