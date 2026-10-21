using System;
using FrameByFrame.src.Engine.Animation;
using FrameByFrame.src.Engine.Export;

namespace FrameByFrame.src.UI.Components
{
    internal sealed class NavbarSaveCoordinator
    {
        private readonly Animation _animation;
        private readonly AutosaveService _autosave;
        private readonly UIFeedbackState? _feedback;
        private readonly Action<Animation> _save;

        public bool IsPending { get; private set; }
        public string Error { get; private set; } = string.Empty;

        public NavbarSaveCoordinator(Animation animation, AutosaveService autosave,
            UIFeedbackState? feedback, Action<Animation>? save = null)
        {
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));
            _autosave = autosave ?? throw new ArgumentNullException(nameof(autosave));
            _feedback = feedback;
            _save = save ?? SaveService.SaveAnimation;
        }

        public void Request()
        {
            if (IsPending) return;
            IsPending = true;
            _feedback?.ShowBusy("Saving project", $"Saving {_animation.projectName}...");
            if (_feedback == null) Process();
        }

        public bool Process()
        {
            if (!IsPending) return true;
            IsPending = false;
            int previousThumbnailFrameIndex = _animation.ThumbnailFrameIndex;
            try
            {
                _autosave.Flush();
                _animation.CaptureThumbnailFrame();
                _save(_animation);
                Error = string.Empty;
                _feedback?.ShowSuccess("Project saved", $"{_animation.projectName} was saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _animation.RestoreThumbnailFrame(previousThumbnailFrameIndex);
                Error = ex.Message;
                _feedback?.ShowError("Project could not be saved", ex.Message,
                    "Check the project name, available disk space, and Projects folder permissions, then try again.");
                return false;
            }
        }

        public void SetError(string error) => Error = error ?? string.Empty;
    }
}
