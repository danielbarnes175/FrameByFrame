using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Services;
using ImageMagick;
using Microsoft.Xna.Framework;
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

        public sealed class ExportOperation
        {
            private readonly Animation.Animation _animation;
            private readonly string _projectName;
            private readonly string _exportRoot;
            private readonly string _projectDirectory;
            private readonly ExportFormat _format;
            private readonly int _startFrameIndex;
            private readonly int _frameCount;
            private int _exportedFrameCount;
            private bool _isFinalizing;

            internal ExportOperation(Animation.Animation animation, string projectName, string exportRoot,
                string projectDirectory, ExportFormat format, int startFrameIndex, int frameCount)
            {
                _animation = animation;
                _projectName = projectName;
                _exportRoot = exportRoot;
                _projectDirectory = projectDirectory;
                _format = format;
                _startFrameIndex = startFrameIndex;
                _frameCount = frameCount;
                OutputPath = GetOutputPath(projectName, exportRoot, projectDirectory, format);
            }

            public bool IsComplete { get; private set; }
            public bool IsFailed => Failure != null;
            public float Progress { get; private set; }
            public string Status { get; private set; } = "Preparing export...";
            public string OutputPath { get; private set; }
            public Exception Failure { get; private set; }

            public void Step()
            {
                if (IsComplete || IsFailed) return;
                try
                {
                    if (_exportedFrameCount < _frameCount)
                    {
                        int sourceIndex = _startFrameIndex + _exportedFrameCount;
                        Color? backgroundOverride = _animation.IsCanvasBackgroundTransparent &&
                            !FormatSupportsTransparency(_format)
                                ? Color.White
                                : null;
                        using RenderTarget2D texture = DrawingService.CombineTextures(
                            _animation, _animation.GetFrameAtIndex(sourceIndex), backgroundOverride);
                        string frameFilename = Path.Combine(_projectDirectory, $"Frame_{_exportedFrameCount}.png");
                        SaveTextureAsPng(frameFilename, texture);
                        _exportedFrameCount++;
                        Progress = .9f * _exportedFrameCount / _frameCount;
                        Status = $"Exporting frame {_exportedFrameCount} of {_frameCount}...";
                        return;
                    }

                    if (!_isFinalizing)
                    {
                        _isFinalizing = true;
                        Progress = .95f;
                        Status = _format switch
                        {
                            ExportFormat.Gif => "Creating GIF...",
                            ExportFormat.Mov => "Encoding MOV...",
                            ExportFormat.Mp4 => "Encoding MP4...",
                            ExportFormat.PngSequence => "Finishing PNG export...",
                            ExportFormat.SpriteSheet => "Creating spritesheet...",
                            _ => "Finishing export..."
                        };
                        return;
                    }
                    RemoveObsoleteFrameFiles(_projectDirectory, _frameCount);
                    OutputPath = _format switch
                    {
                        ExportFormat.Gif => CreateGif(_animation, _projectName, _exportRoot, _projectDirectory, _frameCount),
                        ExportFormat.Mov or ExportFormat.Mp4 =>
                            CreateVideo(_animation, _projectName, _exportRoot, _projectDirectory, _format,
                                _startFrameIndex, _frameCount),
                        ExportFormat.PngSequence => _projectDirectory,
                        ExportFormat.SpriteSheet =>
                            CreateSpriteSheet(_animation, _projectName, _exportRoot, _projectDirectory, _frameCount),
                        _ => throw new ArgumentOutOfRangeException(nameof(_format), _format, "Unsupported export format.")
                    };
                    Progress = 1f;
                    Status = $"Export complete: {OutputPath}";
                    IsComplete = true;
                }
                catch (Exception ex)
                {
                    Failure = ex;
                    Status = $"Export failed: {ex.Message}";
                }
            }

            private static string GetOutputPath(string projectName, string exportRoot,
                string projectDirectory, ExportFormat format) => format switch
            {
                ExportFormat.Gif => Path.Combine(exportRoot, $"{projectName}.gif"),
                ExportFormat.Mov => Path.Combine(exportRoot, $"{projectName}.mov"),
                ExportFormat.Mp4 => Path.Combine(exportRoot, $"{projectName}.mp4"),
                ExportFormat.PngSequence => projectDirectory,
                ExportFormat.SpriteSheet => Path.Combine(exportRoot, $"{projectName}_spritesheet.png"),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported export format.")
            };
        }

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
            ExportOperation operation = BeginExportAnimation(animation, startFrameIndex, endFrameIndex,
                Path.GetFullPath(ProjectsDirectory), format);
            RunToCompletion(operation);
        }

        public static string ExportAnimationTo(Animation.Animation animation, int startFrameIndex, int endFrameIndex,
            string outputDirectory, ExportFormat format = ExportFormat.Gif)
        {
            ExportOperation operation = BeginExportAnimation(
                animation, startFrameIndex, endFrameIndex, outputDirectory, format);
            RunToCompletion(operation);
            return operation.OutputPath;
        }

        private static void RunToCompletion(ExportOperation operation)
        {
            while (!operation.IsComplete && !operation.IsFailed) operation.Step();
            if (operation.IsFailed)
                throw new InvalidOperationException(operation.Status, operation.Failure);
        }

        public static ExportOperation BeginExportAnimation(Animation.Animation animation, int startFrameIndex,
            int endFrameIndex)
        {
            return BeginExportAnimation(animation, startFrameIndex, endFrameIndex,
                Path.GetFullPath(ProjectsDirectory), ExportFormat.Gif);
        }

        public static ExportOperation BeginExportAnimation(Animation.Animation animation, int startFrameIndex,
            int endFrameIndex, string outputDirectory, ExportFormat format = ExportFormat.Gif)
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
            // producing flattened export output.
            SaveAnimation(animation);

            string projectName = ValidateProjectName(animation.projectName);
            string exportRoot = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(exportRoot);
            string projectDirectory = Path.Combine(exportRoot, projectName);
            Directory.CreateDirectory(projectDirectory);
            int exportedFrameCount = endFrameIndex - startFrameIndex + 1;
            return new ExportOperation(animation, projectName, exportRoot, projectDirectory,
                format, startFrameIndex, exportedFrameCount);
        }

        public static bool FormatSupportsTransparency(ExportFormat format) => format switch
        {
            ExportFormat.Gif or ExportFormat.PngSequence or ExportFormat.SpriteSheet => true,
            ExportFormat.Mov or ExportFormat.Mp4 => false,
            _ => false
        };

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
                collection[i].GifDisposeMethod = GifDisposeMethod.Background;
            }

            collection.Write(filename);
            return filename;
        }

        private static string CreateVideo(Animation.Animation animation, string projectName,
            string exportRoot, string projectDirectory, ExportFormat format, int startFrameIndex, int frameCount)
        {
            string extension = format == ExportFormat.Mov ? ".mov" : ".mp4";
            string filename = Path.Combine(exportRoot, projectName + extension);
            string audioDirectory = Path.Combine(Path.GetTempPath(), $"framebyframe-export-audio-{Guid.NewGuid():N}");
            try
            {
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

                double rangeStart = startFrameIndex / (double)animation.fps;
                double duration = frameCount / (double)animation.fps;
                double rangeEnd = rangeStart + duration;
                var audibleTracks = animation.AudioTracks.Where(track => !track.IsMuted && track.Volume > 0 &&
                    track.StartFrame / (double)animation.fps < rangeEnd &&
                    track.StartFrame / (double)animation.fps + track.DurationSeconds > rangeStart).ToList();
                if (audibleTracks.Count > 0)
                {
                    Directory.CreateDirectory(audioDirectory);
                    for (int i = 0; i < audibleTracks.Count; i++)
                    {
                        string input = Path.Combine(audioDirectory, $"track_{i}{audibleTracks[i].SourceExtension}");
                        File.WriteAllBytes(input, audibleTracks[i].EncodedData);
                        startInfo.ArgumentList.Add("-i");
                        startInfo.ArgumentList.Add(input);
                    }
                    startInfo.ArgumentList.Add("-filter_complex");
                    startInfo.ArgumentList.Add(BuildAudioFilter(animation, audibleTracks, rangeStart, rangeEnd, duration));
                    startInfo.ArgumentList.Add("-map");
                    startInfo.ArgumentList.Add("0:v:0");
                    startInfo.ArgumentList.Add("-map");
                    startInfo.ArgumentList.Add("[aout]");
                    startInfo.ArgumentList.Add("-c:a");
                    startInfo.ArgumentList.Add(format == ExportFormat.Mov ? "pcm_s16le" : "aac");
                }
                startInfo.ArgumentList.Add("-c:v");
                startInfo.ArgumentList.Add(format == ExportFormat.Mov ? "qtrle" : "libx264");
                if (format == ExportFormat.Mp4)
                {
                    startInfo.ArgumentList.Add("-vf");
                    startInfo.ArgumentList.Add("pad=ceil(iw/2)*2:ceil(ih/2)*2,format=yuv420p");
                    startInfo.ArgumentList.Add("-movflags");
                    startInfo.ArgumentList.Add("+faststart");
                }
                else
                {
                    // QTRLE can encode ARGB, but alpha handling varies between encoders and players.
                    // Full RGB frames keep MOV playback deterministic and avoid prior-frame carry-over.
                    startInfo.ArgumentList.Add("-pix_fmt");
                    startInfo.ArgumentList.Add("rgb24");
                }
                startInfo.ArgumentList.Add("-t");
                startInfo.ArgumentList.Add(duration.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add(filename);

                using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("FFmpeg could not be started.");
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"FFmpeg export failed: {LastNonEmptyLine(error)}");
                return filename;
            }
            finally
            {
                try { if (Directory.Exists(audioDirectory)) Directory.Delete(audioDirectory, recursive: true); }
                catch { }
            }
        }

        internal static string BuildAudioFilter(Animation.Animation animation,
            System.Collections.Generic.IReadOnlyList<AudioTrack> tracks,
            double rangeStart, double rangeEnd, double duration)
        {
            var filter = new StringBuilder();
            var labels = new List<string>(tracks.Count);
            for (int i = 0; i < tracks.Count; i++)
            {
                AudioTrack track = tracks[i];
                string label = $"track{i}";
                long delayMilliseconds = (long)Math.Round(track.StartFrame * 1000d / animation.fps);
                filter.Append('[').Append(i + 1).Append(":a]")
                    .Append("aresample=44100,aformat=sample_fmts=s16:channel_layouts=stereo,volume=")
                    .Append(track.Volume.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture))
                    .Append(",adelay=").Append(delayMilliseconds).Append(":all=1[").Append(label).Append("]; ");
                labels.Add($"[{label}]");
            }
            foreach (string label in labels) filter.Append(label);
            if (labels.Count > 1) filter.Append("amix=inputs=").Append(labels.Count).Append(":normalize=0,");
            filter.Append("atrim=start=")
                .Append(rangeStart.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture))
                .Append(":end=").Append(rangeEnd.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture))
                .Append(",asetpts=PTS-STARTPTS,apad=pad_dur=")
                .Append(duration.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture))
                .Append(",atrim=duration=")
                .Append(duration.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture))
                .Append("[aout]");
            return filter.ToString();
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

        internal static string ResolveFfmpegPath()
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
