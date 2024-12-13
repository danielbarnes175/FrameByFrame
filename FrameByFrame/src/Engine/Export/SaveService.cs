using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Services;
using ImageMagick;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Export
{
    public class SaveService
    {

        public static void SaveAnimation(Animation.Animation animation)
        {
            SaveData saveData = new SaveData();
            saveData.Save(animation);

            string directory = "Projects";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filename = $"{directory}/{animation.projectName}_saveData.json";

            // Serialize to JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonData = JsonSerializer.Serialize(saveData, options);

            // Write to file
            File.WriteAllText(filename, jsonData);
        }

        public static void ExportAnimation(Animation.Animation animation)
        {
            for (int i = 0; i < animation.frames.Count; i++)
            {
                RenderTarget2D texture = DrawingService.CombineTextures(animation.GetFrameAtIndex(i));
                System.IO.Directory.CreateDirectory("Projects/" + animation.projectName);
                SaveTextureAsPng("Projects/" + animation.projectName + "/Frame_" + i + ".png", texture);
            }
            CreateGif(animation);
        }

        private static void SaveTextureAsPng(string filename, RenderTarget2D texture)
        {
            FileStream setStream = File.Open(filename, FileMode.Create);
            StreamWriter writer = new StreamWriter(setStream);
            texture.SaveAsPng(setStream, texture.Width, texture.Height);
            setStream.Dispose();
        }

        private static void CreateGif(Animation.Animation animation)
        {
            string filename = "Projects/" + animation.projectName + ".gif";
            float frameDelay = 60 / animation.fps;

            using (MagickImageCollection collection = new MagickImageCollection())
            {
                for (int i = 0; i < animation.frames.Count; i++)
                {
                    collection.Add("Projects/" + animation.projectName + "/Frame_" + i + ".png");
                    collection[0].AnimationDelay = (uint)(frameDelay * 100);
                }
                collection.Write(filename);
            }
        }
    }
}
