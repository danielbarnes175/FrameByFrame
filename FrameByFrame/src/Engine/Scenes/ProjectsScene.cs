using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogNET;

namespace FrameByFrame.src.Engine.Scenes
{
    public class ProjectsScene : BaseScene
    {
        private sealed class GalleryProject
        {
            public string FilePath { get; set; }
            public FbfProjectFile.ProjectMetadata Metadata { get; set; }
            public Frame? Thumbnail { get; set; }
            public bool ThumbnailFailed { get; set; }
            public long LastThumbnailAccess { get; set; }
            public GalleryProject(string filePath, FbfProjectFile.ProjectMetadata metadata)
            {
                FilePath = filePath;
                Metadata = metadata;
            }
        }

        private sealed record ThumbnailLoadResult(int Generation, string FilePath,
            FbfProjectFile.ProjectThumbnail? Thumbnail, Exception? Error);
        private sealed record MetadataLoadResult(int Generation, string FilePath,
            FbfProjectFile.ProjectMetadata? Metadata, Exception? Error);

        private const int MaximumCachedThumbnails = 8;
        private readonly List<GalleryProject> _projects = new();
        private readonly List<Rectangle> _projectCardBounds = new();
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
        private UIActionButton _folder = null!;
        private UIActionButton _create = null!;
        private int _selected = -1;
        private int _pageStart;
        private bool _isRenaming;
        private string _renameText = string.Empty;
        private string _renameError = string.Empty;
        private Rectangle _renameInputBounds;
        private bool _isSelectingExportRange;
        private int _exportStart;
        private int _exportEnd;
        private ExportFormat _exportFormat = ExportFormat.Gif;
        private string _exportDirectory = Path.GetFullPath("Projects");
        private string _exportMessage = string.Empty;
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
        private long _thumbnailAccess;
        private int _thumbnailGeneration;

        public override void LoadContent()
        {
            try { Directory.CreateDirectory("Projects"); }
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
            _exportStartDown = new UIActionButton("-", () => SetExportStart(_exportStart - 1));
            _exportStartUp = new UIActionButton("+", () => SetExportStart(_exportStart + 1));
            _exportEndDown = new UIActionButton("-", () => SetExportEnd(_exportEnd - 1));
            _exportEndUp = new UIActionButton("+", () => SetExportEnd(_exportEnd + 1));
            _exportFormatPrevious = new UIActionButton("<", () => SelectExportFormat(-1));
            _exportFormatNext = new UIActionButton(">", () => SelectExportFormat(1));
            _confirmExport = new UIActionButton("Export range", ExportSelectedProject);
            _cancelExport = new UIActionButton("Cancel", () => _isSelectingExportRange = false);
            _chooseExportDirectory = new UIActionButton("Choose export folder", ChooseExportDirectory);
            _folder = new UIActionButton("Open projects folder", OpenProjectFolder);
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
            _pageStart = ResolvePageStart(_pageStart, _selected, _projects.Count, pageSize);
            Rectangle gallery = new(margin, navigationY + S(50),
                Math.Max(1, GlobalParameters.screenWidth - margin * 2),
                Math.Max(1, actionY - S(14) - (navigationY + S(50))));
            _projectCardBounds.Clear();
            _projectCardBounds.AddRange(UILayoutEngine.Grid(gallery, pageSize, columns, S(14)));

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
            _chooseExportDirectory.Bounds = new Rectangle(cx - S(220), cy + S(72), S(440), S(44));
            _confirmExport.Bounds = new Rectangle(cx - S(220), cy + S(130), S(210), S(52));
            _cancelExport.Bounds = new Rectangle(cx + S(10), cy + S(130), S(210), S(52));
            bool hasProjects = _projects.Count > 0;
            bool hasSelection = _selected >= 0 && _selected < _projects.Count;
            _previous.IsEnabled = hasProjects && _pageStart > 0;
            _next.IsEnabled = hasProjects && _pageStart + pageSize < _projects.Count;
            _edit.IsEnabled = _export.IsEnabled = _rename.IsEnabled = hasSelection;
            _feedbackOverlay.Arrange(new Rectangle(0, 0,
                GlobalParameters.screenWidth, GlobalParameters.screenHeight));
        }

