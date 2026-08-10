using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Animation
{
    /// <summary>Owns the ordered frames, current selection, clipboard, and playback state.</summary>
    internal sealed class Timeline : IDisposable
    {
        private readonly Func<Vector2> _framePosition;
        private readonly Func<Vector2> _frameSize;
        private readonly Func<IReadOnlyList<AnimationLayer>> _layers;
        private readonly Action _beforeSelectionChange;
        private readonly Action _clearEditHistory;
        private readonly LinkedList<Frame> _frames = new();
        private readonly List<Frame> _frameCache = new();
        private LinkedListNode<Frame> _currentFrame;
        private Dictionary<Guid, Color[]> _clipboard;
        private bool _frameCacheValid = true;
        private double _playbackTimer;
        private bool _disposed;

        public Timeline(Func<Vector2> framePosition, Func<Vector2> frameSize,
            Func<IReadOnlyList<AnimationLayer>> layers, Action beforeSelectionChange,
            Action clearEditHistory)
        {
            _framePosition = framePosition ?? throw new ArgumentNullException(nameof(framePosition));
            _frameSize = frameSize ?? throw new ArgumentNullException(nameof(frameSize));
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            _beforeSelectionChange = beforeSelectionChange ?? throw new ArgumentNullException(nameof(beforeSelectionChange));
            _clearEditHistory = clearEditHistory ?? throw new ArgumentNullException(nameof(clearEditHistory));
        }

        public IEnumerable<Frame> Frames => _frames;
        public int TotalFrames => _frames.Count;
        public int CurrentFrameIndex { get; private set; }
        public Frame CurrentFrame => _currentFrame?.Value;
        public bool IsPlaying { get; private set; }

        public void Initialize()
        {
            if (_frames.Count != 0) return;
            _frames.AddLast(CreateFrame());
            _currentFrame = _frames.First;
            CurrentFrameIndex = 0;
            InvalidateFrameCache();
        }

        public void Load(IEnumerable<Frame> loadedFrames)
        {
            ArgumentNullException.ThrowIfNull(loadedFrames);
            List<Frame> replacements = loadedFrames.Where(frame => frame != null).ToList();
            if (replacements.Count == 0)
                throw new ArgumentException("A saved animation must contain at least one frame.", nameof(loadedFrames));

            foreach (Frame frame in _frames) frame.Dispose();
            _frames.Clear();
            foreach (Frame frame in replacements) _frames.AddLast(frame);
            _currentFrame = _frames.First;
            CurrentFrameIndex = 0;
            _playbackTimer = 0;
            IsPlaying = false;
            InvalidateFrameCache();
        }

        public Frame GetFrameAtIndex(int index)
        {
            if (index < 0 || index >= _frames.Count) return null;
            EnsureFrameCache();
            return _frameCache[index];
        }

        public bool Contains(Frame frame) => frame != null && _frames.Contains(frame);

        public int IndexOf(Frame frame)
        {
            if (frame == null) return -1;
            EnsureFrameCache();
            return _frameCache.IndexOf(frame);
        }

        public void FirstFrame()
        {
            _beforeSelectionChange();
            _currentFrame = _frames.First;
            CurrentFrameIndex = 0;
        }

        public void LastFrame()
        {
            _beforeSelectionChange();
            _currentFrame = _frames.Last;
            CurrentFrameIndex = Math.Max(0, TotalFrames - 1);
        }

        public void NextFrame()
        {
            _beforeSelectionChange();
            CurrentFrameIndex++;
            if (CurrentFrameIndex >= TotalFrames)
            {
                _frames.AddLast(CreateFrame());
                InvalidateFrameCache();
            }
            _currentFrame = _currentFrame?.Next ?? _frames.First;
        }

        public void PreviousFrame()
        {
            if (CurrentFrameIndex <= 0) return;
            _beforeSelectionChange();
            CurrentFrameIndex--;
            _currentFrame = _currentFrame.Previous;
        }

        public void SelectFrame(int index)
        {
            if (index < 0 || index >= TotalFrames) return;
            _beforeSelectionChange();
            _currentFrame = _frames.First;
            for (int i = 0; i < index; i++) _currentFrame = _currentFrame.Next;
            CurrentFrameIndex = index;
        }

        public void DeleteFrame()
        {
            if (_frames.Count <= 1) return;
            _beforeSelectionChange();
            LinkedListNode<Frame> removed = _currentFrame;
            bool hasNextFrame = removed.Next != null;
            _currentFrame = removed.Next ?? removed.Previous;
            removed.Value.Dispose();
            _frames.Remove(removed);
            if (!hasNextFrame) CurrentFrameIndex--;
            _clearEditHistory();
            InvalidateFrameCache();
        }

        public void InsertFrame()
        {
            _beforeSelectionChange();
            _frames.AddBefore(_currentFrame, CreateFrame());
            _currentFrame = _currentFrame.Previous;
            InvalidateFrameCache();
        }

        public void DuplicateCurrentFrame()
        {
            if (_currentFrame == null) return;
            _beforeSelectionChange();
            Frame duplicate = CreateFrame();
            foreach (AnimationLayer layer in _layers())
                duplicate.SetLayerPixels(layer.Id, CurrentFrame.GetLayerPixels(layer.Id), ignoreLock: true);
            _currentFrame = _frames.AddAfter(_currentFrame, duplicate);
            CurrentFrameIndex++;
            InvalidateFrameCache();
        }

        public void CopyCurrentFrame()
        {
            if (_currentFrame == null) return;
            _beforeSelectionChange();
            _clipboard = _layers().ToDictionary(layer => layer.Id,
                layer => CurrentFrame.GetLayerPixels(layer.Id));
        }

        public bool PasteFrame()
        {
            if (_currentFrame == null || _clipboard == null) return false;
            _beforeSelectionChange();
            Frame pastedFrame = CreateFrame();
            foreach (AnimationLayer layer in _layers())
            {
                if (_clipboard.TryGetValue(layer.Id, out Color[] pixels))
                    pastedFrame.SetLayerPixels(layer.Id, pixels, ignoreLock: true);
            }
            _currentFrame = _frames.AddAfter(_currentFrame, pastedFrame);
            CurrentFrameIndex++;
            InvalidateFrameCache();
            return true;
        }

        public bool MoveFrame(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= TotalFrames || newIndex < 0 ||
                newIndex >= TotalFrames || oldIndex == newIndex) return false;

            _beforeSelectionChange();
            LinkedListNode<Frame> moving = _frames.First;
            for (int i = 0; i < oldIndex; i++) moving = moving.Next;
            Frame selectedFrame = CurrentFrame;
            _frames.Remove(moving);
            if (newIndex >= _frames.Count) _frames.AddLast(moving);
            else
            {
                LinkedListNode<Frame> destination = _frames.First;
                for (int i = 0; i < newIndex; i++) destination = destination.Next;
                _frames.AddBefore(destination, moving);
            }

            _currentFrame = _frames.Find(selectedFrame) ?? moving;
            CurrentFrameIndex = 0;
            for (LinkedListNode<Frame> node = _frames.First;
                 node != null && node != _currentFrame; node = node.Next) CurrentFrameIndex++;
            InvalidateFrameCache();
            return true;
        }

        public void TogglePlaying()
        {
            _beforeSelectionChange();
            IsPlaying = !IsPlaying;
        }

        public void Start()
        {
            _beforeSelectionChange();
            IsPlaying = true;
        }

        public void Stop()
        {
            _beforeSelectionChange();
            IsPlaying = false;
        }

        public void Animate(GameTime gameTime, int fps)
        {
            if (!IsPlaying || fps <= 0 || TotalFrames == 0) return;
            double frameDuration = 1.0 / fps;
            _playbackTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_playbackTimer < frameDuration) return;

            long elapsedFrames = (long)(_playbackTimer / frameDuration);
            _playbackTimer -= elapsedFrames * frameDuration;
            int framesToAdvance = (int)(elapsedFrames % TotalFrames);
            CurrentFrameIndex = (CurrentFrameIndex + framesToAdvance) % TotalFrames;
            for (int i = 0; i < framesToAdvance; i++)
                _currentFrame = _currentFrame.Next ?? _frames.First;
        }

        private Frame CreateFrame() => new(_framePosition(), _frameSize(), _layers());

        private void EnsureFrameCache()
        {
            if (_frameCacheValid && _frameCache.Count == _frames.Count) return;
            _frameCache.Clear();
            _frameCache.AddRange(_frames);
            _frameCacheValid = true;
        }

        private void InvalidateFrameCache() => _frameCacheValid = false;

        public void Dispose()
        {
            if (_disposed) return;
            foreach (Frame frame in _frames) frame.Dispose();
            _frames.Clear();
            _currentFrame = null;
            _frameCache.Clear();
            _disposed = true;
        }
    }
}
