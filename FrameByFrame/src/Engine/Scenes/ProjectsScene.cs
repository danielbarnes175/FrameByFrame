using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.Engine.Projects;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogNET;

namespace FrameByFrame.src.Engine.Scenes
{
    public class ProjectsScene : BaseScene
    {
        private sealed record ThumbnailLoadResult(int Generation, string FilePath,
            FbfProjectFile.ProjectThumbnail? Thumbnail, Exception? Error);
        private sealed record MetadataLoadResult(int Generation, string FilePath,
            FbfProjectFile.ProjectMetadata? Metadata, Exception? Error);

        private const int MaximumCachedThumbnails = 8;
        private readonly ProjectGalleryState _gallery = new();
        private readonly ProjectGalleryComponent _galleryView;
        private readonly ProjectRenameDialogState _renameDialog = new();
        private readonly ProjectExportDialogState _exportDialog = new();
        private readonly ProjectOperationsService _operations = new();
        private UIActionButton _back = null!;
        private UIActionButton _previous = null!;
        private UIActionButton _next = null!;
        private UIActionButton _edit = null!;
        private UIActionButton _export = null!;
        private UIActionButton _rename = null!;
        private UIActionButton _confirmRename = null!;
        private UIActionButton _cancelRename = null!;
        private UIActionButton _exportStartDown = null!;
        private UIActionButton _exportStartUp = null!;
        private UIActionButton _exportEndDown = null!;
        private UIActionButton _exportEndUp = null!;
        private UIActionButton _exportFormatPrevious = null!;
        private UIActionButton _exportFormatNext = null!;
        private UIActionButton _confirmExport = null!;
        private UIActionButton _cancelExport = null!;
        private UIActionButton _chooseExportDirectory = null!;
        private UIActionButton _replaceExistingExport = null!;
        private UIActionButton _folder = null!;
        private UIActionButton _create = null!;
        private Rectangle _renameInputBounds;
        private SaveService.ExportOperation? _exportOperation;
        private bool _exportPending;
        private readonly Queue<string> _pendingProjectFiles = new();
        private readonly List<string> _projectLoadErrors = new();
        private readonly UIFeedbackState _feedback = new();
        private UIFeedbackOverlay _feedbackOverlay = null!;
        private int _projectLoadTotal;
        private int _projectLoadProcessed;
        private string? _pendingOpenFile;
        private Task<ThumbnailLoadResult>? _thumbnailLoad;
        private Task<MetadataLoadResult>? _metadataLoad;
        private int _thumbnailGeneration;

        public ProjectsScene() => _galleryView = new ProjectGalleryComponent(_gallery);

        public override void LoadContent()
        {
            try { _operations.EnsureProjectsDirectory(); }
            catch (Exception ex)
            {
                _feedback.ShowError("Projects folder is unavailable", ex.Message,
                    "Check the application's folder permissions, then restart FrameByFrame.");
            }
            _back = new UIActionButton("< Home", GoHome);
            _previous = new UIActionButton("< Page", () => ChangePage(-1));
            _next = new UIActionButton("Page >", () => ChangePage(1));
            _edit = new UIActionButton("Edit animation", OpenSelectedProject);
            _export = new UIActionButton("Export animation", BeginExportRange);
            _rename = new UIActionButton("Rename project", BeginRename);
            _confirmRename = new UIActionButton("Save name", ConfirmRename);
            _cancelRename = new UIActionButton("Cancel", CancelRename);
            _exportStartDown = new UIActionButton("-", () => _exportDialog.SetStart(_exportDialog.StartFrame - 1));
            _exportStartUp = new UIActionButton("+", () => _exportDialog.SetStart(_exportDialog.StartFrame + 1));
            _exportEndDown = new UIActionButton("-", () => _exportDialog.SetEnd(_exportDialog.EndFrame - 1));
            _exportEndUp = new UIActionButton("+", () => _exportDialog.SetEnd(_exportDialog.EndFrame + 1));
            _exportFormatPrevious = new UIActionButton("<", () => _exportDialog.SelectFormat(-1));
            _exportFormatNext = new UIActionButton(">", () => _exportDialog.SelectFormat(1));
            _confirmExport = new UIActionButton("Export range", ExportSelectedProject);
            _cancelExport = new UIActionButton("Cancel", _exportDialog.Close);
            _chooseExportDirectory = new UIActionButton("Choose export folder", ChooseExportDirectory);
            _replaceExistingExport = new UIActionButton("Replace existing: No", () =>
            {
                _exportDialog.ToggleReplace();
                _replaceExistingExport.Text = $"Replace existing: {(_exportDialog.ReplaceExisting ? "Yes" : "No")}";
                _replaceExistingExport.IsSelected = _exportDialog.ReplaceExisting;
            });
            _folder = new UIActionButton("Open projects folder", OpenProjectFolder);
            _folder.Tooltip = _operations.ProjectsDirectory;
            _create = new UIActionButton("Create animation", CreateAnimation);
            _feedbackOverlay = new UIFeedbackOverlay(_feedback);
        }

