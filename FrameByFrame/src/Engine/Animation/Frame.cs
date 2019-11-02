using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameByFrame.src.Engine.Animation
{
    public class Frame
    {
        public List<BasicTexture> _layer1;
        public List<BasicTexture> _layer2;
        public List<BasicTexture> _layer3;

        public Frame()
        {
            _layer1 = new List<BasicTexture>();
            _layer2 = new List<BasicTexture>();
            _layer3 = new List<BasicTexture>();
        }
    }
}