        private static int GalleryColumnCount(int width) => width >= 1000 ? 3 : width >= 600 ? 2 : 1;

        internal static int ResolvePageStart(int currentPageStart, int selectedIndex,
            int itemCount, int pageSize)
        {
            if (itemCount <= 0) return 0;
            pageSize = Math.Max(1, pageSize);
            int maxPageStart = ((itemCount - 1) / pageSize) * pageSize;
            if (selectedIndex >= 0 && selectedIndex < itemCount)
                return Math.Clamp(selectedIndex / pageSize * pageSize, 0, maxPageStart);
            int clamped = Math.Clamp(currentPageStart, 0, maxPageStart);
            return clamped / pageSize * pageSize;
        }

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
            if (_isRenaming)
            {
                _confirmRename.Update(); _cancelRename.Update(); UpdateRenameText();
                return;
            }
            if (_isSelectingExportRange)
            {
                _exportFormatPrevious.Update(); _exportFormatNext.Update();
                _exportStartDown.Update(); _exportStartUp.Update();
                _exportEndDown.Update(); _exportEndUp.Update();
                _chooseExportDirectory.Update();
                _confirmExport.Update(); _cancelExport.Update();
                if (GlobalParameters.GlobalKeyboard.GetPressSingle("ESC")) _isSelectingExportRange = false;
                return;
            }
            _back.Update(); _folder.Update();
            if (_projects.Count == 0) _create.Update();
            else
            {
                _previous.Update(); _next.Update();
                int visibleCount = Math.Min(_projectCardBounds.Count, _projects.Count - _pageStart);
                bool clickedCard = false;
                for (int i = 0; i < visibleCount; i++)
                {
                    if (!UIPointerRouter.Clicked(_projectCardBounds[i])) continue;
                    clickedCard = true;
                    SelectProject(_pageStart + i);
                }
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
            string heading = _projects.Count == 0
                ? "Your animations - No saved projects yet"
                : $"Your animations - {_pageStart + 1}-{Math.Min(_pageStart + _projectCardBounds.Count, _projects.Count)} of {_projects.Count}";
            new UITextContainer
            {
                Bounds = new Rectangle(_back.Bounds.Right + S(16), S(10), Math.Max(1, _folder.Bounds.X - _back.Bounds.Right - S(32)), S(68)),
                HorizontalAlignment = UIAlign.Start,
                MaxLines = 2
            }.Draw(heading, UITheme.Text, UITextRole.Title);
            _back.Draw(); _folder.Draw();

            if (_projects.Count == 0) DrawEmptyState(); else DrawSelectedProject();
            if (_isRenaming) DrawRenameDialog();
            if (_isSelectingExportRange) DrawExportRangeDialog();
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
            int S(int value) => UILayoutEngine.Scale(value);
            int visibleCount = Math.Min(_projectCardBounds.Count,
                Math.Max(0, _projects.Count - _pageStart));
            for (int i = 0; i < visibleCount; i++)
            {
                int projectIndex = _pageStart + i;
                GalleryProject project = _projects[projectIndex];
                FbfProjectFile.ProjectMetadata metadata = project.Metadata;
                Rectangle card = _projectCardBounds[i];
                bool selected = projectIndex == _selected;
                UIRenderer.Fill(card, selected ? UITheme.SurfaceRaised : UITheme.Surface);
                UIRenderer.Border(card, selected ? UITheme.Primary : UITheme.Border, selected ? 3 : 2);

                int padding = Math.Max(4, S(10));
                int detailsHeight = Math.Min(S(66), Math.Max(S(42), card.Height / 3));
                Rectangle previewArea = new(card.X + padding, card.Y + padding,
                    Math.Max(1, card.Width - padding * 2),
                    Math.Max(1, card.Height - detailsHeight - padding * 2));
                Rectangle preview = UILayoutEngine.FitAspect(previewArea,
                    metadata.Width / (float)metadata.Height);
                project.Thumbnail?.DrawPreview(preview, 1f,
                    metadata.IsBackgroundTransparent, metadata.BackgroundColor);
                if (project.Thumbnail == null)
                    new UITextContainer { Bounds = preview, MaxLines = 1 }.Draw(
                        project.ThumbnailFailed ? "Preview unavailable" : "Loading preview...",
                        UITheme.TextMuted, UITextRole.Caption);
                else project.LastThumbnailAccess = ++_thumbnailAccess;
                UIRenderer.Border(preview, selected ? UITheme.Secondary : UITheme.Border, 2);

                Rectangle nameBounds = new(card.X + padding, previewArea.Bottom + S(3),
                    Math.Max(1, card.Width - padding * 2), Math.Max(20, detailsHeight / 2));
                new UITextContainer { Bounds = nameBounds, HorizontalAlignment = UIAlign.Start, MaxLines = 1, Padding = 2 }
                    .Draw(metadata.ProjectName, UITheme.Text, UITextRole.Label);
                Rectangle details = new(nameBounds.X, nameBounds.Bottom, nameBounds.Width,
                    Math.Max(18, card.Bottom - padding - nameBounds.Bottom));
                new UITextContainer { Bounds = details, HorizontalAlignment = UIAlign.Start, MaxLines = 1, Padding = 2 }
                    .Draw($"{metadata.FrameCount} frames | {metadata.FramesPerSecond} fps", UITheme.TextMuted, UITextRole.Caption);
            }

            int pageSize = GalleryColumnCount(GlobalParameters.screenWidth) * 2;
            if (_projects.Count > pageSize) { _previous.Draw(); _next.Draw(); }
            _edit.Draw(true); _export.Draw(); _rename.Draw();
        }

