using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.Engine.Projects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Xunit;

public sealed class ProjectSceneComponentTests
{
    [Fact]
    public void GalleryKeepsSelectionAndPaginationTogether()
    {
        ProjectGalleryState gallery = new();
        for (int i = 0; i < 8; i++)
            gallery.Add(new GalleryProject($"{i}.fbf", Metadata($"Project {i}")));

        gallery.Select(7);
        gallery.NormalizePage(6);

        Assert.Equal(7, gallery.SelectedIndex);
        Assert.Equal(6, gallery.PageStart);
        Assert.True(gallery.ChangePage(-1, 6));
        Assert.Equal(0, gallery.PageStart);
        Assert.Equal(-1, gallery.SelectedIndex);
    }

    [Fact]
    public void RenameDialogOwnsTextAndFailureState()
    {
        ProjectRenameDialogState dialog = new();
        dialog.Open("Demo");

        RenameInputResult input = dialog.ApplyKeys([Keys.Back, Keys.A, Keys.Enter], shift: false);
        bool renamed = dialog.Confirm(_ => "name collision");

        Assert.Equal(RenameInputResult.Confirm, input);
        Assert.Equal("Dema", dialog.Text);
        Assert.False(renamed);
        Assert.Contains("name collision", dialog.Error);
        Assert.True(dialog.IsOpen);
    }

    [Fact]
    public void RenameDialogRepeatsBackspaceWhileHeld()
    {
        ProjectRenameDialogState dialog = new();
        dialog.Open("Project");

        dialog.ApplyKeys([Keys.Back], shift: false, backspaceHeld: true,
            TimeSpan.FromMilliseconds(16));
        dialog.ApplyKeys([], shift: false, backspaceHeld: true,
            TimeSpan.FromMilliseconds(384));
        dialog.ApplyKeys([], shift: false, backspaceHeld: true,
            TimeSpan.FromMilliseconds(50));

        Assert.Equal("Proj", dialog.Text);
    }

    [Fact]
    public void SelectedProjectPreviewStartsAndAdvancesAutomatically()
    {
        ProjectPreviewState preview = new();
        FrameByFrame.src.Engine.Animation.Animation animation = new("Preview");
        animation.InitializeFrames(64, 64);
        animation.NextFrame();
        animation.SelectFrame(0);

        preview.Start(animation);
        preview.Update(new GameTime(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100)));

        Assert.True(animation.IsPlaying);
        Assert.True(preview.HasAudioPlayback);
        Assert.Equal(1, animation.CurrentFrameIndex);
        preview.Dispose();
    }

    [Fact]
    public void ExportDialogClampsRangeIndependentlyOfScene()
    {
        ProjectExportDialogState dialog = new();
        dialog.Open(10);

        dialog.SetStart(7);
        dialog.SetEnd(3);
        dialog.ToggleReplace();

        Assert.Equal(7, dialog.StartFrame);
        Assert.Equal(7, dialog.EndFrame);
        Assert.True(dialog.ReplaceExisting);
    }

    [Fact]
    public void ProjectOperationsListsOnlyProjectFilesWithoutGraphics()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"fbf-project-operations-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "one.fbf"), "test");
            File.WriteAllText(Path.Combine(directory, "ignore.txt"), "test");
            ProjectOperationsService operations = new(directory);

            IReadOnlyList<string> files = operations.ListProjectFiles();

            Assert.Single(files);
            Assert.EndsWith("one.fbf", files[0]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LegacyMigrationDoesNotRestoreAProjectRemovedAfterMigration()
    {
        string root = Path.Combine(Path.GetTempPath(), $"fbf-app-data-{Guid.NewGuid():N}");
        string legacyRoot = Path.Combine(Path.GetTempPath(), $"fbf-legacy-data-{Guid.NewGuid():N}");
        string legacyProjects = Path.Combine(legacyRoot, "Projects");
        Directory.CreateDirectory(legacyProjects);
        File.WriteAllBytes(Path.Combine(legacyProjects, "Old name.fbf"), [1, 2, 3]);

        try
        {
            AppDataPaths firstRun = new(root, [legacyRoot]);
            string migratedProject = Path.Combine(firstRun.ProjectsDirectory, "Old name.fbf");
            Assert.True(File.Exists(migratedProject));
            File.Delete(migratedProject);

            AppDataPaths restarted = new(root, [legacyRoot]);

            Assert.False(File.Exists(Path.Combine(restarted.ProjectsDirectory, "Old name.fbf")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            if (Directory.Exists(legacyRoot)) Directory.Delete(legacyRoot, recursive: true);
        }
    }

    private static FbfProjectFile.ProjectMetadata Metadata(string name) =>
        new(name, 64, 64, 12, 1, 0, false, Microsoft.Xna.Framework.Color.White);
}