        private void Layout()
        {
            int cx = GlobalParameters.screenWidth / 2;
            int cy = GlobalParameters.screenHeight / 2;
            int S(int value) => UILayoutEngine.Scale(value);
            _back.Bounds = new Rectangle(S(20), S(16), S(150), S(56));
            _folder.Bounds = new Rectangle(GlobalParameters.screenWidth - S(300), S(16), S(280), S(56));

            int margin = S(20);
            int navigationY = S(96);
            int navigationWidth = Math.Min(S(110), Math.Max(64, (GlobalParameters.screenWidth - margin * 3) / 2));
            _previous.Bounds = new Rectangle(margin, navigationY, navigationWidth, S(42));
            _next.Bounds = new Rectangle(GlobalParameters.screenWidth - margin - navigationWidth,
                navigationY, navigationWidth, S(42));

            int actionHeight = S(54);
            int actionY = GlobalParameters.screenHeight - margin - actionHeight;
            Rectangle actions = new(margin, actionY,
                Math.Max(1, GlobalParameters.screenWidth - margin * 2), actionHeight);
            IReadOnlyList<Rectangle> actionBounds = UILayoutEngine.Stack(actions, UIAxis.Horizontal, 3, S(12));
            _edit.Bounds = actionBounds[0];
            _export.Bounds = actionBounds[1];
            _rename.Bounds = actionBounds[2];

            int columns = GalleryColumnCount(GlobalParameters.screenWidth);
            int pageSize = columns * 2;
            _gallery.NormalizePage(pageSize);
            Rectangle gallery = new(margin, navigationY + S(50),
                Math.Max(1, GlobalParameters.screenWidth - margin * 2),
                Math.Max(1, actionY - S(14) - (navigationY + S(50))));
            _galleryView.Arrange(gallery, pageSize, columns, S(14));

            _renameInputBounds = new Rectangle(cx - S(220), cy - S(15), S(440), S(54));
            _confirmRename.Bounds = new Rectangle(cx - S(220), cy + S(60), S(210), S(52));
            _cancelRename.Bounds = new Rectangle(cx + S(10), cy + S(60), S(210), S(52));
            _create.Bounds = new Rectangle(cx - S(120), cy + S(30), S(240), S(48));
            _exportFormatPrevious.Bounds = new Rectangle(cx - S(40), cy - S(130), S(46), S(42));
            _exportFormatNext.Bounds = new Rectangle(cx + S(110), cy - S(130), S(46), S(42));
            _exportStartDown.Bounds = new Rectangle(cx - S(40), cy - S(75), S(46), S(42));
            _exportStartUp.Bounds = new Rectangle(cx + S(110), cy - S(75), S(46), S(42));
            _exportEndDown.Bounds = new Rectangle(cx - S(40), cy - S(20), S(46), S(42));
            _exportEndUp.Bounds = new Rectangle(cx + S(110), cy - S(20), S(46), S(42));
            _replaceExistingExport.Bounds = new Rectangle(cx - S(220), cy + S(30), S(440), S(38));
            _chooseExportDirectory.Bounds = new Rectangle(cx - S(220), cy + S(98), S(440), S(44));
            _confirmExport.Bounds = new Rectangle(cx - S(220), cy + S(152), S(210), S(52));
            _cancelExport.Bounds = new Rectangle(cx + S(10), cy + S(152), S(210), S(52));
            bool hasProjects = _gallery.Projects.Count > 0;
            bool hasSelection = _gallery.SelectedIndex >= 0 && _gallery.SelectedIndex < _gallery.Projects.Count;
            _previous.IsEnabled = hasProjects && _gallery.PageStart > 0;
            _next.IsEnabled = hasProjects && _gallery.PageStart + pageSize < _gallery.Projects.Count;
            _edit.IsEnabled = _export.IsEnabled = _rename.IsEnabled = hasSelection;
            _feedbackOverlay.Arrange(new Rectangle(0, 0,
                GlobalParameters.screenWidth, GlobalParameters.screenHeight));
        }

