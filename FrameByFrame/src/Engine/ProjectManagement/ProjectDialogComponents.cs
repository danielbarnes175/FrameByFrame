using System;
using FrameByFrame.src.Engine.Export;
using FrameByFrame.src.UI;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Projects
{
    internal static class ProjectRenameDialogComponent
    {
        public static void Draw(IApplicationContext app, ProjectRenameDialogState state, Rectangle input,
            UIActionButton confirm, UIActionButton cancel)
        {
            ArgumentNullException.ThrowIfNull(app);
            int S(int value) => UILayoutEngine.Scale(value);
            Rectangle dialog = new(app.ScreenWidth / 2 - S(260),
                app.ScreenHeight / 2 - S(120), S(520), S(270));
            UIRenderer.Fill(dialog, UITheme.SurfaceRaised);
            UIRenderer.Border(dialog, UITheme.Primary, 3);
            new UITextContainer { Bounds = new Rectangle(dialog.X + S(24), dialog.Y + S(18),
                dialog.Width - S(48), S(42)), MaxLines = 1 }
                .Draw("Rename project", UITheme.Primary, UITextRole.Heading);
            UIRenderer.Fill(input, UITheme.Surface);
            UIRenderer.Border(input, UITheme.Border, 2);
            string display = string.IsNullOrEmpty(state.Text) ? "Type a project name" : state.Text + "|";
            new UITextContainer { Bounds = input, HorizontalAlignment = UIAlign.Start,
                Padding = S(12), MaxLines = 1 }.Draw(display,
                string.IsNullOrEmpty(state.Text) ? UITheme.TextMuted : UITheme.Text, UITextRole.Body);
            if (!string.IsNullOrEmpty(state.Error))
                new UITextContainer { Bounds = new Rectangle(dialog.X + S(24), input.Bottom + S(4),
                    dialog.Width - S(48), S(34)), MaxLines = 1 }
                    .Draw(state.Error, Color.IndianRed, UITextRole.Caption);
            confirm.Draw(true);
            cancel.Draw();
        }
    }

    internal static class ProjectExportDialogComponent
    {
        public static void Draw(IApplicationContext app, ProjectExportDialogState state,
            UIActionButton formatPrevious, UIActionButton formatNext,
            UIActionButton startDown, UIActionButton startUp,
            UIActionButton endDown, UIActionButton endUp,
            UIActionButton replaceExisting, UIActionButton chooseDirectory,
            UIActionButton confirm, UIActionButton cancel)
        {
            ArgumentNullException.ThrowIfNull(app);
            int S(int value) => UILayoutEngine.Scale(value);
            int cx = app.ScreenWidth / 2;
            int cy = app.ScreenHeight / 2;
            Rectangle dialog = new(cx - S(260), cy - S(240), S(520), S(500));
            UIRenderer.Fill(dialog, UITheme.SurfaceRaised);
            UIRenderer.Border(dialog, UITheme.Primary, 3);
            new UITextContainer { Bounds = new Rectangle(dialog.X + S(24), dialog.Y + S(18),
                dialog.Width - S(48), S(42)), MaxLines = 1 }
                .Draw("Export frame range", UITheme.Primary, UITextRole.Heading);
            Label(new Rectangle(cx - S(210), cy - S(130), S(150), S(42)), "Format");
            Value(new Rectangle(cx + S(10), cy - S(130), S(90), S(42)), FormatLabel(state.Format));
            formatPrevious.Draw(); formatNext.Draw();
            Label(new Rectangle(cx - S(210), cy - S(75), S(150), S(42)), "First frame");
            Value(new Rectangle(cx + S(10), cy - S(75), S(90), S(42)), (state.StartFrame + 1).ToString());
            startDown.Draw(); startUp.Draw();
            Label(new Rectangle(cx - S(210), cy - S(20), S(150), S(42)), "Last frame");
            Value(new Rectangle(cx + S(10), cy - S(20), S(90), S(42)), (state.EndFrame + 1).ToString());
            endDown.Draw(); endUp.Draw();
            new UITextContainer { Bounds = new Rectangle(cx - S(220), cy + S(70), S(440), S(24)),
                HorizontalAlignment = UIAlign.Start, MaxLines = 1 }.Draw(
                string.IsNullOrEmpty(state.Message) ? state.Directory : state.Message,
                string.IsNullOrEmpty(state.Message) ? UITheme.TextMuted : UITheme.Danger, UITextRole.Caption);
            replaceExisting.Draw(); chooseDirectory.Draw(); confirm.Draw(true); cancel.Draw();
        }

        private static void Label(Rectangle bounds, string text) =>
            new UITextContainer { Bounds = bounds, HorizontalAlignment = UIAlign.Start, MaxLines = 1 }
                .Draw(text, UITheme.Text, UITextRole.Body);

        private static void Value(Rectangle bounds, string text) =>
            new UITextContainer { Bounds = bounds, MaxLines = 1 }.Draw(text, UITheme.Primary, UITextRole.Label);

        private static string FormatLabel(ExportFormat format) => format switch
        {
            ExportFormat.PngSequence => "PNG files",
            ExportFormat.SpriteSheet => "Spritesheet",
            _ => format.ToString().ToUpperInvariant()
        };
    }
}