        private void BeginRename()
        {
            if (_selected < 0 || _selected >= _projects.Count) return;
            _renameText = _projects[_selected].Metadata.ProjectName;
            _renameError = string.Empty;
            _isRenaming = true;
        }

        private void CancelRename() { _isRenaming = false; _renameError = string.Empty; }

        private void ConfirmRename()
        {
            try
            {
                GalleryProject project = _projects[_selected];
                using Animation.Animation animation = SaveService.LoadAnimation(project.FilePath);
                project.FilePath = SaveService.RenameAnimation(animation, project.FilePath, _renameText);
                project.Metadata = SaveService.ReadProjectMetadata(project.FilePath);
                _isRenaming = false;
                _renameError = string.Empty;
                _feedback.ShowSuccess("Project renamed", "The new project name was saved successfully.");
            }
            catch (Exception ex)
            {
                _renameError = $"{ex.Message} Choose a unique name without path characters, then try again.";
            }
        }

        private void UpdateRenameText()
        {
            bool shift = GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.LeftShift) ||
                GlobalParameters.GlobalKeyboard.IsKeyHeldDown(Keys.RightShift);
            foreach (Keys key in GlobalParameters.GlobalKeyboard.GetNewlyPressedKeys())
            {
                if (key == Keys.Enter) { ConfirmRename(); return; }
                if (key == Keys.Escape) { CancelRename(); return; }
                if (key == Keys.Back && _renameText.Length > 0) { _renameText = _renameText[..^1]; continue; }
                char? character = KeyToCharacter(key, shift);
                if (character.HasValue && _renameText.Length < 80) _renameText += character.Value;
            }
        }

