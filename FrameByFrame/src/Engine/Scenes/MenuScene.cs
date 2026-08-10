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
            int centerX = GlobalParameters.screenWidth / 2;
            int cardWidth = Math.Min(UILayoutEngine.Scale(820), GlobalParameters.screenWidth - UILayoutEngine.Scale(80));
            int cardX = centerX - cardWidth / 2;
            int cardY = GlobalParameters.screenHeight / 2 - UILayoutEngine.Scale(250);
            int buttonWidth = (cardWidth - UILayoutEngine.Scale(116)) / 2;
            int y = cardY + UILayoutEngine.Scale(342);
            _newButton.Bounds = new Rectangle(cardX + UILayoutEngine.Scale(50), y, buttonWidth, UILayoutEngine.Scale(64));
            _projectsButton.Bounds = new Rectangle(cardX + UILayoutEngine.Scale(66) + buttonWidth, y, buttonWidth, UILayoutEngine.Scale(64));

            int dialogWidth = Math.Min(UILayoutEngine.Scale(560), GlobalParameters.screenWidth - UILayoutEngine.Scale(48));
            int dialogX = centerX - dialogWidth / 2;
            int dialogY = GlobalParameters.screenHeight / 2 - UILayoutEngine.Scale(190);
            int valueX = dialogX + UILayoutEngine.Scale(245);
            int controlWidth = UILayoutEngine.Scale(48);
            int valueWidth = UILayoutEngine.Scale(100);
            int firstRowY = dialogY + UILayoutEngine.Scale(100);
            int secondRowY = dialogY + UILayoutEngine.Scale(160);
            _widthDown.Bounds = new Rectangle(valueX, firstRowY, controlWidth, UILayoutEngine.Scale(44));
            _widthUp.Bounds = new Rectangle(valueX + controlWidth + valueWidth, firstRowY, controlWidth, UILayoutEngine.Scale(44));
            _heightDown.Bounds = new Rectangle(valueX, secondRowY, controlWidth, UILayoutEngine.Scale(44));
            _heightUp.Bounds = new Rectangle(valueX + controlWidth + valueWidth, secondRowY, controlWidth, UILayoutEngine.Scale(44));
            int actionY = dialogY + UILayoutEngine.Scale(270);
            _createButton.Bounds = new Rectangle(dialogX + UILayoutEngine.Scale(35), actionY, UILayoutEngine.Scale(235), UILayoutEngine.Scale(54));
            _cancelButton.Bounds = new Rectangle(dialogX + dialogWidth - UILayoutEngine.Scale(235) - UILayoutEngine.Scale(35), actionY,
                UILayoutEngine.Scale(235), UILayoutEngine.Scale(54));
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
            int centerX = GlobalParameters.screenWidth / 2;
            int cardWidth = Math.Min(UILayoutEngine.Scale(820), GlobalParameters.screenWidth - UILayoutEngine.Scale(80));
            Rectangle glow = new(centerX - cardWidth / 2, GlobalParameters.screenHeight / 2 - UILayoutEngine.Scale(250), cardWidth, UILayoutEngine.Scale(440));
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
            int width = Math.Min(S(560), GlobalParameters.screenWidth - S(48));
            Rectangle dialog = new(GlobalParameters.screenWidth / 2 - width / 2,
                GlobalParameters.screenHeight / 2 - S(190), width, S(360));
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
