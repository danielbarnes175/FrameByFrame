using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using FrameByFrame.src.Engine.Animation;

namespace FrameByFrame.src.Engine.Export
{
    public class SaveService
    {

        public static void SaveAnimation(List<Frame> givenFrames)
        {
            SaveData saveData = new SaveData();
            saveData.frames = givenFrames;
            string filename = "test.fbfa";

            
        }
    }
}
