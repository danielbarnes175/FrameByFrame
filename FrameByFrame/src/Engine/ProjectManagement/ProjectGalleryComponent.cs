using System;
using System.Collections.Generic;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Projects
{
    internal sealed class ProjectGalleryComponent
    {
        private readonly ProjectGalleryState _state;
        public List<Rectangle> CardBounds { get; } = new();

        public ProjectGalleryComponent(ProjectGalleryState state) =>
            _state = state ?? throw new ArgumentNullException(nameof(state));

        public void Arrange(Rectangle bounds, int itemCount, int columns, int gap)
        {
            CardBounds.Clear();
            CardBounds.AddRange(UILayoutEngine.Grid(bounds, itemCount, columns, gap));
        }

        public bool UpdateSelection()
        {
            int visibleCount = Math.Min(CardBounds.Count,
                Math.Max(0, _state.Projects.Count - _state.PageStart));
            for (int i = 0; i < visibleCount; i++)
            {
                if (!UIPointerRouter.Clicked(CardBounds[i])) continue;
                _state.Select(_state.PageStart + i);
                return true;
            }
            return false;
        }

        public void Draw()
        {
            int S(int value) => UILayoutEngine.Scale(value);
            int visibleCount = Math.Min(CardBounds.Count,
                Math.Max(0, _state.Projects.Count - _state.PageStart));
            for (int i = 0; i < visibleCount; i++)
            {
                int projectIndex = _state.PageStart + i;
                GalleryProject project = _state.Projects[projectIndex];
                FbfProjectFile.ProjectMetadata metadata = project.Metadata;
                Rectangle card = CardBounds[i];
                bool selected = projectIndex == _state.SelectedIndex;
                UIRenderer.Fill(card, selected ? UITheme.SurfaceRaised : UITheme.Surface);
                UIRenderer.Border(card, selected ? UITheme.Primary : UITheme.Border, selected ? 3 : 2);
                int padding = Math.Max(4, S(10));
                int detailsHeight = Math.Min(S(66), Math.Max(S(42), card.Height / 3));
                Rectangle previewArea = new(card.X + padding, card.Y + padding,
                    Math.Max(1, card.Width - padding * 2),
                    Math.Max(1, card.Height - detailsHeight - padding * 2));
                Rectangle preview = UILayoutEngine.FitAspect(previewArea, metadata.Width / (float)metadata.Height);
                project.Thumbnail?.DrawPreview(preview, 1f,
                    metadata.IsBackgroundTransparent, metadata.BackgroundColor);
                if (project.Thumbnail == null)
                    new UITextContainer { Bounds = preview, MaxLines = 1 }.Draw(
                        project.ThumbnailFailed ? "Preview unavailable" : "Loading preview...",
                        UITheme.TextMuted, UITextRole.Caption);
                else _state.Touch(project);
                UIRenderer.Border(preview, selected ? UITheme.Secondary : UITheme.Border, 2);
                Rectangle nameBounds = new(card.X + padding, previewArea.Bottom + S(3),
                    Math.Max(1, card.Width - padding * 2), Math.Max(20, detailsHeight / 2));
                new UITextContainer { Bounds = nameBounds, HorizontalAlignment = UIAlign.Start,
                    MaxLines = 1, Padding = 2 }.Draw(metadata.ProjectName, UITheme.Text, UITextRole.Label);
                Rectangle details = new(nameBounds.X, nameBounds.Bottom, nameBounds.Width,
                    Math.Max(18, card.Bottom - padding - nameBounds.Bottom));
                new UITextContainer { Bounds = details, HorizontalAlignment = UIAlign.Start,
                    MaxLines = 1, Padding = 2 }.Draw(
                    $"{metadata.FrameCount} frames | {metadata.FramesPerSecond} fps",
                    UITheme.TextMuted, UITextRole.Caption);
            }
        }
    }
}
