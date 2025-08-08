using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Export
{
    public class SaveData
    {
        public string ProjectName { get; set; }
        public int Fps { get; set; }

        [XmlArray("Frames")]
        [XmlArrayItem("Frame")]
        public List<FrameData> Frames { get; set; }

        public SaveData()
        {
            Frames = new List<FrameData>();
        }

        public void Save(Animation.Animation animation)
        {
            ProjectName = animation.projectName;
            Fps = animation.fps;

            Frames.Clear();
            foreach (var frame in animation.frames)
            {
                Frames.Add(new FrameData(frame));
            }
        }
    }

    public class FrameData
    {
        public Vector2 FrameSize { get; set; }
        public Vector2 FramePosition { get; set; }
        public Color[] Layer1 { get; set; }
        public Color[] Layer2 { get; set; }
        public Color[] Layer3 { get; set; }

        public FrameData() { }

        public FrameData(Frame frame)
        {
            FrameSize = new Vector2(frame.width, frame.height);
            FramePosition = Frame.position;
            Layer1 = frame.GetLayerPixels("_layer1");
            Layer2 = frame.GetLayerPixels("_layer2");
            Layer3 = frame.GetLayerPixels("_layer3");
        }
    }
}