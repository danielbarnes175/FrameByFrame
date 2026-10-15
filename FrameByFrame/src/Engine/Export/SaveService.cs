using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using ImageMagick;
using Microsoft.Xna.Framework;

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
            private readonly object _stateLock = new();
            private readonly ExportSnapshot _snapshot;
            private readonly string _projectName;
            private readonly string _exportRoot;
            private readonly string _projectDirectory;
            private readonly ExportFormat _format;
            private readonly Task _worker;
            private bool _isComplete;
            private float _progress;
            private string _status = "Preparing export...";
            private string _outputPath;
            private Exception _failure;

            internal ExportOperation(ExportSnapshot snapshot, string projectName, string exportRoot,
                string projectDirectory, ExportFormat format)
            {
                _snapshot = snapshot;
                _projectName = projectName;
                _exportRoot = exportRoot;
                _projectDirectory = projectDirectory;
                _format = format;
                _outputPath = GetOutputPath(projectName, exportRoot, projectDirectory, format);
                _worker = Task.Factory.StartNew(ExportOnWorker,
                    System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }

            public bool IsComplete { get { lock (_stateLock) return _isComplete; } }
            public bool IsFailed { get { lock (_stateLock) return _failure != null; } }
            public float Progress { get { lock (_stateLock) return _progress; } }
            public string Status { get { lock (_stateLock) return _status; } }
            public string OutputPath { get { lock (_stateLock) return _outputPath; } }
            public Exception Failure { get { lock (_stateLock) return _failure; } }

            internal void WaitForCompletion() => _worker.GetAwaiter().GetResult();

            private void ExportOnWorker()
            {
                try
                {
                    for (int i = 0; i < _snapshot.Frames.Count; i++)
                    {
                        string frameFilename = Path.Combine(_projectDirectory, $"Frame_{i}.png");
                        SaveFrameAsPng(frameFilename, _snapshot, _snapshot.Frames[i]);
                        UpdateState(.9f * (i + 1) / _snapshot.Frames.Count,
                            $"Exporting frame {i + 1} of {_snapshot.Frames.Count}...");
                    }

                    UpdateState(.95f, _format switch
                    {
                        ExportFormat.Gif => "Creating GIF...",
                        ExportFormat.Mov => "Encoding MOV...",
                        ExportFormat.Mp4 => "Encoding MP4...",
                        ExportFormat.PngSequence => "Finishing PNG export...",
                        ExportFormat.SpriteSheet => "Creating spritesheet...",
                        _ => "Finishing export..."
                    });
                    RemoveObsoleteFrameFiles(_projectDirectory, _snapshot.Frames.Count);
                    string outputPath = _format switch
                    {
                        ExportFormat.Gif => CreateGif(_snapshot.FramesPerSecond, _projectName, _exportRoot,
                            _projectDirectory, _snapshot.Frames.Count),
                        ExportFormat.Mov or ExportFormat.Mp4 =>
                            CreateVideo(_snapshot, _projectName, _exportRoot, _projectDirectory, _format),
                        ExportFormat.PngSequence => _projectDirectory,
                        ExportFormat.SpriteSheet =>
                            CreateSpriteSheet(_snapshot.Width, _snapshot.Height, _projectName, _exportRoot,
                                _projectDirectory, _snapshot.Frames.Count),
                        _ => throw new ArgumentOutOfRangeException(nameof(_format), _format, "Unsupported export format.")
                    };
                    lock (_stateLock)
                    {
                        _outputPath = outputPath;
                        _progress = 1f;
                        _status = $"Export complete: {outputPath}";
                        _isComplete = true;
                    }
                }
                catch (Exception ex)
                {
                    lock (_stateLock)
                    {
                        _failure = ex;
                        _status = $"Export failed: {ex.Message}";
                    }
                }
            }

            private void UpdateState(float progress, string status)
            {
                lock (_stateLock)
                {
                    _progress = progress;
                    _status = status;
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

        internal sealed record ExportSnapshot(int Width, int Height, int FramesPerSecond,
            int StartFrameIndex, Color BackgroundColor, IReadOnlyList<ExportFrameSnapshot> Frames,
            IReadOnlyList<AudioTrack> AudioTracks);

        internal sealed record ExportFrameSnapshot(IReadOnlyList<ExportLayerSnapshot> Layers);

        internal sealed record ExportLayerSnapshot(IReadOnlyList<KeyValuePair<int, Color>> Pixels);

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
            operation.WaitForCompletion();
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
            ExportSnapshot snapshot = CaptureExportSnapshot(animation, startFrameIndex, endFrameIndex, format);
            return new ExportOperation(snapshot, projectName, exportRoot, projectDirectory, format);
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

        private static ExportSnapshot CaptureExportSnapshot(Animation.Animation animation, int startFrameIndex,
            int endFrameIndex, ExportFormat format)
        {
            Color background = animation.IsCanvasBackgroundTransparent
                ? FormatSupportsTransparency(format) ? Color.Transparent : Color.White
                : animation.CanvasBackgroundColor;
            Guid[] visibleLayerIds = animation.Layers.Where(layer => layer.IsVisible).Select(layer => layer.Id).ToArray();
            var frames = new List<ExportFrameSnapshot>(endFrameIndex - startFrameIndex + 1);
            for (int frameIndex = startFrameIndex; frameIndex <= endFrameIndex; frameIndex++)
            {
                Frame frame = animation.GetFrameAtIndex(frameIndex);
                var layers = new List<ExportLayerSnapshot>(visibleLayerIds.Length);
                foreach (Guid layerId in visibleLayerIds)
                    layers.Add(new ExportLayerSnapshot(frame.GetSparseLayerPixels(layerId).ToArray()));
                frames.Add(new ExportFrameSnapshot(layers));
            }

            IReadOnlyList<AudioTrack> audioTracks = format is ExportFormat.Mov or ExportFormat.Mp4
                ? animation.AudioTracks.Select(track => new AudioTrack(track.SourceName, track.SourceExtension,
                    track.EncodedData.ToArray(), track.StartFrame, track.DurationSeconds,
                    id: track.Id, volume: track.Volume, isMuted: track.IsMuted)).ToArray()
                : Array.Empty<AudioTrack>();
            Frame firstFrame = animation.GetFrameAtIndex(startFrameIndex);
            return new ExportSnapshot(firstFrame.width, firstFrame.height, animation.fps, startFrameIndex,
                background, frames, audioTracks);
        }

        private static void SaveFrameAsPng(string filename, ExportSnapshot snapshot, ExportFrameSnapshot frame)
        {
            byte[] pixels = ComposeFramePixels(snapshot, frame);
            var settings = new PixelReadSettings((uint)snapshot.Width, (uint)snapshot.Height,
                StorageType.Char, PixelMapping.RGBA);
            using var image = new MagickImage(MagickColors.Transparent, (uint)snapshot.Width, (uint)snapshot.Height);
            image.ReadPixels(pixels, settings);
            image.Format = MagickFormat.Png;
            image.Write(filename);
        }

        internal static byte[] ComposeFramePixels(ExportSnapshot snapshot, ExportFrameSnapshot frame)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            ArgumentNullException.ThrowIfNull(frame);

            byte[] pixels = new byte[checked(snapshot.Width * snapshot.Height * 4)];
            for (int index = 0; index < snapshot.Width * snapshot.Height; index++)
                WriteColor(pixels, index, snapshot.BackgroundColor);

            for (int layerIndex = frame.Layers.Count - 1; layerIndex >= 0; layerIndex--)
            {
                foreach (KeyValuePair<int, Color> pixel in frame.Layers[layerIndex].Pixels)
                {
                    if ((uint)pixel.Key >= (uint)(snapshot.Width * snapshot.Height)) continue;
                    BlendColor(pixels, pixel.Key, pixel.Value);
                }
            }
            return pixels;
        }

        private static void WriteColor(byte[] pixels, int pixelIndex, Color color)
        {
            int offset = pixelIndex * 4;
            pixels[offset] = color.R;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.B;
            pixels[offset + 3] = color.A;
        }

        private static void BlendColor(byte[] pixels, int pixelIndex, Color source)
        {
            if (source.A == 0) return;
            int offset = pixelIndex * 4;
            if (source.A == byte.MaxValue)
            {
                WriteColor(pixels, pixelIndex, source);
                return;
            }

            int inverseAlpha = byte.MaxValue - source.A;
            pixels[offset] = (byte)Math.Min(byte.MaxValue,
                source.R + (pixels[offset] * inverseAlpha + 127) / byte.MaxValue);
            pixels[offset + 1] = (byte)Math.Min(byte.MaxValue,
                source.G + (pixels[offset + 1] * inverseAlpha + 127) / byte.MaxValue);
            pixels[offset + 2] = (byte)Math.Min(byte.MaxValue,
                source.B + (pixels[offset + 2] * inverseAlpha + 127) / byte.MaxValue);
            pixels[offset + 3] = (byte)Math.Min(byte.MaxValue,
                source.A + (pixels[offset + 3] * inverseAlpha + 127) / byte.MaxValue);
        }

        private static string CreateGif(int framesPerSecond, string projectName,
            string exportRoot, string projectDirectory, int frameCount)
        {
            string filename = Path.Combine(exportRoot, $"{projectName}.gif");
            uint frameDelay = (uint)Math.Max(1, Math.Round(100d / framesPerSecond));

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

        private static string CreateVideo(ExportSnapshot snapshot, string projectName,
            string exportRoot, string projectDirectory, ExportFormat format)
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
                startInfo.ArgumentList.Add(snapshot.FramesPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture));
                startInfo.ArgumentList.Add("-start_number");
                startInfo.ArgumentList.Add("0");
                startInfo.ArgumentList.Add("-i");
                startInfo.ArgumentList.Add(Path.Combine(projectDirectory, "Frame_%d.png"));

                double rangeStart = snapshot.StartFrameIndex / (double)snapshot.FramesPerSecond;
                double duration = snapshot.Frames.Count / (double)snapshot.FramesPerSecond;
                double rangeEnd = rangeStart + duration;
                var audibleTracks = snapshot.AudioTracks.Where(track => !track.IsMuted && track.Volume > 0 &&
                    track.StartFrame / (double)snapshot.FramesPerSecond < rangeEnd &&
                    track.StartFrame / (double)snapshot.FramesPerSecond + track.DurationSeconds > rangeStart).ToList();
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
                    startInfo.ArgumentList.Add(BuildAudioFilter(snapshot.FramesPerSecond, audibleTracks,
                        rangeStart, rangeEnd, duration));
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
            => BuildAudioFilter(animation.fps, tracks, rangeStart, rangeEnd, duration);

        private static string BuildAudioFilter(int framesPerSecond,
            System.Collections.Generic.IReadOnlyList<AudioTrack> tracks,
            double rangeStart, double rangeEnd, double duration)
        {
            var filter = new StringBuilder();
            var labels = new List<string>(tracks.Count);
            for (int i = 0; i < tracks.Count; i++)
            {
                AudioTrack track = tracks[i];
                string label = $"track{i}";
                long delayMilliseconds = (long)Math.Round(track.StartFrame * 1000d / framesPerSecond);
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

        private static string CreateSpriteSheet(int frameWidth, int frameHeight, string projectName,
            string exportRoot, string projectDirectory, int frameCount)
        {
            int columns = (int)Math.Ceiling(Math.Sqrt(frameCount));
            int rows = (int)Math.Ceiling(frameCount / (double)columns);
            uint width = checked((uint)(frameWidth * columns));
            uint height = checked((uint)(frameHeight * rows));
            using var sheet = new MagickImage(MagickColors.Transparent, width, height);
            for (int i = 0; i < frameCount; i++)
            {
                using var frame = new MagickImage(Path.Combine(projectDirectory, $"Frame_{i}.png"));
                int x = i % columns * frameWidth;
                int y = i / columns * frameHeight;
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
