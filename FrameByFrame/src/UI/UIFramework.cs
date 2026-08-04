using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FrameByFrame.src.Engine;
using FrameByFrame.src.Engine.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FrameByFrame.src.UI
{
    public static class UITheme
    {
        public static readonly Color Background = new(250, 248, 244);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceRaised = new(255, 244, 229);
        public static readonly Color CanvasStage = new(239, 235, 228);
        public static readonly Color Primary = new(238, 112, 24);
        public static readonly Color PrimaryHover = new(255, 137, 52);
        public static readonly Color Secondary = new(255, 184, 107);
        public static readonly Color Accent = new(238, 112, 24);
        public static readonly Color ToolSelected = new(48, 132, 232);
        public static readonly Color ToolSelectedSurface = new(222, 238, 255);
        public static readonly Color Text = new(43, 38, 34);
        public static readonly Color TextMuted = new(108, 97, 88);
        public static readonly Color Border = new(222, 211, 200);
        public static readonly Color Danger = new(194, 64, 48);

        public const int SpaceXs = 4;
        public const int SpaceSm = 8;
        public const int SpaceMd = 16;
        public const int SpaceLg = 24;
        public const int ControlHeight = 40;
        public const int AppBarHeight = 64;
        public const int ToolRailWidth = 176;
        public const int InspectorWidth = 220;
        public const int TimelineHeight = 150;
        public const int MinWidth = 1024;
        public const int MinHeight = 720;
    }

    public enum UIAxis { Horizontal, Vertical }
    public enum UIAlign { Start, Center, End, Stretch }

    public readonly record struct UIBox(Rectangle Bounds, int Padding = 0)
    {
        public Rectangle Content => new(
            Bounds.X + Padding, Bounds.Y + Padding,
            Math.Max(0, Bounds.Width - Padding * 2), Math.Max(0, Bounds.Height - Padding * 2));
    }

    public sealed class UITextContainer
    {
        public Rectangle Bounds { get; set; }
        public int Padding { get; set; } = UITheme.SpaceSm;
        public UIAlign HorizontalAlignment { get; set; } = UIAlign.Center;
        public UIAlign VerticalAlignment { get; set; } = UIAlign.Center;
        public bool Wrap { get; set; } = true;
        public int MaxLines { get; set; }

        public Rectangle ContentBounds => new UIBox(Bounds, Padding).Content;

        public void Draw(string value, Color color, float scale = 1f)
        {
            UIRenderer.TextInContainer(value, this, color, scale);
        }
    }

    public static class UILayoutEngine
    {
        public static float ResponsiveScale => Math.Clamp(
            Math.Min(GlobalParameters.screenWidth / (float)UIConstants.DEFAULT_SCREEN_WIDTH,
                GlobalParameters.screenHeight / (float)UIConstants.DEFAULT_SCREEN_HEIGHT),
            .72f, 1.3f);

        public static int Scale(int value) => Math.Max(1, (int)Math.Round(value * ResponsiveScale));

        public static IReadOnlyList<Rectangle> Stack(Rectangle bounds, UIAxis axis, int count, int spacing = UITheme.SpaceSm)
        {
            List<Rectangle> result = new(count);
            if (count <= 0) return result;
            int available = (axis == UIAxis.Horizontal ? bounds.Width : bounds.Height) - spacing * (count - 1);
            int itemSize = Math.Max(0, available / count);
            for (int i = 0; i < count; i++)
            {
                result.Add(axis == UIAxis.Horizontal
                    ? new Rectangle(bounds.X + i * (itemSize + spacing), bounds.Y, itemSize, bounds.Height)
                    : new Rectangle(bounds.X, bounds.Y + i * (itemSize + spacing), bounds.Width, itemSize));
            }
            return result;
        }

        public static Rectangle FitAspect(Rectangle bounds, float aspect)
        {
            int width = bounds.Width;
            int height = (int)(width / aspect);
            if (height > bounds.Height)
            {
                height = bounds.Height;
                width = (int)(height * aspect);
            }
            return new Rectangle(bounds.Center.X - width / 2, bounds.Center.Y - height / 2, width, height);
        }
    }

    public static class UIRenderer
    {
        private static Texture2D Pixel => TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, Color.White, 1);
        private static SpriteFont Font => GlobalParameters.uiFont ?? GlobalParameters.font;

        public static void Fill(Rectangle bounds, Color color) =>
            GlobalParameters.GlobalSpriteBatch.Draw(Pixel, bounds, color);

        public static void Border(Rectangle bounds, Color color, int thickness = 1)
        {
            Fill(new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
            Fill(new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
            Fill(new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
            Fill(new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
        }

        public static void Text(string value, Vector2 position, Color? color = null, float scale = 1f) =>
            GlobalParameters.GlobalSpriteBatch.DrawString(Font, value, position, color ?? UITheme.Text,
                0f, Vector2.Zero, Math.Max(.55f, scale), SpriteEffects.None, 0f);

        public static void CenteredText(string value, Rectangle bounds, Color? color = null, float scale = 1f)
        {
            scale = Math.Max(.55f, scale);
            Vector2 size = Font.MeasureString(value) * scale;
            Text(value, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), color, scale);
        }

        public static void TextInContainer(string value, UITextContainer container, Color color, float scale = 1f)
        {
            scale = Math.Max(.55f, scale);
            Rectangle content = container.ContentBounds;
            List<string> lines = container.Wrap
                ? WrapText(value ?? string.Empty, content.Width, scale)
                : new List<string> { value ?? string.Empty };
            int lineHeight = (int)Math.Ceiling(Font.LineSpacing * scale);
            int capacity = Math.Max(1, content.Height / Math.Max(1, lineHeight));
            int maxLines = container.MaxLines > 0 ? Math.Min(container.MaxLines, capacity) : capacity;
            if (lines.Count > maxLines)
            {
                lines = lines.Take(maxLines).ToList();
                lines[^1] = Ellipsize(lines[^1], content.Width, scale);
            }

            int blockHeight = lines.Count * lineHeight;
            float y = container.VerticalAlignment switch
            {
                UIAlign.End => content.Bottom - blockHeight,
                UIAlign.Center => content.Y + (content.Height - blockHeight) / 2f,
                _ => content.Y
            };

            foreach (string line in lines)
            {
                float width = Font.MeasureString(line).X * scale;
                float x = container.HorizontalAlignment switch
                {
                    UIAlign.End => content.Right - width,
                    UIAlign.Center => content.X + (content.Width - width) / 2f,
                    _ => content.X
                };
                Text(line, new Vector2(x, y), color, scale);
                y += lineHeight;
            }
        }

        public static List<string> WrapText(string value, int maxWidth, float scale = 1f)
        {
            List<string> lines = new();
            if (maxWidth <= 0) return lines;
            foreach (string paragraph in value.Replace("\r", string.Empty).Split('\n'))
            {
                string current = string.Empty;
                foreach (string rawWord in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    foreach (string word in BreakWord(rawWord, maxWidth, scale))
                    {
                        string candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
                        if (Font.MeasureString(candidate).X * scale <= maxWidth) current = candidate;
                        else
                        {
                            if (!string.IsNullOrEmpty(current)) lines.Add(current);
                            current = word;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(current)) lines.Add(current);
                else if (paragraph.Length == 0) lines.Add(string.Empty);
            }
            return lines;
        }

        private static IEnumerable<string> BreakWord(string word, int maxWidth, float scale)
        {
            if (Font.MeasureString(word).X * scale <= maxWidth) { yield return word; yield break; }
            StringBuilder part = new();
            foreach (char character in word)
            {
                if (part.Length > 0 && Font.MeasureString(part.ToString() + character).X * scale > maxWidth)
                {
                    yield return part.ToString(); part.Clear();
                }
                part.Append(character);
            }
            if (part.Length > 0) yield return part.ToString();
        }

        private static string Ellipsize(string value, int maxWidth, float scale)
        {
            const string suffix = "...";
            while (value.Length > 0 && Font.MeasureString(value + suffix).X * scale > maxWidth)
                value = value[..^1];
            return value + suffix;
        }
    }

    public static class UIPointerRouter
    {
        private static readonly List<Rectangle> BlockingRegions = new();
        public static bool IsCaptured { get; private set; }

        public static void BeginFrame()
        {
            BlockingRegions.Clear();
            if (!GlobalParameters.GlobalMouse.LeftClickHold()) IsCaptured = false;
        }

        public static void Block(Rectangle bounds) => BlockingRegions.Add(bounds);
        public static bool ContainsPointer(Rectangle bounds) => bounds.Contains(GlobalParameters.GlobalMouse.newMousePos);
        public static bool Clicked(Rectangle bounds)
        {
            Block(bounds);
            if (!ContainsPointer(bounds) || !GlobalParameters.GlobalMouse.LeftClick()) return false;
            IsCaptured = true;
            return true;
        }

        public static bool IsPointerBlocked()
        {
            if (IsCaptured) return true;
            for (int i = BlockingRegions.Count - 1; i >= 0; i--)
                if (BlockingRegions[i].Contains(GlobalParameters.GlobalMouse.newMousePos)) return true;
            return false;
        }
    }

    public sealed class UIActionButton
    {
        public Rectangle Bounds { get; set; }
        public string Text { get; set; }
        public string Tooltip { get; set; }
        public Action OnClick { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsSelected { get; set; }
        public bool IsHovered => IsEnabled && UIPointerRouter.ContainsPointer(Bounds);

        public UIActionButton(string text, Action onClick = null) { Text = text; OnClick = onClick; }

        public void Update()
        {
            UIPointerRouter.Block(Bounds);
            if (IsEnabled && UIPointerRouter.Clicked(Bounds)) OnClick?.Invoke();
        }

        public void Draw(bool primary = false)
        {
            Color color = IsSelected ? UITheme.Secondary
                : IsHovered ? (primary ? UITheme.PrimaryHover : UITheme.SurfaceRaised)
                : primary ? UITheme.Primary : UITheme.Surface;
            UIRenderer.Fill(Bounds, color);
            UIRenderer.Border(Bounds, IsSelected ? UITheme.Secondary : UITheme.Border, IsSelected ? 2 : 1);
            new UITextContainer { Bounds = Bounds, Padding = UILayoutEngine.Scale(10), MaxLines = 2 }.Draw(
                Text, primary ? Color.White : UITheme.Text, .9f);
            if (IsHovered && !string.IsNullOrWhiteSpace(Tooltip))
            {
                Vector2 size = (GlobalParameters.uiFont ?? GlobalParameters.font).MeasureString(Tooltip) * .9f;
                Rectangle tip = new(Bounds.Right + 8, Bounds.Y, (int)size.X + 16, 34);
                if (tip.Right > GlobalParameters.screenWidth) tip.X = Bounds.X - tip.Width - 8;
                UIRenderer.Fill(tip, UITheme.SurfaceRaised);
                UIRenderer.Border(tip, UITheme.Border);
                UIRenderer.Text(Tooltip, new Vector2(tip.X + 8, tip.Y + 5), UITheme.Text, .9f);
            }
        }
    }
}
