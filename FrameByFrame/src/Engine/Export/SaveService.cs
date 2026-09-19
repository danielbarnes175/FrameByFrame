using System;
using System.Diagnostics;
using System.IO;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Services;
using ImageMagick;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.Engine.Export
{
    public enum ExportFormat
    {
        Gif,
        Mov,
        Mp4,
        PngSequence,
        SpriteSheet
    }

    public class SaveService
    {
        private const string ProjectsDirectory = "Projects";
        public static void SaveAnimation(Animation.Animation animation)
        {
            ArgumentNullException.ThrowIfNull(animation);

            Directory.CreateDirectory(ProjectsDirectory);
            string projectName = ValidateProjectName(animation.projectName);
            string filename = GetProjectPath($"{projectName}.fbf");
            FbfProjectFile.Save(filename, animation);
        }

        public static Animation.Animation LoadAnimation(string filename)
        {
            return FbfProjectFile.Load(filename);
        }

        public static string RenameAnimation(Animation.Animation animation, string currentFilename, string newName)
        {
            ArgumentNullException.ThrowIfNull(animation);
            ArgumentException.ThrowIfNullOrWhiteSpace(currentFilename);

            string normalizedName = ValidateProjectName(newName);

            Directory.CreateDirectory(ProjectsDirectory);
            string destination = GetProjectPath($"{normalizedName}.fbf");
            string currentFullPath = Path.GetFullPath(currentFilename);
            string destinationFullPath = Path.GetFullPath(destination);
            if (!string.Equals(currentFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase) && File.Exists(destination))
                throw new IOException($"A project named '{normalizedName}' already exists.");

            string oldName = animation.projectName;
            animation.projectName = normalizedName;
            try
            {
                FbfProjectFile.Save(destination, animation);
                if (!string.Equals(currentFullPath, destinationFullPath, StringComparison.OrdinalIgnoreCase))
                    File.Delete(currentFilename);
                return destination;
            }
            catch
            {
                animation.projectName = oldName;
                throw;
            }
        }

        public static void ExportAnimation(Animation.Animation animation)
        {
            ArgumentNullException.ThrowIfNull(animation);
            ExportAnimation(animation, 0, animation.TotalFrames - 1, ExportFormat.Gif);
        }

        public static void ExportAnimation(Animation.Animation animation, int startFrameIndex, int endFrameIndex)
        {
            ExportAnimation(animation, startFrameIndex, endFrameIndex, ExportFormat.Gif);
        }

        public static void ExportAnimation(Animation.Animation animation, int startFrameIndex, int endFrameIndex,
            ExportFormat format)
        {
            ExportAnimationTo(animation, startFrameIndex, endFrameIndex,
                Path.GetFullPath(ProjectsDirectory), format);
        }

        public static string ExportAnimationTo(Animation.Animation animation, int startFrameIndex, int endFrameIndex,
            string outputDirectory, ExportFormat format = ExportFormat.Gif)
        {
            ArgumentNullException.ThrowIfNull(animation);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

            if (animation.fps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(animation), "Animation FPS must be greater than zero.");
            }
            if (startFrameIndex < 0 || endFrameIndex < startFrameIndex || endFrameIndex >= animation.TotalFrames)
                throw new ArgumentOutOfRangeException(nameof(startFrameIndex), "Export frame range is invalid.");

            // The editable save is the source of truth. Always update it before
            // producing flattened PNG/GIF output.
            SaveAnimation(animation);

            string projectName = ValidateProjectName(animation.projectName);
            string exportRoot = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(exportRoot);
            string projectDirectory = Path.Combine(exportRoot, projectName);
            Directory.CreateDirectory(projectDirectory);

            int exportedFrameCount = endFrameIndex - startFrameIndex + 1;
            for (int sourceIndex = startFrameIndex; sourceIndex <= endFrameIndex; sourceIndex++)
            {
                int outputIndex = sourceIndex - startFrameIndex;
                using RenderTarget2D texture = DrawingService.CombineTextures(animation.GetFrameAtIndex(sourceIndex));
                string frameFilename = Path.Combine(projectDirectory, $"Frame_{outputIndex}.png");
                SaveTextureAsPng(frameFilename, texture);
            }

            RemoveObsoleteFrameFiles(projectDirectory, exportedFrameCount);
            return format switch
            {
                ExportFormat.Gif => CreateGif(animation, projectName, exportRoot, projectDirectory, exportedFrameCount),
                ExportFormat.Mov or ExportFormat.Mp4 =>
                    CreateVideo(animation, projectName, exportRoot, projectDirectory, format),
                ExportFormat.PngSequence => projectDirectory,
                ExportFormat.SpriteSheet =>
                    CreateSpriteSheet(animation, projectName, exportRoot, projectDirectory, exportedFrameCount),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported export format.")
            };
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

        private static string CreateGif(Animation.Animation animation, string projectName,
            string exportRoot, string projectDirectory, int frameCount)
        {
            string filename = Path.Combine(exportRoot, $"{projectName}.gif");
            uint frameDelay = (uint)Math.Max(1, Math.Round(100d / animation.fps));

            using MagickImageCollection collection = new MagickImageCollection();
            for (int i = 0; i < frameCount; i++)
            {
                string frameFilename = Path.Combine(projectDirectory, $"Frame_{i}.png");
                collection.Add(frameFilename);
                collection[i].AnimationDelay = frameDelay;
            }

            collection.Write(filename);
            return filename;
        }

        private static string CreateVideo(Animation.Animation animation, string projectName,
            string exportRoot, string projectDirectory, ExportFormat format)
        {
            string extension = format == ExportFormat.Mov ? ".mov" : ".mp4";
            string filename = Path.Combine(exportRoot, projectName + extension);
            var startInfo = new ProcessStartInfo
            {
                FileName = ResolveFfmpegPath(),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-y");
            startInfo.ArgumentList.Add("-framerate");
            startInfo.ArgumentList.Add(animation.fps.ToString(System.Globalization.CultureInfo.InvariantCulture));
            startInfo.ArgumentList.Add("-start_number");
            startInfo.ArgumentList.Add("0");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(Path.Combine(projectDirectory, "Frame_%d.png"));
            startInfo.ArgumentList.Add("-c:v");
            startInfo.ArgumentList.Add(format == ExportFormat.Mov ? "qtrle" : "libx264");
            if (format == ExportFormat.Mp4)
            {
                startInfo.ArgumentList.Add("-vf");
                startInfo.ArgumentList.Add("pad=ceil(iw/2)*2:ceil(ih/2)*2,format=yuv420p");
                startInfo.ArgumentList.Add("-movflags");
                startInfo.ArgumentList.Add("+faststart");
            }
            startInfo.ArgumentList.Add(filename);

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("FFmpeg could not be started.");
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"FFmpeg export failed: {LastNonEmptyLine(error)}");
            return filename;
        }

        private static string CreateSpriteSheet(Animation.Animation animation, string projectName,
            string exportRoot, string projectDirectory, int frameCount)
        {
            Frame firstFrame = animation.GetFrameAtIndex(0);
            int columns = (int)Math.Ceiling(Math.Sqrt(frameCount));
            int rows = (int)Math.Ceiling(frameCount / (double)columns);
            uint width = checked((uint)(firstFrame.width * columns));
            uint height = checked((uint)(firstFrame.height * rows));
            using var sheet = new MagickImage(MagickColors.Transparent, width, height);
            for (int i = 0; i < frameCount; i++)
            {
                using var frame = new MagickImage(Path.Combine(projectDirectory, $"Frame_{i}.png"));
                int x = i % columns * firstFrame.width;
                int y = i / columns * firstFrame.height;
                sheet.Composite(frame, x, y, CompositeOperator.Over);
            }
            string filename = Path.Combine(exportRoot, $"{projectName}_spritesheet.png");
            sheet.Write(filename);
            return filename;
        }

        private static string ResolveFfmpegPath()
        {
            string executable = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
            string bundledPath = Path.Combine(AppContext.BaseDirectory, executable);
            if (File.Exists(bundledPath)) return bundledPath;

            string developmentPath = Path.GetFullPath(executable);
            if (File.Exists(developmentPath)) return developmentPath;
            throw new FileNotFoundException(
                $"The bundled {executable} executable was not found. Build a release bundle or place it beside FrameByFrame.",
                bundledPath);
        }

        private static string LastNonEmptyLine(string value)
        {
            string[] lines = (value ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            return lines.Length == 0 ? "No diagnostic output was provided." : lines[^1];
        }

        internal static string ValidateProjectName(string name)
        {
            string normalizedName = name?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName) || normalizedName is "." or ".." ||
                Path.IsPathRooted(normalizedName) ||
                normalizedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                normalizedName.IndexOfAny(['<', '>', ':', '"', '/', '\\', '|', '?', '*']) >= 0)
                throw new ArgumentException("Project names must contain valid filename characters.", nameof(name));
            return normalizedName;
        }

        private static string GetProjectPath(string relativeName)
        {
            string projectsRoot = Path.GetFullPath(ProjectsDirectory);
            string candidate = Path.GetFullPath(Path.Combine(projectsRoot, relativeName));
            string rootPrefix = projectsRoot.EndsWith(Path.DirectorySeparatorChar)
                ? projectsRoot
                : projectsRoot + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The project path must remain inside the projects directory.");
            return candidate;
        }
    }
}
