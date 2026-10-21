using System;
using FrameByFrame.src.Engine.Export;
using Microsoft.Xna.Framework.Input;

namespace FrameByFrame.src.Engine.Projects
{
    internal sealed class ProjectRenameDialogState
    {
        public bool IsOpen { get; private set; }
        public string Text { get; private set; } = string.Empty;
        public string Error { get; private set; } = string.Empty;

        public void Open(string projectName)
        {
            Text = projectName ?? string.Empty;
            Error = string.Empty;
            IsOpen = true;
        }

        public void Cancel()
        {
            IsOpen = false;
            Error = string.Empty;
        }

        public bool Confirm(Func<string, string?> rename)
        {
            ArgumentNullException.ThrowIfNull(rename);
            try
            {
                string? error = rename(Text);
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                Cancel();
                return true;
            }
            catch (Exception ex)
            {
                Error = $"{ex.Message} Choose a unique name without path characters, then try again.";
                return false;
            }
        }

        public RenameInputResult ApplyKeys(Keys[] keys, bool shift)
        {
            foreach (Keys key in keys)
            {
                if (key == Keys.Enter) return RenameInputResult.Confirm;
                if (key == Keys.Escape) return RenameInputResult.Cancel;
                if (key == Keys.Back && Text.Length > 0) { Text = Text[..^1]; continue; }
                char? character = KeyToCharacter(key, shift);
                if (character.HasValue && Text.Length < 80) Text += character.Value;
            }
            return RenameInputResult.None;
        }

        internal static char? KeyToCharacter(Keys key, bool shift)
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
    }

    internal enum RenameInputResult { None, Confirm, Cancel }

    internal sealed class ProjectExportDialogState
    {
        public bool IsOpen { get; private set; }
        public int StartFrame { get; private set; }
        public int EndFrame { get; private set; }
        public int FrameCount { get; private set; }
        public ExportFormat Format { get; private set; } = ExportFormat.Gif;
        public bool ReplaceExisting { get; private set; }
        public string Directory { get; private set; } = SaveService.ProjectsDirectory;
        public string Message { get; private set; } = string.Empty;

        public void Open(int frameCount)
        {
            FrameCount = Math.Max(1, frameCount);
            StartFrame = 0;
            EndFrame = FrameCount - 1;
            ReplaceExisting = false;
            Message = string.Empty;
            IsOpen = true;
        }

        public void Close() => IsOpen = false;
        public void SetStart(int value) => StartFrame = Math.Clamp(value, 0, EndFrame);
        public void SetEnd(int value) => EndFrame = Math.Clamp(value, StartFrame, FrameCount - 1);
        public void SelectFormat(int delta)
        {
            int count = Enum.GetValues<ExportFormat>().Length;
            Format = (ExportFormat)(((int)Format + delta + count) % count);
        }
        public void ToggleReplace() => ReplaceExisting = !ReplaceExisting;
        public void SetDirectory(string directory)
        {
            Directory = System.IO.Path.GetFullPath(directory);
            Message = string.Empty;
        }
        public void SetError(string message) => Message = message ?? string.Empty;
    }
}
