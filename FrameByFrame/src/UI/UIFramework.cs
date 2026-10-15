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
        public const int DefaultScreenWidth = 1600;
        public const int DefaultScreenHeight = 900;
        public const int MinWidth = 320;
        public const int MinHeight = 480;

        public static UITextMetrics Typography(UITextRole role) => role switch
        {
            UITextRole.Display => new(1.45f, true),
            UITextRole.Title => new(1f, true),
            UITextRole.Heading => new(.82f, true),
            UITextRole.Button => new(.72f, true),
            UITextRole.Body => new(.76f, false),
            UITextRole.Label => new(.62f, true),
            UITextRole.Caption => new(.66f, false),
            UITextRole.Tooltip => new(.66f, false),
            UITextRole.Debug => new(.55f, false),
            _ => new(.76f, false)
        };
    }

    public enum UIAxis { Horizontal, Vertical }
    public enum UIAlign { Start, Center, End, Stretch }
    public enum UITextRole { Display, Title, Heading, Button, Body, Label, Caption, Tooltip, Debug }
    public readonly record struct UITextMetrics(float Scale, bool Bold);

    public abstract class UIElement
    {
        public Rectangle Bounds { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;

        public virtual Point Measure(Point available) => new(
            Math.Min(Bounds.Width, available.X), Math.Min(Bounds.Height, available.Y));

        public virtual void Arrange(Rectangle bounds) => Bounds = bounds;
        public virtual void Update() { }
        public virtual void Draw() { }
    }

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

        public void Draw(string value, Color color, UITextRole role = UITextRole.Body)
        {
            UIRenderer.TextInContainer(value, this, color, role);
        }
    }

    public static class UILayoutEngine
    {
        public static float ResponsiveScale => Math.Clamp(
            Math.Min(GlobalParameters.screenWidth / (float)UITheme.DefaultScreenWidth,
                GlobalParameters.screenHeight / (float)UITheme.DefaultScreenHeight),
            .45f, 1.3f);

        public static int Scale(int value) => Math.Max(1, (int)Math.Round(value * ResponsiveScale));

        public static Rectangle InsetScreen(int margin)
        {
            int inset = Math.Min(margin, Math.Max(0, Math.Min(GlobalParameters.screenWidth, GlobalParameters.screenHeight) / 2 - 1));
            return new Rectangle(inset, inset, Math.Max(1, GlobalParameters.screenWidth - inset * 2),
                Math.Max(1, GlobalParameters.screenHeight - inset * 2));
        }

        public static Rectangle CenteredInScreen(int preferredWidth, int preferredHeight, int margin = UITheme.SpaceMd)
        {
            Rectangle available = InsetScreen(margin);
            int width = Math.Min(preferredWidth, available.Width);
            int height = Math.Min(preferredHeight, available.Height);
            return new Rectangle(available.Center.X - width / 2, available.Center.Y - height / 2, width, height);
        }

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

        public static IReadOnlyList<Rectangle> Grid(Rectangle bounds, int count, int columns,
            int spacing = UITheme.SpaceMd)
        {
            List<Rectangle> result = new(Math.Max(0, count));
            if (count <= 0 || columns <= 0) return result;
            columns = Math.Min(columns, count);
            int rows = (count + columns - 1) / columns;
            int cellWidth = Math.Max(1, (bounds.Width - spacing * (columns - 1)) / columns);
            int cellHeight = Math.Max(1, (bounds.Height - spacing * (rows - 1)) / rows);
            for (int i = 0; i < count; i++)
            {
                int row = i / columns;
                int column = i % columns;
                result.Add(new Rectangle(
                    bounds.X + column * (cellWidth + spacing),
                    bounds.Y + row * (cellHeight + spacing),
                    cellWidth,
                    cellHeight));
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

        public static Rectangle PlaceFloatingPanel(Rectangle anchor, Point requestedSize,
            Rectangle viewport, bool preferBelow)
        {
            int width = Math.Clamp(requestedSize.X, 1, Math.Max(1, viewport.Width));
            int height = Math.Clamp(requestedSize.Y, 1, Math.Max(1, viewport.Height));
            int x = Math.Clamp(anchor.X, viewport.X, Math.Max(viewport.X, viewport.Right - width));
            int preferredY = preferBelow ? anchor.Bottom + UITheme.SpaceSm : anchor.Y;
            int alternateY = preferBelow
                ? anchor.Y - height - UITheme.SpaceSm
                : anchor.Bottom + UITheme.SpaceSm;
            int y = preferredY >= viewport.Y && preferredY + height <= viewport.Bottom
                ? preferredY
                : alternateY;
            y = Math.Clamp(y, viewport.Y, Math.Max(viewport.Y, viewport.Bottom - height));
            return new Rectangle(x, y, width, height);
        }
    }

    public static class UIRenderer
    {
        private static Texture2D Pixel => TextureManager.GetOrCreateColorTexture(GlobalParameters.GlobalGraphics, Color.White, 1);
        private static SpriteFont Font(UITextRole role)
        {
            UITextMetrics metrics = UITheme.Typography(role);
            return metrics.Bold
                ? GlobalParameters.uiFont ?? GlobalParameters.font
                : GlobalParameters.font ?? GlobalParameters.uiFont;
        }

        public static void Fill(Rectangle bounds, Color color) =>
            GlobalParameters.GlobalSpriteBatch.Draw(Pixel, bounds, color);

        public static void Line(Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length <= 0f) return;
            GlobalParameters.GlobalSpriteBatch.Draw(Pixel, start, null, color,
                (float)Math.Atan2(delta.Y, delta.X), Vector2.Zero,
                new Vector2(length, Math.Max(1f, thickness)), SpriteEffects.None, 0f);
        }

        public static void Border(Rectangle bounds, Color color, int thickness = 1)
        {
            Fill(new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
            Fill(new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
            Fill(new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
            Fill(new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
        }

        public static void Text(string value, Vector2 position, Color? color = null,
            UITextRole role = UITextRole.Body)
        {
            UITextMetrics metrics = UITheme.Typography(role);
            GlobalParameters.GlobalSpriteBatch.DrawString(Font(role), value ?? string.Empty, position,
                color ?? UITheme.Text, 0f, Vector2.Zero, metrics.Scale, SpriteEffects.None, 0f);
        }

        public static void CenteredText(string value, Rectangle bounds, Color? color = null,
            UITextRole role = UITextRole.Body)
        {
            UITextMetrics metrics = UITheme.Typography(role);
            Vector2 size = Font(role).MeasureString(value ?? string.Empty) * metrics.Scale;
            Text(value, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), color, role);
        }

        public static void TextInContainer(string value, UITextContainer container, Color color,
            UITextRole role = UITextRole.Body)
        {
            SpriteFont font = Font(role);
            float scale = UITheme.Typography(role).Scale;
            Rectangle content = container.ContentBounds;
            List<string> lines = container.Wrap
                ? WrapText(value ?? string.Empty, content.Width, font, scale)
                : new List<string> { value ?? string.Empty };
            int lineHeight = (int)Math.Ceiling(font.LineSpacing * scale);
            int capacity = Math.Max(1, content.Height / Math.Max(1, lineHeight));
            int maxLines = container.MaxLines > 0 ? Math.Min(container.MaxLines, capacity) : capacity;
            if (lines.Count > maxLines)
            {
                lines = lines.Take(maxLines).ToList();
                lines[^1] = Ellipsize(lines[^1], content.Width, font, scale);
            }
            else if (!container.Wrap && lines.Count == 1)
                lines[0] = Ellipsize(lines[0], content.Width, font, scale, onlyWhenNeeded: true);

            int blockHeight = lines.Count * lineHeight;
            float y = container.VerticalAlignment switch
            {
                UIAlign.End => content.Bottom - blockHeight,
                UIAlign.Center => content.Y + (content.Height - blockHeight) / 2f,
                _ => content.Y
            };

            foreach (string line in lines)
            {
                float width = font.MeasureString(line).X * scale;
                float x = container.HorizontalAlignment switch
                {
                    UIAlign.End => content.Right - width,
                    UIAlign.Center => content.X + (content.Width - width) / 2f,
                    _ => content.X
                };
                GlobalParameters.GlobalSpriteBatch.DrawString(font, line, new Vector2(x, y), color,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                y += lineHeight;
            }
        }

        private static List<string> WrapText(string value, int maxWidth, SpriteFont font, float scale)
        {
            List<string> lines = new();
            if (maxWidth <= 0) return lines;
            foreach (string paragraph in value.Replace("\r", string.Empty).Split('\n'))
            {
                string current = string.Empty;
                foreach (string rawWord in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    foreach (string word in BreakWord(rawWord, maxWidth, font, scale))
                    {
                        string candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
                        if (font.MeasureString(candidate).X * scale <= maxWidth) current = candidate;
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

        private static IEnumerable<string> BreakWord(string word, int maxWidth, SpriteFont font, float scale)
        {
            if (font.MeasureString(word).X * scale <= maxWidth) { yield return word; yield break; }
            StringBuilder part = new();
            foreach (char character in word)
            {
                if (part.Length > 0 && font.MeasureString(part.ToString() + character).X * scale > maxWidth)
                {
                    yield return part.ToString(); part.Clear();
                }
                part.Append(character);
            }
            if (part.Length > 0) yield return part.ToString();
        }

        private static string Ellipsize(string value, int maxWidth, SpriteFont font, float scale,
            bool onlyWhenNeeded = false)
        {
            const string suffix = "...";
            if (onlyWhenNeeded && font.MeasureString(value).X * scale <= maxWidth) return value;
            if (font.MeasureString(suffix).X * scale > maxWidth) return string.Empty;
            while (value.Length > 0 && font.MeasureString(value + suffix).X * scale > maxWidth)
                value = value[..^1];
            return value + suffix;
        }

        public static void Tooltip(string value, Rectangle anchor, bool preferBelow, Color border)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            UITextRole role = UITextRole.Tooltip;
            SpriteFont font = Font(role);
            float scale = UITheme.Typography(role).Scale;
            Rectangle viewport = new(8, 8, Math.Max(1, GlobalParameters.screenWidth - 16),
                Math.Max(1, GlobalParameters.screenHeight - 16));
            int width = Math.Min(420, viewport.Width);
            List<string> lines = WrapText(value, Math.Max(1, width - 16), font, scale);
            int lineHeight = (int)Math.Ceiling(font.LineSpacing * scale);
            int contentWidth = lines.Count == 0 ? 1 : (int)Math.Ceiling(lines.Max(line =>
                font.MeasureString(line).X * scale));
            width = Math.Min(width, Math.Max(40, contentWidth + 16));
            int height = Math.Max(30, lines.Count * lineHeight + 10);
            Rectangle bounds = UILayoutEngine.PlaceFloatingPanel(anchor, new Point(width, height),
                viewport, preferBelow);
            Fill(bounds, UITheme.Surface);
            Border(bounds, border, 1);
            new UITextContainer { Bounds = bounds, Padding = 8, HorizontalAlignment = UIAlign.Start,
                MaxLines = Math.Max(1, lines.Count) }.Draw(value, UITheme.Text, role);
        }
    }

    public static class UIPointerRouter
    {
        private static readonly List<Rectangle> BlockingRegions = new();
        private static readonly object ClickCapture = new();
        private static object _captureOwner;
        public static bool IsCaptured => _captureOwner != null;

        public static void BeginFrame()
        {
            BlockingRegions.Clear();
            if (!GlobalParameters.GlobalMouse.LeftClickHold()) _captureOwner = null;
        }

        public static void Block(Rectangle bounds) => BlockingRegions.Add(bounds);
        public static bool ContainsPointer(Rectangle bounds) => bounds.Contains(GlobalParameters.GlobalMouse.newMousePos);
        public static bool Clicked(Rectangle bounds)
        {
            Block(bounds);
            if (!ContainsPointer(bounds) || !GlobalParameters.GlobalMouse.LeftClick()) return false;
            _captureOwner ??= ClickCapture;
            return true;
        }

        public static bool Held(object owner, Rectangle bounds)
        {
            Block(bounds);
            if (ContainsPointer(bounds) && GlobalParameters.GlobalMouse.LeftClick()) _captureOwner = owner;
            return ReferenceEquals(_captureOwner, owner) && GlobalParameters.GlobalMouse.LeftClickHold();
        }

        public static bool IsPointerBlocked()
        {
            if (IsCaptured) return true;
            for (int i = BlockingRegions.Count - 1; i >= 0; i--)
                if (BlockingRegions[i].Contains(GlobalParameters.GlobalMouse.newMousePos)) return true;
            return false;
        }
    }

    public sealed class UIActionButton : UIElement
    {
        public string Text { get; set; }
        public string Tooltip { get; set; }
        public Action OnClick { get; set; }
        public bool IsSelected { get; set; }
        public bool IsHovered => IsEnabled && UIPointerRouter.ContainsPointer(Bounds);

        public UIActionButton(string text, Action onClick = null) { Text = text; OnClick = onClick; }

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            if (IsEnabled && UIPointerRouter.Clicked(Bounds)) OnClick?.Invoke();
        }

        public void Draw(bool primary = false, bool drawTooltip = true)
        {
            Color color = IsSelected ? UITheme.Secondary
                : IsHovered ? (primary ? UITheme.PrimaryHover : UITheme.SurfaceRaised)
                : primary ? UITheme.Primary : UITheme.Surface;
            UIRenderer.Fill(Bounds, color);
            UIRenderer.Border(Bounds, IsSelected ? UITheme.Secondary : UITheme.Border, IsSelected ? 2 : 1);
            new UITextContainer { Bounds = Bounds, Padding = UILayoutEngine.Scale(10), MaxLines = 2 }.Draw(
                Text, primary ? Color.White : UITheme.Text, UITextRole.Button);
            if (drawTooltip) DrawTooltip();
        }

        public void DrawTooltip()
        {
            if (UIPointerRouter.ContainsPointer(Bounds) && !string.IsNullOrWhiteSpace(Tooltip))
                UIRenderer.Tooltip(Tooltip, new Rectangle(Bounds.Right + 8, Bounds.Y, 1, Bounds.Height),
                    preferBelow: false, border: UITheme.Border);
        }

        public override void Draw() => Draw(false);
    }

    public sealed class UIIconButton : UIElement
    {
        public Texture2D Icon { get; }
        public Action OnClick { get; set; }
        public string Tooltip { get; set; }
        public bool IsSelected { get; set; }
        public bool UseToolSelectionStyle { get; set; }

        public UIIconButton(Texture2D icon, Action onClick = null)
        {
            Icon = icon;
            OnClick = onClick;
        }

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            if (IsEnabled && UIPointerRouter.Clicked(Bounds)) OnClick?.Invoke();
        }

        public void Draw(bool drawTooltip)
        {
            Rectangle visual = Bounds;
            bool hovered = IsEnabled && UIPointerRouter.ContainsPointer(Bounds);
            if (UseToolSelectionStyle && IsSelected)
            {
                visual.Y -= 2;
                UIRenderer.Fill(new Rectangle(Bounds.X + 1, Bounds.Y + 1, Bounds.Width, Bounds.Height), new Color(0, 0, 0, 28));
                UIRenderer.Fill(visual, UITheme.ToolSelectedSurface);
                UIRenderer.Border(visual, UITheme.ToolSelected, 2);
            }
            GlobalParameters.GlobalSpriteBatch.Draw(Icon, visual, hovered && !IsSelected ? Color.White * .72f : Color.White);
            if (drawTooltip) DrawTooltip();
        }

        public void DrawTooltip()
        {
            if (!IsEnabled || !UIPointerRouter.ContainsPointer(Bounds) || string.IsNullOrWhiteSpace(Tooltip)) return;
            UIRenderer.Tooltip(Tooltip, Bounds, preferBelow: true, border: UITheme.Primary);
        }

        public override void Draw() => Draw(true);
    }

    public sealed class UIToggle : UIElement
    {
        public bool Value { get; set; }
        public Action<bool> OnChanged { get; set; }

        public UIToggle(bool initialValue, Action<bool> onChanged = null)
        {
            Value = initialValue;
            OnChanged = onChanged;
        }

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            if (!IsEnabled || !UIPointerRouter.Clicked(Bounds)) return;
            Value = !Value;
            OnChanged?.Invoke(Value);
        }

        public override void Draw()
        {
            UIRenderer.Fill(Bounds, Value ? UITheme.Primary : UITheme.SurfaceRaised);
            UIRenderer.Border(Bounds, Value ? UITheme.Primary : UITheme.Border, 2);
            int knob = Math.Max(8, Bounds.Height - 8);
            int x = Value ? Bounds.Right - knob - 4 : Bounds.X + 4;
            UIRenderer.Fill(new Rectangle(x, Bounds.Y + 4, knob, knob), Color.White);
        }
    }

    public sealed class UISlider : UIElement
    {
        public int Minimum { get; }
        public int Maximum { get; }
        public int Value { get; private set; }
        public Action<int> OnChanged { get; set; }

        public UISlider(int minimum, int maximum, int value, Action<int> onChanged = null)
        {
            Minimum = minimum;
            Maximum = Math.Max(minimum + 1, maximum);
            Value = Math.Clamp(value, Minimum, Maximum);
            OnChanged = onChanged;
        }

        public void SetValue(int value) => Value = Math.Clamp(value, Minimum, Maximum);

        public override void Update()
        {
            UIPointerRouter.Block(Bounds);
            if (!IsEnabled || !UIPointerRouter.Held(this, Bounds)) return;
            float ratio = Math.Clamp((GlobalParameters.GlobalMouse.newMousePos.X - Bounds.X - 8) /
                Math.Max(1f, Bounds.Width - 16f), 0f, 1f);
            int next = Minimum + (int)Math.Round(ratio * (Maximum - Minimum));
            if (next == Value) return;
            Value = next;
            OnChanged?.Invoke(Value);
        }

        public override void Draw()
        {
            UIRenderer.Fill(Bounds, UITheme.SurfaceRaised);
            UIRenderer.Border(Bounds, UITheme.Border);
            Rectangle track = new(Bounds.X + 8, Bounds.Bottom - 13, Math.Max(1, Bounds.Width - 16), 4);
            UIRenderer.Fill(track, UITheme.Border);
            float ratio = (Value - Minimum) / (float)(Maximum - Minimum);
            int x = track.X + (int)Math.Round(track.Width * ratio);
            UIRenderer.Fill(new Rectangle(track.X, track.Y, Math.Max(1, x - track.X), track.Height), UITheme.Primary);
            UIRenderer.Fill(new Rectangle(x - 6, track.Y - 5, 12, 14), Color.White);
            UIRenderer.Border(new Rectangle(x - 6, track.Y - 5, 12, 14), UITheme.Primary);
            new UITextContainer { Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height - 12), MaxLines = 1 }
                .Draw(Value.ToString(), UITheme.Text, UITextRole.Label);
        }
    }

    public class UIPopover : UIElement
    {
        public bool IsOpen { get; set; }
        public bool ShowOutline { get; set; } = true;

        public override void Update()
        {
            if (IsOpen) UIPointerRouter.Block(Bounds);
        }

        public override void Draw()
        {
            if (!IsOpen) return;
            UIRenderer.Fill(Bounds, UITheme.Surface);
            if (ShowOutline) UIRenderer.Border(Bounds, UITheme.Primary, 3);
        }
    }
}