        private static int GalleryColumnCount(int width) => width >= 1000 ? 3 : width >= 600 ? 2 : 1;

        internal static int ResolvePageStart(int currentPageStart, int selectedIndex,
            int itemCount, int pageSize) => ProjectGalleryState.ResolvePageStart(
                currentPageStart, selectedIndex, itemCount, pageSize);

        public override void Update(GameTime gameTime)
        {
            Layout(); UIPointerRouter.BeginFrame(); _feedback.Advance(gameTime);
            if (_pendingOpenFile != null)
            {
                ProcessPendingProjectOpen();
                return;
            }
            if (_metadataLoad != null || _pendingProjectFiles.Count > 0)
            {
                LoadNextProject();
                return;
            }
            if (_exportPending)
            {
                StartPendingExport();
                return;
            }
            if (_exportOperation != null)
            {
                AdvanceExport();
                return;
            }
            if (_feedback.IsVisible)
            {
                _feedbackOverlay.Update();
                return;
            }
            AdvanceThumbnailLoading();
            if (_renameDialog.IsOpen)
            {
                _confirmRename.Update(); _cancelRename.Update(); UpdateRenameText();
                return;
            }
            if (_exportDialog.IsOpen)
            {
                _exportFormatPrevious.Update(); _exportFormatNext.Update();
                _exportStartDown.Update(); _exportStartUp.Update();
                _exportEndDown.Update(); _exportEndUp.Update();
                _replaceExistingExport.Update();
                _chooseExportDirectory.Update();
                _confirmExport.Update(); _cancelExport.Update();
                if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC")) _exportDialog.Close();
                return;
            }
            _back.Update(); _folder.Update();
            if (_gallery.Projects.Count == 0) _create.Update();
            else
            {
                _previous.Update(); _next.Update();
                bool clickedCard = _galleryView.UpdateSelection();
                _edit.Update(); _export.Update(); _rename.Update();
                if (GlobalParameters.GlobalMouse.LeftClick() && !clickedCard &&
                    !UIPointerRouter.IsPointerBlocked()) DeselectProject();
            }
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC")) GoHome();
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("ENTER")) OpenSelectedProject();
        }

        public override void Draw(Vector2 offset)
        {
            Layout();
            GlobalParameters.GlobalGraphics.Clear(UITheme.Background);
            int S(int value) => UILayoutEngine.Scale(value);
            UIRenderer.Fill(new Rectangle(0, 0, GlobalParameters.screenWidth, S(88)), UITheme.Surface);
            string heading = _gallery.Projects.Count == 0
                ? "Your animations - No saved projects yet"
                : $"Your animations - {_gallery.PageStart + 1}-{Math.Min(_gallery.PageStart + _galleryView.CardBounds.Count, _gallery.Projects.Count)} of {_gallery.Projects.Count}";
            new UITextContainer
            {
                Bounds = new Rectangle(_back.Bounds.Right + S(16), S(7), Math.Max(1, _folder.Bounds.X - _back.Bounds.Right - S(32)), S(42)),
                HorizontalAlignment = UIAlign.Start,
                MaxLines = 1
            }.Draw(heading, UITheme.Text, UITextRole.Title);
            new UITextContainer
            {
                Bounds = new Rectangle(_back.Bounds.Right + S(16), S(48),
                    Math.Max(1, _folder.Bounds.X - _back.Bounds.Right - S(32)), S(28)),
                HorizontalAlignment = UIAlign.Start,
                MaxLines = 1,
                Padding = 0
            }.Draw(_operations.ProjectsDirectory, UITheme.TextMuted, UITextRole.Caption);
            _back.Draw(); _folder.Draw();

            if (_gallery.Projects.Count == 0) DrawEmptyState(); else DrawSelectedProject();
            if (_renameDialog.IsOpen) DrawRenameDialog();
            if (_exportDialog.IsOpen) DrawExportRangeDialog();
            _feedbackOverlay.Draw();
        }

