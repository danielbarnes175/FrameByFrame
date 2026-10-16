using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Export
{
    public sealed class AutosaveService : IDisposable
    {
        private sealed class Preferences { public bool AutosaveEnabled { get; set; } = true; }
        private sealed record PendingSave(FbfProjectFile.ProjectSnapshot Snapshot, Animation.Animation Animation);

        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);
        private readonly string _preferencesPath;
        private readonly TimeSpan _interval;
        private readonly Action<FbfProjectFile.ProjectSnapshot> _save;
        private readonly object _sync = new();
        private TimeSpan _elapsed;
        private PendingSave? _pending;
        private Task? _worker;
        private string _lastError = string.Empty;
        private bool _disposed;

        public bool IsEnabled { get; private set; }
        public bool IsSaving { get { lock (_sync) return _worker is { IsCompleted: false }; } }
        public string LastError { get { lock (_sync) return _lastError; } }

        public AutosaveService(string preferencesPath = "Settings.json", TimeSpan? interval = null)
            : this(preferencesPath, interval, SaveService.SaveSnapshot) { }

        internal AutosaveService(string preferencesPath, TimeSpan? interval,
            Action<FbfProjectFile.ProjectSnapshot> save)
        {
            _preferencesPath = Path.GetFullPath(preferencesPath);
            _interval = interval ?? DefaultInterval;
            _save = save ?? SaveService.SaveSnapshot;
            IsEnabled = LoadPreference();
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            _elapsed = TimeSpan.Zero;
            SavePreference();
        }

        public void Update(GameTime gameTime, Animation.Animation animation)
        {
            ArgumentNullException.ThrowIfNull(gameTime);
            if (!IsEnabled || animation == null || animation.TotalFrames == 0 || !animation.IsDirty) return;
            _elapsed += gameTime.ElapsedGameTime;
            if (_elapsed < _interval) return;
            _elapsed = TimeSpan.Zero;
            SaveNow(animation);
        }

        public bool SaveNow(Animation.Animation animation)
        {
            if (!IsEnabled || animation == null || animation.TotalFrames == 0 || _disposed) return false;
            try
            {
                var request = new PendingSave(SaveService.CaptureProjectSnapshot(animation), animation);
                lock (_sync)
                {
                    _pending = request;
                    if (_worker == null) _worker = Task.Run(ProcessQueue);
                }
                _elapsed = TimeSpan.Zero;
                return true;
            }
            catch (Exception ex)
            {
                lock (_sync) _lastError = $"Autosave failed: {ex.Message}";
                return false;
            }
        }

        public void Flush(Animation.Animation? animation = null)
        {
            if (animation is { IsDirty: true } && IsEnabled && !_disposed) SaveNow(animation);
            Task? worker;
            lock (_sync) worker = _worker;
            worker?.GetAwaiter().GetResult();
        }

        private void ProcessQueue()
        {
            while (true)
            {
                PendingSave? request;
                lock (_sync)
                {
                    request = _pending;
                    _pending = null;
                    if (request == null)
                    {
                        _worker = null;
                        return;
                    }
                }
                try
                {
                    _save(request.Snapshot);
                    request.Animation.MarkSaved(request.Snapshot.Revision);
                    lock (_sync) _lastError = string.Empty;
                }
                catch (Exception ex)
                {
                    lock (_sync) _lastError = $"Autosave failed: {ex.Message}";
                }
            }
        }

        private bool LoadPreference()
        {
            try
            {
                if (!File.Exists(_preferencesPath)) return true;
                Preferences? preferences = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(_preferencesPath));
                return preferences?.AutosaveEnabled ?? true;
            }
            catch { return true; }
        }

        private void SavePreference()
        {
            string? directory = Path.GetDirectoryName(_preferencesPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporaryPath = _preferencesPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(
                new Preferences { AutosaveEnabled = IsEnabled }, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, _preferencesPath, overwrite: true);
        }

        public void Dispose()
        {
            if (_disposed) return;
            Flush();
            _disposed = true;
        }
    }
}
