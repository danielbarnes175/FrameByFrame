using System;
using System.Collections.Generic;
using System.Linq;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;

namespace FrameByFrame.src.Engine.Projects
{
    internal sealed class GalleryProject : IDisposable
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

        public void Dispose()
        {
            Thumbnail?.Dispose();
            Thumbnail = null;
        }
    }

    internal sealed class ProjectGalleryState : IDisposable
    {
        private long _thumbnailAccess;
        public List<GalleryProject> Projects { get; } = new();
        public int SelectedIndex { get; private set; } = -1;
        public int PageStart { get; private set; }
        public GalleryProject? Selected => SelectedIndex >= 0 && SelectedIndex < Projects.Count
            ? Projects[SelectedIndex] : null;

        public void Add(GalleryProject project) => Projects.Add(project);

        public void Select(int index)
        {
            if (index < 0 || index >= Projects.Count) return;
            SelectedIndex = index;
            Projects[index].LastThumbnailAccess = ++_thumbnailAccess;
        }

        public void Deselect() => SelectedIndex = -1;

        public void ResetPage() => PageStart = 0;

        public void NormalizePage(int pageSize) =>
            PageStart = ResolvePageStart(PageStart, SelectedIndex, Projects.Count, pageSize);

        public bool ChangePage(int delta, int pageSize)
        {
            if (Projects.Count == 0) return false;
            int next = ResolvePageStart(PageStart + delta * pageSize, -1, Projects.Count, pageSize);
            if (next == PageStart) return false;
            PageStart = next;
            Deselect();
            return true;
        }

        public void Touch(GalleryProject project) => project.LastThumbnailAccess = ++_thumbnailAccess;

        public void TrimThumbnailCache(int maximum, int visibleCount)
        {
            int cached = Projects.Count(project => project.Thumbnail != null);
            while (cached > maximum)
            {
                GalleryProject? oldest = Projects
                    .Where(project => project.Thumbnail != null && !IsVisible(project, visibleCount))
                    .OrderBy(project => project.LastThumbnailAccess)
                    .FirstOrDefault();
                if (oldest == null) return;
                oldest.Thumbnail!.Dispose();
                oldest.Thumbnail = null;
                cached--;
            }
        }

        private bool IsVisible(GalleryProject project, int visibleCount)
        {
            int index = Projects.IndexOf(project);
            return index >= PageStart && index < PageStart + visibleCount;
        }

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

        public void Clear()
        {
            foreach (GalleryProject project in Projects) project.Dispose();
            Projects.Clear();
            SelectedIndex = -1;
            PageStart = 0;
        }

        public void Dispose() => Clear();
    }
}