        private void DrawEmptyState()
        {
            int S(int value) => UILayoutEngine.Scale(value);
            Rectangle card = new(GlobalParameters.screenWidth / 2 - S(300), GlobalParameters.screenHeight / 2 - S(160), S(600), S(300));
            UIRenderer.Fill(card, UITheme.Surface); UIRenderer.Border(card, UITheme.Border, 2);
            new UITextContainer { Bounds = new Rectangle(card.X + S(24), card.Y + S(40), card.Width - S(48), S(64)), MaxLines = 2 }
                .Draw("Your first animation starts here", UITheme.Primary, UITextRole.Title);
            new UITextContainer { Bounds = new Rectangle(card.X + S(32), card.Y + S(110), card.Width - S(64), S(72)), MaxLines = 2 }
                .Draw("Create a canvas, draw a frame, and save it to see it here.", UITheme.TextMuted, UITextRole.Body);
            _create.Draw(true);
        }

        private void DrawSelectedProject()
        {
            _galleryView.Draw();

            int pageSize = GalleryColumnCount(GlobalParameters.screenWidth) * 2;
            if (_gallery.Projects.Count > pageSize) { _previous.Draw(); _next.Draw(); }
            _edit.Draw(true); _export.Draw(); _rename.Draw();
        }

        private void BeginRename()
        {
            if (_gallery.Selected == null) return;
            _renameDialog.Open(_gallery.Selected.Metadata.ProjectName);
        }

        private void CancelRename() => _renameDialog.Cancel();

        private void ConfirmRename()
        {
            GalleryProject? project = _gallery.Selected;
            if (project == null) return;
            if (_renameDialog.Confirm(name =>
            {
                ProjectRenameResult result = _operations.Rename(project.FilePath, name);
                project.FilePath = result.FilePath;
                project.Metadata = result.Metadata;
                return null;
            }))
                _feedback.ShowSuccess("Project renamed", "The new project name was saved successfully.");
        }

