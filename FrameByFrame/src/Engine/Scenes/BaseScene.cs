using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Scenes
{
    public class BaseScene : IDisposable
    {
        protected IApplicationContext App { get; }
        protected INavigationService Navigation { get; }

        protected BaseScene(IApplicationContext app, INavigationService navigation)
        {
            App = app ?? throw new ArgumentNullException(nameof(app));
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        }

        public virtual void LoadContent() { }

        public virtual void Update(GameTime gameTime) { }

        public virtual void Draw(Vector2 offset) { }

        public virtual void Dispose() { }
    }
}
