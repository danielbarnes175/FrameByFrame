using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrameByFrame.src.Engine.Animation;
using Microsoft.Xna.Framework.Audio;

namespace FrameByFrame.src.Engine.Audio
{
    public sealed class AudioPlaybackController : IDisposable
    {
        private const int BufferSize = 16_384;
        private const int TargetPendingBuffers = 3;

        private sealed class TrackPlayback : IDisposable
        {
            public AudioTrack Track { get; }
            public DynamicSoundEffectInstance Instance { get; }
            public int Cursor { get; set; }

            public TrackPlayback(AudioTrack track, DynamicSoundEffectInstance instance, int cursor)
            {
                Track = track;
                Instance = instance;
                Cursor = cursor;
            }

            public void Dispose()
            {
                Instance.Stop(immediate: true);
                Instance.Dispose();
            }
        }

        private readonly Animation.Animation _animation;
        private readonly Dictionary<Guid, TrackPlayback> _playing = new();
        private readonly Dictionary<Guid, Task> _decoding = new();
        private readonly HashSet<Guid> _failedDecodes = new();
        private long _playbackRevision = -1;
        private long _audioRevision = -1;
        private int _fps = -1;
        private int _decodedTrackCount;
        private bool _disposed;

        public string LastError { get; private set; } = string.Empty;
        public bool IsPreparing => _decoding.Values.Any(task => !task.IsCompleted);

        public AudioPlaybackController(Animation.Animation animation)
        {
            _animation = animation ?? throw new ArgumentNullException(nameof(animation));
            PrepareMissingTracks();
        }

        public void Update()
        {
            if (_disposed) return;
            PrepareMissingTracks();
            ObserveDecodeResults();

            if (!_animation.IsPlaying)
            {
                StopAll();
                RememberState();
                return;
            }

            bool needsSync = _playbackRevision != _animation.PlaybackRevision ||
                _audioRevision != _animation.AudioRevision || _fps != _animation.fps ||
                _decodedTrackCount != _animation.AudioTracks.Count(track => track.PcmData != null);
            try
            {
                if (needsSync) Synchronize();
                else
                {
                    StartTracksReachedByPlayhead();
                    foreach (TrackPlayback playback in _playing.Values.ToArray()) FillBuffers(playback);
                }
                RememberState();
            }
            catch (Exception ex)
            {
                LastError = $"Audio preview failed: {ex.Message}";
                StopAll();
                RememberState();
            }
        }

        public void Stop()
        {
            StopAll();
            RememberState();
        }

        private void PrepareMissingTracks()
        {
            if (_decoding.Count > 0) return;
            foreach (AudioTrack track in _animation.AudioTracks)
            {
                if (track.PcmData == null && !_decoding.ContainsKey(track.Id) && !_failedDecodes.Contains(track.Id))
                {
                    _decoding[track.Id] = AudioService.DecodeAsync(track);
                    break;
                }
            }
        }

        private void ObserveDecodeResults()
        {
            foreach (var item in _decoding.ToArray())
            {
                if (!item.Value.IsCompleted) continue;
                if (item.Value.IsFaulted)
                {
                    LastError = $"Audio preparation failed: {item.Value.Exception?.GetBaseException().Message}";
                    _failedDecodes.Add(item.Key);
                }
                else
                {
                    AudioTrack decoded = _animation.AudioTracks.FirstOrDefault(track => track.Id == item.Key);
                    long decodedBytes = _animation.AudioTracks.Sum(track => (long)(track.PcmData?.Length ?? 0));
                    if (decoded != null && decodedBytes > AudioService.MaxDecodedProjectBytes)
                    {
                        decoded.ReleaseDecodedPcm();
                        _failedDecodes.Add(item.Key);
                        LastError = "Audio preparation failed: the project exceeds the decoded audio limit.";
                    }
                }
                _decoding.Remove(item.Key);
            }
        }

        private void Synchronize()
        {
            StopAll();
            StartTracksReachedByPlayhead();
        }

        private void StartTracksReachedByPlayhead()
        {
            double position = _animation.PlaybackPositionSeconds;
            foreach (AudioTrack track in _animation.AudioTracks)
            {
                if (_playing.ContainsKey(track.Id) || track.IsMuted || track.Volume <= 0 || track.PcmData == null)
                    continue;
                int cursor = CalculatePcmOffset(track, position, _animation.fps);
                if (cursor < 0) continue;
                var instance = new DynamicSoundEffectInstance(AudioTrack.SampleRate, AudioChannels.Stereo)
                {
                    Volume = track.Volume
                };
                var playback = new TrackPlayback(track, instance, cursor);
                _playing.Add(track.Id, playback);
                FillBuffers(playback);
                if (instance.PendingBufferCount > 0) instance.Play();
            }
        }

        private void FillBuffers(TrackPlayback playback)
        {
            byte[] pcm = playback.Track.PcmData;
            while (playback.Instance.PendingBufferCount < TargetPendingBuffers && playback.Cursor < pcm.Length)
            {
                int count = Math.Min(BufferSize, pcm.Length - playback.Cursor);
                count -= count % (AudioTrack.ChannelCount * AudioTrack.BytesPerSample);
                if (count <= 0) break;
                playback.Instance.SubmitBuffer(pcm, playback.Cursor, count);
                playback.Cursor += count;
            }
        }

        internal static int CalculatePcmOffset(AudioTrack track, double timelineSeconds, int fps)
        {
            if (track?.PcmData == null || fps <= 0 || !double.IsFinite(timelineSeconds)) return -1;
            double trackPosition = timelineSeconds - track.StartFrame / (double)fps;
            if (trackPosition < 0 || trackPosition >= track.DurationSeconds) return -1;
            int cursor = (int)Math.Min(track.PcmData.Length,
                Math.Floor(trackPosition * AudioTrack.BytesPerSecond));
            return cursor - cursor % (AudioTrack.ChannelCount * AudioTrack.BytesPerSample);
        }

        private void RememberState()
        {
            _playbackRevision = _animation.PlaybackRevision;
            _audioRevision = _animation.AudioRevision;
            _fps = _animation.fps;
            _decodedTrackCount = _animation.AudioTracks.Count(track => track.PcmData != null);
        }

        private void StopAll()
        {
            foreach (TrackPlayback playback in _playing.Values) playback.Dispose();
            _playing.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            StopAll();
            _disposed = true;
        }
    }
}
