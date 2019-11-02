using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using FrameByFrame.src.Engine.Animation;

namespace FrameByFrame.src.Engine.Export
{
    public class SaveData
    {
        [XmlArray("Frame")]
        [XmlArrayItem("Layer")]
        public List<Frame> frames;
    }
}