        private static char? KeyToCharacter(Keys key, bool shift)
        {
            if (key >= Keys.A && key <= Keys.Z)
            {
                char value = (char)('a' + (int)key - (int)Keys.A);
                return shift ? char.ToUpperInvariant(value) : value;
            }
            if (key >= Keys.D0 && key <= Keys.D9) return (char)('0' + (int)key - (int)Keys.D0);
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return (char)('0' + (int)key - (int)Keys.NumPad0);
            return key switch { Keys.Space => ' ', Keys.OemMinus => shift ? '_' : '-', _ => null };
        }

        private void DrawRenameDialog()
        {
            int S(int value) => UILayoutEngine.Scale(value);
            Rectangle dialog = new(GlobalParameters.screenWidth / 2 - S(260), GlobalParameters.screenHeight / 2 - S(120), S(520), S(270));
            UIRenderer.Fill(dialog, UITheme.SurfaceRaised); UIRenderer.Border(dialog, UITheme.Primary, 3);
            new UITextContainer { Bounds = new Rectangle(dialog.X + S(24), dialog.Y + S(18), dialog.Width - S(48), S(42)), MaxLines = 1 }
                .Draw("Rename project", UITheme.Primary, UITextRole.Heading);
            UIRenderer.Fill(_renameInputBounds, UITheme.Surface); UIRenderer.Border(_renameInputBounds, UITheme.Border, 2);
            string display = string.IsNullOrEmpty(_renameText) ? "Type a project name" : _renameText + "|";
            new UITextContainer { Bounds = _renameInputBounds, HorizontalAlignment = UIAlign.Start, Padding = S(12), MaxLines = 1 }
                .Draw(display, string.IsNullOrEmpty(_renameText) ? UITheme.TextMuted : UITheme.Text, UITextRole.Body);
            if (!string.IsNullOrEmpty(_renameError))
                new UITextContainer { Bounds = new Rectangle(dialog.X + S(24), _renameInputBounds.Bottom + S(4), dialog.Width - S(48), S(34)), MaxLines = 1 }
                    .Draw(_renameError, Color.IndianRed, UITextRole.Caption);
            _confirmRename.Draw(true); _cancelRename.Draw();
        }

