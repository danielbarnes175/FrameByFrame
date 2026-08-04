using System;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Scenes
{
    public class MenuScene : BaseScene
    {
        private UIActionButton _newButton;
        private UIActionButton _projectsButton;

        public override void LoadContent()
        {
            _newButton = new UIActionButton("Create new animation", OpenDrawing);
            _projectsButton = new UIActionButton("Open a project", OpenProjects);
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
        }

        public override void Update(GameTime gameTime)
        {
            Layout(); UIPointerRouter.BeginFrame(); _newButton.Update(); _projectsButton.Update();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("O")) OpenDrawing();
        }

        private void OpenDrawing()
        {
            DrawingScene drawing = (DrawingScene)GlobalParameters.Scenes[UIConstants.DRAWING_SCENE];
            drawing.BeginNewAnimation();
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
        }
    }
}
