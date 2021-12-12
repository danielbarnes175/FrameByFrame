using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.UI;
using FrameByFrame.src.UI.Components.Buttons;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class MenuScene : BaseScene
    {
        private List<BasicTexture> _textures;
        private List<UIElement> _uiElements;

        private BasicTexture _logo;
        private bool cannotLoadProjects;

        public MenuScene()
        {
            _textures = new List<BasicTexture>();
            _uiElements = new List<UIElement>();
            cannotLoadProjects = false;
        }

        public override void LoadContent()
        {
            Vector2 projectsButtonLocation = new Vector2(GlobalParameters.screenWidth / 2 - 300, GlobalParameters.screenHeight / 2);
            Vector2 drawButtonLocation = new Vector2(GlobalParameters.screenWidth / 2 + 15, GlobalParameters.screenHeight / 2);
            _uiElements.Add(new RedirectButton("Projects Scene", "Static\\MenuScene/button_view-animations", projectsButtonLocation, new Vector2(280, 54), "YOOOOOOOOOOOOOOOOO"));
            _uiElements.Add(new RedirectButton("Drawing Scene", "Static\\MenuScene/button_new-animation", drawButtonLocation, new Vector2(285, 54)));
            _logo = new BasicTexture("Static\\MenuScene/logo", new Vector2(0, 0), new Vector2(400, 400));
        }

        public override void Update(GameTime gameTime)
        {
            foreach(UIElement element in _uiElements) 
            {
                element.Update();
            }

            if (GlobalParameters.GlobalKeyboard.GetPress("O"))
            {
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Drawing Scene"];
            }
            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            _logo.Draw(new Vector2(GlobalParameters.screenWidth / 2 - 200, GlobalParameters.screenHeight / 2 - 400), new Vector2(0, 0));

            foreach (UIElement element in _uiElements)
            {
                element.Draw(offset, new Vector2(0, 0));
            }

            foreach (BasicTexture texture in _textures)
            {
               texture.Draw(offset);
            }

            if (cannotLoadProjects)
            {
                GlobalParameters.GlobalSpriteBatch.DrawString(GlobalParameters.font, "No projects found", new Vector2(GlobalParameters.screenWidth / 2 - 250, GlobalParameters.screenHeight / 2 + 40), Color.Black);
            }
           base.Draw(offset);
        }
    }
}
