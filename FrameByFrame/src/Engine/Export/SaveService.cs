using System;
using System.IO;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Services;
using ImageMagick;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Export
{
    public class SaveService
    {
        private const string ProjectsDirectory = "Projects";
        public static void SaveAnimation(Animation.Animation animation)
        {
            ArgumentNullException.ThrowIfNull(animation);

            Directory.CreateDirectory(ProjectsDirectory);
            string filename = Path.Combine(ProjectsDirectory, $"{animation.projectName}.fbf");
            FbfProjectFile.Save(filename, animation);
        }

        public static Animation.Animation LoadAnimation(string filename)
        {
            return FbfProjectFile.Load(filename);
        }

        public static void ExportAnimation(Animation.Animation animation)
        {
            ArgumentNullException.ThrowIfNull(animation);

            if (animation.fps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(animation), "Animation FPS must be greater than zero.");
            }

            // The editable save is the source of truth. Always update it before
            // producing flattened PNG/GIF output.
            SaveAnimation(animation);

            string projectDirectory = Path.Combine(ProjectsDirectory, animation.projectName);
            Directory.CreateDirectory(projectDirectory);

            for (int i = 0; i < animation.frames.Count; i++)
            {
                using RenderTarget2D texture = DrawingService.CombineTextures(animation.GetFrameAtIndex(i));
                string frameFilename = Path.Combine(projectDirectory, $"Frame_{i}.png");
                SaveTextureAsPng(frameFilename, texture);
            }

            RemoveObsoleteFrameFiles(projectDirectory, animation.frames.Count);
            CreateGif(animation, projectDirectory);
        }

        private static void RemoveObsoleteFrameFiles(string projectDirectory, int frameCount)
        {
            foreach (string filename in Directory.EnumerateFiles(projectDirectory, "Frame_*.png"))
            {
                string fileStem = Path.GetFileNameWithoutExtension(filename);
                string indexText = fileStem["Frame_".Length..];

                if (int.TryParse(indexText, out int frameIndex) && frameIndex >= frameCount)
                {
                    File.Delete(filename);
                }
            }
        }

        private static void SaveTextureAsPng(string filename, RenderTarget2D texture)
        {
            using FileStream setStream = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            texture.SaveAsPng(setStream, texture.Width, texture.Height);
        }

        private static void CreateGif(Animation.Animation animation, string projectDirectory)
        {
            string filename = Path.Combine(ProjectsDirectory, $"{animation.projectName}.gif");
            uint frameDelay = (uint)Math.Max(1, Math.Round(100d / animation.fps));

            using MagickImageCollection collection = new MagickImageCollection();
            for (int i = 0; i < animation.frames.Count; i++)
            {
                string frameFilename = Path.Combine(projectDirectory, $"Frame_{i}.png");
                collection.Add(frameFilename);
                collection[i].AnimationDelay = frameDelay;
            }

            collection.Write(filename);
        }
    }
}