        private void UpdateRenameText()
        {
            bool shift = GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.LeftShift) ||
                GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.RightShift);
            RenameInputResult result = _renameDialog.ApplyKeys(
                GlobalParameters.GlobalKeyboard.GetNewlyPressedKeys(), shift);
            if (result == RenameInputResult.Confirm)
            {
                ConfirmRename();
                return;
            }
            if (result == RenameInputResult.Cancel) CancelRename();
        }

        private void DrawRenameDialog() => ProjectRenameDialogComponent.Draw(
            _renameDialog, _renameInputBounds, _confirmRename, _cancelRename);

        public void LoadAnimations()
        {
            DisposeProjects();
            _pendingProjectFiles.Clear();
            _projectLoadErrors.Clear();
            _projectLoadProcessed = 0;
            _feedback.Clear();
            try
            {
                foreach (string file in _operations.ListProjectFiles())
                    _pendingProjectFiles.Enqueue(file);
                _projectLoadTotal = _pendingProjectFiles.Count;
                if (_projectLoadTotal > 0)
                    _feedback.ShowBusy("Loading projects", $"Preparing {_projectLoadTotal} saved project(s)...", 0f);
            }
            catch (Exception ex)
            {
                _feedback.ShowError("Projects could not be listed", ex.Message,
                    "Check access to the Projects folder, then reopen this screen to retry.");
            }
        }

        private void LoadNextProject()
        {
            if (_metadataLoad == null)
            {
                string pendingFile = _pendingProjectFiles.Dequeue();
                int generation = _thumbnailGeneration;
                _metadataLoad = Task.Run(() =>
                {
                    try { return new MetadataLoadResult(generation, pendingFile,
                        _operations.ReadMetadata(pendingFile), null); }
                    catch (Exception ex) { return new MetadataLoadResult(generation, pendingFile, null, ex); }
                });
                return;
            }
            if (!_metadataLoad.IsCompleted) return;
            MetadataLoadResult result = _metadataLoad.GetAwaiter().GetResult();
            _metadataLoad = null;
            if (result.Generation != _thumbnailGeneration) return;
            if (result.Metadata != null) _gallery.Add(new GalleryProject(result.FilePath, result.Metadata));
            else if (result.Error != null)
                _projectLoadErrors.Add($"{Path.GetFileName(result.FilePath)}: {result.Error.Message}");
            _projectLoadProcessed++;
            if (_pendingProjectFiles.Count > 0)
            {
                _feedback.ShowBusy("Loading projects",
                    $"Processed {_projectLoadProcessed} of {_projectLoadTotal} project files...",
                    _projectLoadProcessed / (float)Math.Max(1, _projectLoadTotal));
                return;
            }

            if (_projectLoadErrors.Count == 0) _feedback.Clear();
            else
            {
                _feedback.ShowErrorList($"{_projectLoadErrors.Count} project(s) were skipped", _projectLoadErrors,
                    "The remaining projects are safe to use. Replace or remove the damaged files before retrying.");
            }
        }

        private void AdvanceThumbnailLoading()
        {
            if (_thumbnailLoad != null)
            {
                if (!_thumbnailLoad.IsCompleted) return;
                ThumbnailLoadResult result = _thumbnailLoad.GetAwaiter().GetResult();
                _thumbnailLoad = null;
                if (result.Generation != _thumbnailGeneration) return;
                GalleryProject? project = _gallery.Projects.FirstOrDefault(candidate => candidate.FilePath == result.FilePath);
                if (result.Thumbnail != null && project != null)
                {
                    project.Thumbnail?.Dispose();
                    project.Thumbnail = SaveService.MaterializeProjectThumbnail(result.Thumbnail);
                    _gallery.Touch(project);
                    _gallery.TrimThumbnailCache(MaximumCachedThumbnails, _galleryView.CardBounds.Count);
                }
                else if (result.Error != null && project != null)
                {
                    project.ThumbnailFailed = true;
                    _projectLoadErrors.Add($"{Path.GetFileName(result.FilePath)}: {result.Error.Message}");
                    _feedback.ShowErrorList("A project preview could not be loaded", _projectLoadErrors,
                        "The damaged project was not opened. Repair or remove it, then reload the gallery.");
                    return;
                }
            }

            int visibleCount = Math.Min(_galleryView.CardBounds.Count, Math.Max(0, _gallery.Projects.Count - _gallery.PageStart));
            GalleryProject? next = Enumerable.Range(_gallery.PageStart, visibleCount)
                .Select(index => _gallery.Projects[index])
                .FirstOrDefault(project => project.Thumbnail == null && !project.ThumbnailFailed);
            if (next == null) return;
            string file = next.FilePath;
            int generation = _thumbnailGeneration;
            _thumbnailLoad = Task.Run(() =>
            {
                try { return new ThumbnailLoadResult(generation, file, _operations.LoadThumbnail(file), null); }
                catch (Exception ex) { return new ThumbnailLoadResult(generation, file, null, ex); }
            });
        }

        private void SelectProject(int index) => _gallery.Select(index);

        private void DeselectProject() => _gallery.Deselect();

        private void ChangePage(int delta)
        {
            int pageSize = GalleryColumnCount(GlobalParameters.screenWidth) * 2;
            if (_gallery.ChangePage(delta, pageSize)) Layout();
        }

        private void OpenSelectedProject()
        {
            if (_gallery.SelectedIndex < 0 || _gallery.SelectedIndex >= _gallery.Projects.Count) return;
            _pendingOpenFile = _gallery.Projects[_gallery.SelectedIndex].FilePath;
            _feedback.ShowBusy("Opening project", $"Loading {Path.GetFileNameWithoutExtension(_pendingOpenFile)}...");
        }

        private void ProcessPendingProjectOpen()
        {
            string? file = _pendingOpenFile;
            _pendingOpenFile = null;
            if (string.IsNullOrWhiteSpace(file)) return;
            try
            {
                Animation.Animation loaded = _operations.Load(file);
                DrawingScene drawing = (DrawingScene)GlobalParameters.Scenes[UIConstants.DRAWING_SCENE];
                drawing.LoadAnimation(loaded);
                _feedback.Clear();
                GlobalParameters.CurrentScene = drawing;
            }
            catch (Exception ex)
            {
                _feedback.ShowError("Project could not be opened", ex.Message,
                    "Confirm the file is a valid FrameByFrame project and that it is readable, then try again.");
            }
        }
        private void CreateAnimation()
        {
            MenuScene menu = (MenuScene)GlobalParameters.Scenes[UIConstants.MENU_SCENE];
            menu.BeginNewAnimationConfiguration();
            GlobalParameters.CurrentScene = menu;
        }

        private void BeginExportRange()
        {
            if (_gallery.Selected == null) return;
            _exportDialog.Open(_gallery.Selected.Metadata.FrameCount);
            _replaceExistingExport.Text = "Replace existing: No";
            _replaceExistingExport.IsSelected = false;
        }
        private void ExportSelectedProject()
        {
            if (_gallery.SelectedIndex < 0 || _gallery.SelectedIndex >= _gallery.Projects.Count) return;
            _exportDialog.Close();
            _exportOperation = null;
            _exportPending = true;
            _feedback.ShowBusy("Preparing export", "Saving the project and preparing frames...", 0f);
        }

        private void StartPendingExport()
        {
            _exportPending = false;
            try
            {
                GalleryProject project = _gallery.Selected ?? throw new InvalidOperationException("No project is selected.");
                _exportOperation = _operations.BeginExport(project.FilePath,
                    _exportDialog.StartFrame, _exportDialog.EndFrame, _exportDialog.Directory,
                    _exportDialog.Format, _exportDialog.ReplaceExisting);
                _feedback.ShowBusy("Exporting animation", _exportOperation.Status, _exportOperation.Progress);
            }
            catch (Exception ex)
            {
                _exportOperation = null;
                _feedback.ShowError("Export could not be started", ex.Message,
                    "Check the destination permissions and available disk space, then try again.");
            }
        }

        private void AdvanceExport()
        {
            SaveService.ExportOperation? operation = _exportOperation;
            if (operation == null) return;
            if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC")) operation.Cancel();
            if (operation.IsCanceled)
            {
                _exportOperation = null;
                _feedback.ShowSuccess("Export canceled", "No existing output was changed.");
                return;
            }
            if (operation.IsFailed)
            {
                string message = operation.Failure?.Message ?? operation.Status;
                _exportOperation = null;
                _feedback.ShowError("Export failed", message,
                    "Check the destination permissions, available disk space, and export dependencies, then try again.");
                return;
            }
            if (operation.IsComplete)
            {
                string outputPath = operation.OutputPath;
                _exportOperation = null;
                _feedback.ShowSuccess("Export complete", $"Animation exported to {outputPath}");
                return;
            }
            _feedback.ShowBusy("Exporting animation", $"{operation.Status} Press Esc to cancel.", operation.Progress);
        }
        private void ChooseExportDirectory()
        {
            try
            {
                using var dialog = new NativeFileDialog().SelectFolder();
                DialogResult result = dialog.Open(out string[]? output, _exportDialog.Directory);
                if (result == DialogResult.Okay && output is { Length: > 0 })
                {
                    _exportDialog.SetDirectory(output[0]);
                }
            }
            catch (Exception ex)
            {
                _exportDialog.SetError($"Could not choose an export folder: {ex.Message}");
                _feedback.ShowError("Export folder could not be selected", ex.Message,
                    "Check folder access permissions, then choose another destination.");
            }
        }
        private void DrawExportRangeDialog() => ProjectExportDialogComponent.Draw(
            _exportDialog, _exportFormatPrevious, _exportFormatNext,
            _exportStartDown, _exportStartUp, _exportEndDown, _exportEndUp,
            _replaceExistingExport, _chooseExportDirectory, _confirmExport, _cancelExport);
        private void OpenProjectFolder()
        {
            try { _operations.OpenProjectsFolder(); }
            catch (Exception ex)
            {
                _feedback.ShowError("Projects folder could not be opened", ex.Message,
                    $"Open this folder manually: {_operations.ProjectsDirectory}");
            }
        }
        private void GoHome()
        {
            GlobalParameters.CurrentScene = GlobalParameters.Scenes[UIConstants.MENU_SCENE];
        }
        private void DisposeProjects()
        {
            _thumbnailGeneration++;
            _thumbnailLoad = null;
            _metadataLoad = null;
            _gallery.Clear();
        }
        public override void Dispose() => DisposeProjects();
    }
}
