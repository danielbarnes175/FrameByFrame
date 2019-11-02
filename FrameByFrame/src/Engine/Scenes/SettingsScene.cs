using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Scenes
{
    public class SettingsScene : BaseScene
    {
        private List<BasicTexture> _textures;

        public SettingsScene()
        {
            _textures = new List<BasicTexture>();
        }

        public override void LoadContent()
        {

        }

        public override void Update()
        {
            
            base.Update();
        }

        public override void Draw(Vector2 offset)
        {
           foreach (BasicTexture texture in _textures)
           {
               texture.Draw(offset);
           }
           base.Draw(offset);
        }
    }
}
