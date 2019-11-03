using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class MenuScene : BaseScene
    {
        private List<BasicTexture> _textures;

        private BasicTexture _viewProjectsButtons;
        private BasicTexture _newProjectButton;
        private BasicTexture _logo;

        public MenuScene()
        {
            _textures = new List<BasicTexture>();
        }

        public override void LoadContent()
        {
            _viewProjectsButtons = new BasicTexture("Static\\MenuScene/button_view-animations", new Vector2(0, 0), new Vector2(280, 54));
            _newProjectButton = new BasicTexture("Static\\MenuScene/button_new-animation", new Vector2(0, 0), new Vector2(285, 54));
            _logo = new BasicTexture("Static\\MenuScene/logo", new Vector2(0, 0), new Vector2(400, 400));
        }

        public override void Update(GameTime gameTime)
        {
            if (GlobalParameters.GlobalMouse.LeftClickHold())
            {
                Vector2 clickPosition = GlobalParameters.GlobalMouse.newMousePos;
                if (clickPosition.X > GlobalParameters.screenWidth / 2 - 300 && clickPosition.X < GlobalParameters.screenWidth / 2 - 20
                                                                             && clickPosition.Y > GlobalParameters.screenHeight / 2 - 54
                                                                             && clickPosition.Y < GlobalParameters.screenHeight / 2)
                {
                    GlobalParameters.CurrentScene = GlobalParameters.Scenes["Projects Scene"];
                    ((ProjectsScene)GlobalParameters.Scenes["Projects Scene"]).LoadAnimations();
                }
                if (clickPosition.X > GlobalParameters.screenWidth / 2 + 15 && clickPosition.X < GlobalParameters.screenWidth / 2 + 300
                                                                             && clickPosition.Y > GlobalParameters.screenHeight / 2 - 54
                                                                             && clickPosition.Y < GlobalParameters.screenHeight / 2)
                {
                    GlobalParameters.CurrentScene = GlobalParameters.Scenes["Drawing Scene"];
                }
            }
            if (GlobalParameters.GlobalKeyboard.GetPress("O"))
            {
                GlobalParameters.CurrentScene = GlobalParameters.Scenes["Drawing Scene"];
            }
            base.Update(gameTime);
        }

        public override void Draw(Vector2 offset)
        {
            _viewProjectsButtons.Draw(new Vector2(GlobalParameters.screenWidth / 2 - 300, GlobalParameters.screenHeight / 2 - 54 / 2), new Vector2(0, 0));
            _newProjectButton.Draw(new Vector2(GlobalParameters.screenWidth / 2 + 15, GlobalParameters.screenHeight / 2 - 54 / 2), new Vector2(0, 0));
            _logo.Draw(new Vector2(GlobalParameters.screenWidth / 2 - 200, GlobalParameters.screenHeight / 2 - 400), new Vector2(0, 0));
            foreach (BasicTexture texture in _textures)
           {
               texture.Draw(offset);
           }
           base.Draw(offset);
        }
    }
}
