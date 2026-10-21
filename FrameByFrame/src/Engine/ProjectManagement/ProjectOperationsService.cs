using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;

namespace FrameByFrame.src.Engine.Projects
{
    internal sealed record ProjectRenameResult(string FilePath, FbfProjectFile.ProjectMetadata Metadata);

    internal sealed class ProjectOperationsService
    {
        private readonly Func<string, FbfProjectFile.ProjectMetadata> _readMetadata;
        private readonly Func<string, Animation.Animation> _load;
        private readonly Func<Animation.Animation, string, string, string> _rename;

        public string ProjectsDirectory { get; }

        public ProjectOperationsService(string? projectsDirectory = null)
            : this(projectsDirectory ?? SaveService.ProjectsDirectory,
                SaveService.ReadProjectMetadata, SaveService.LoadAnimation, SaveService.RenameAnimation) { }

        internal ProjectOperationsService(string projectsDirectory,
            Func<string, FbfProjectFile.ProjectMetadata> readMetadata,
            Func<string, Animation.Animation> load,
            Func<Animation.Animation, string, string, string> rename)
        {
            ProjectsDirectory = Path.GetFullPath(projectsDirectory);
            _readMetadata = readMetadata ?? throw new ArgumentNullException(nameof(readMetadata));
            _load = load ?? throw new ArgumentNullException(nameof(load));
            _rename = rename ?? throw new ArgumentNullException(nameof(rename));
        }

        public void EnsureProjectsDirectory() => Directory.CreateDirectory(ProjectsDirectory);

        public IReadOnlyList<string> ListProjectFiles()
        {
            EnsureProjectsDirectory();
            return Directory.GetFiles(ProjectsDirectory, "*.fbf");
        }

        public FbfProjectFile.ProjectMetadata ReadMetadata(string filePath) => _readMetadata(filePath);
        public FbfProjectFile.ProjectThumbnail LoadThumbnail(string filePath) =>
            SaveService.LoadProjectThumbnail(filePath);
        public Animation.Animation Load(string filePath) => _load(filePath);

        public ProjectRenameResult Rename(string filePath, string projectName)
        {
            using Animation.Animation animation = _load(filePath);
            string renamedPath = _rename(animation, filePath, projectName);
            return new ProjectRenameResult(renamedPath, _readMetadata(renamedPath));
        }

        public SaveService.ExportOperation BeginExport(string filePath, int startFrame, int endFrame,
            string directory, ExportFormat format, bool replaceExisting)
        {
            using Animation.Animation animation = _load(filePath);
            return SaveService.BeginExportAnimation(animation, startFrame, endFrame, directory, format,
                replaceExisting ? ExportCollisionBehavior.Replace : ExportCollisionBehavior.Fail);
        }

        public void OpenProjectsFolder() => Process.Start(new ProcessStartInfo
        {
            FileName = ProjectsDirectory,
            UseShellExecute = true,
            Verb = "open"
        });
    }
}