        public void LoadAnimations()
        {
            DisposeProjects(); _selected = -1; _pageStart = 0;
            _pendingProjectFiles.Clear();
            _projectLoadErrors.Clear();
            _projectLoadProcessed = 0;
            _feedback.Clear();
            try
            {
                foreach (string file in Directory.GetFiles("Projects", "*.fbf"))
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
                        SaveService.ReadProjectMetadata(pendingFile), null); }
                    catch (Exception ex) { return new MetadataLoadResult(generation, pendingFile, null, ex); }
                });
                return;
            }
            if (!_metadataLoad.IsCompleted) return;
            MetadataLoadResult result = _metadataLoad.GetAwaiter().GetResult();
            _metadataLoad = null;
            if (result.Generation != _thumbnailGeneration) return;
            if (result.Metadata != null) _projects.Add(new GalleryProject(result.FilePath, result.Metadata));
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
                GalleryProject? project = _projects.FirstOrDefault(candidate => candidate.FilePath == result.FilePath);
                if (result.Thumbnail != null && project != null)
                {
                    project.Thumbnail?.Dispose();
                    project.Thumbnail = SaveService.MaterializeProjectThumbnail(result.Thumbnail);
                    project.LastThumbnailAccess = ++_thumbnailAccess;
                    TrimThumbnailCache();
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

            int visibleCount = Math.Min(_projectCardBounds.Count, Math.Max(0, _projects.Count - _pageStart));
            GalleryProject? next = Enumerable.Range(_pageStart, visibleCount)
                .Select(index => _projects[index])
                .FirstOrDefault(project => project.Thumbnail == null && !project.ThumbnailFailed);
            if (next == null) return;
            string file = next.FilePath;
            int generation = _thumbnailGeneration;
            _thumbnailLoad = Task.Run(() =>
            {
                try { return new ThumbnailLoadResult(generation, file, SaveService.LoadProjectThumbnail(file), null); }
                catch (Exception ex) { return new ThumbnailLoadResult(generation, file, null, ex); }
            });
        }

        private void TrimThumbnailCache()
        {
            int cached = _projects.Count(project => project.Thumbnail != null);
            while (cached > MaximumCachedThumbnails)
            {
                GalleryProject? oldest = _projects
                    .Where(project => project.Thumbnail != null && !IsVisible(project))
                    .OrderBy(project => project.LastThumbnailAccess)
                    .FirstOrDefault();
                if (oldest == null) return;
                oldest.Thumbnail!.Dispose();
                oldest.Thumbnail = null;
                cached--;
            }
        }

        private bool IsVisible(GalleryProject project)
        {
            int index = _projects.IndexOf(project);
            return index >= _pageStart && index < _pageStart + _projectCardBounds.Count;
        }

        private void SelectProject(int index)
        {
            if (index < 0 || index >= _projects.Count) return;
            _selected = index;
            _projects[index].LastThumbnailAccess = ++_thumbnailAccess;
        }

        private void DeselectProject()
        {
            if (_selected < 0 || _selected >= _projects.Count) return;
            _selected = -1;
        }

        private void ChangePage(int delta)
        {
            if (_projects.Count == 0) return;
            int pageSize = GalleryColumnCount(GlobalParameters.screenWidth) * 2;
            int nextPageStart = ResolvePageStart(_pageStart + delta * pageSize,
                -1, _projects.Count, pageSize);
            if (nextPageStart == _pageStart) return;
            DeselectProject();
            _pageStart = nextPageStart;
            Layout();
        }

        private void OpenSelectedProject()
        {
            if (_selected < 0 || _selected >= _projects.Count) return;
            _pendingOpenFile = _projects[_selected].FilePath;
            _feedback.ShowBusy("Opening project", $"Loading {Path.GetFileNameWithoutExtension(_pendingOpenFile)}...");
        }

        private void ProcessPendingProjectOpen()
        {
            string? file = _pendingOpenFile;
            _pendingOpenFile = null;
            if (string.IsNullOrWhiteSpace(file)) return;
            try
            {
                Animation.Animation loaded = SaveService.LoadAnimation(file);
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
            if (_selected < 0 || _selected >= _projects.Count) return;
            _exportStart = 0;
            _exportEnd = _projects[_selected].Metadata.FrameCount - 1;
            _exportMessage = string.Empty;
            _isSelectingExportRange = true;
        }
        private void SetExportStart(int value) => _exportStart = Math.Clamp(value, 0, _exportEnd);
        private void SetExportEnd(int value) => _exportEnd = Math.Clamp(value, _exportStart, _projects[_selected].Metadata.FrameCount - 1);
        private void SelectExportFormat(int delta)
        {
            int count = Enum.GetValues<ExportFormat>().Length;
            _exportFormat = (ExportFormat)(((int)_exportFormat + delta + count) % count);
        }
        private void ExportSelectedProject()
        {
            if (_selected < 0 || _selected >= _projects.Count) return;
            _isSelectingExportRange = false;
            _exportOperation = null;
            _exportPending = true;
            _feedback.ShowBusy("Preparing export", "Saving the project and preparing frames...", 0f);
        }

        private void StartPendingExport()
        {
            _exportPending = false;
            try
            {
                using Animation.Animation animation = SaveService.LoadAnimation(_projects[_selected].FilePath);
                _exportOperation = SaveService.BeginExportAnimation(
                    animation, _exportStart, _exportEnd, _exportDirectory, _exportFormat);
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
            _feedback.ShowBusy("Exporting animation", operation.Status, operation.Progress);
        }
        private void ChooseExportDirectory()
        {
            try
            {
                using var dialog = new NativeFileDialog().SelectFolder();
                DialogResult result = dialog.Open(out string[]? output, _exportDirectory);
                if (result == DialogResult.Okay && output is { Length: > 0 })
                {
                    _exportDirectory = Path.GetFullPath(output[0]);
                    _exportMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                _exportMessage = $"Could not choose an export folder: {ex.Message}";
                _feedback.ShowError("Export folder could not be selected", ex.Message,
                    "Check folder access permissions, then choose another destination.");
            }
        }
        private void DrawExportRangeDialog()
        {
            int S(int value) => UILayoutEngine.Scale(value);
            int cx = GlobalParameters.screenWidth / 2;
            int cy = GlobalParameters.screenHeight / 2;
            Rectangle dialog = new(cx - S(260), cy - S(215), S(520), S(450));
            UIRenderer.Fill(dialog, UITheme.SurfaceRaised); UIRenderer.Border(dialog, UITheme.Primary, 3);
            new UITextContainer { Bounds = new Rectangle(dialog.X + S(24), dialog.Y + S(18), dialog.Width - S(48), S(42)), MaxLines = 1 }
                .Draw("Export frame range", UITheme.Primary, UITextRole.Heading);
            new UITextContainer { Bounds = new Rectangle(cx - S(210), cy - S(130), S(150), S(42)), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Format", UITheme.Text, UITextRole.Body);
            new UITextContainer { Bounds = new Rectangle(cx + S(10), cy - S(130), S(90), S(42)), MaxLines = 1 }
                .Draw(ExportFormatLabel(_exportFormat), UITheme.Primary, UITextRole.Label);
            _exportFormatPrevious.Draw(); _exportFormatNext.Draw();
            new UITextContainer { Bounds = new Rectangle(cx - S(210), cy - S(75), S(150), S(42)), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("First frame", UITheme.Text, UITextRole.Body);
            new UITextContainer { Bounds = new Rectangle(cx + S(10), cy - S(75), S(90), S(42)), MaxLines = 1 }
                .Draw((_exportStart + 1).ToString(), UITheme.Primary, UITextRole.Label);
            _exportStartDown.Draw(); _exportStartUp.Draw();
            new UITextContainer { Bounds = new Rectangle(cx - S(210), cy - S(20), S(150), S(42)), HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw("Last frame", UITheme.Text, UITextRole.Body);
            new UITextContainer { Bounds = new Rectangle(cx + S(10), cy - S(20), S(90), S(42)), MaxLines = 1 }
                .Draw((_exportEnd + 1).ToString(), UITheme.Primary, UITextRole.Label);
            _exportEndDown.Draw(); _exportEndUp.Draw();
            new UITextContainer
            {
                Bounds = new Rectangle(cx - S(220), cy + S(42), S(440), S(26)),
                HorizontalAlignment = UIAlign.Start,
                MaxLines = 1
            }.Draw(string.IsNullOrEmpty(_exportMessage) ? _exportDirectory : _exportMessage,
                string.IsNullOrEmpty(_exportMessage) ? UITheme.TextMuted : UITheme.Danger, UITextRole.Caption);
            _chooseExportDirectory.Draw();
            _confirmExport.Draw(true); _cancelExport.Draw();
        }
        private static string ExportFormatLabel(ExportFormat format) => format switch
        {
            ExportFormat.PngSequence => "PNG files",
            ExportFormat.SpriteSheet => "Spritesheet",
            _ => format.ToString().ToUpperInvariant()
        };
        private void OpenProjectFolder()
        {
            try { Process.Start(new ProcessStartInfo { FileName = Path.GetFullPath("Projects"), UseShellExecute = true, Verb = "open" }); }
            catch (Exception ex)
            {
                _feedback.ShowError("Projects folder could not be opened", ex.Message,
                    $"Open this folder manually: {Path.GetFullPath("Projects")}");
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
            foreach (GalleryProject project in _projects) project.Thumbnail?.Dispose();
            _projects.Clear();
        }
        public override void Dispose() => DisposeProjects();
    }
}
