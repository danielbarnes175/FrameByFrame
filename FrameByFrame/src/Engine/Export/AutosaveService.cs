using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace FrameByFrame.src.Engine.Export
{
    public sealed class AutosaveService
    {
        private sealed class Preferences
        {
            public bool AutosaveEnabled { get; set; } = true;
        }

        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

        private readonly string _preferencesPath;
        private readonly TimeSpan _interval;
        private readonly Action<Animation.Animation> _save;
        private TimeSpan _elapsed;

        public bool IsEnabled { get; private set; }
        public string LastError { get; private set; } = string.Empty;

        public AutosaveService(string preferencesPath = "Settings.json", TimeSpan? interval = null,
            Action<Animation.Animation> save = null)
        {
            _preferencesPath = Path.GetFullPath(preferencesPath);
            _interval = interval ?? DefaultInterval;
            _save = save ?? SaveService.SaveAnimation;
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
            if (!IsEnabled || animation == null || animation.TotalFrames == 0) return;

            _elapsed += gameTime.ElapsedGameTime;
            if (_elapsed < _interval) return;

            _elapsed = TimeSpan.Zero;
            SaveNow(animation);
        }

        public bool SaveNow(Animation.Animation animation)
        {
            if (!IsEnabled || animation == null || animation.TotalFrames == 0) return false;
            try
            {
                _save(animation);
                LastError = string.Empty;
                _elapsed = TimeSpan.Zero;
                return true;
            }
            catch (Exception ex)
            {
                LastError = $"Autosave failed: {ex.Message}";
                return false;
            }
        }

        private bool LoadPreference()
        {
            try
            {
                if (!File.Exists(_preferencesPath)) return true;
                Preferences preferences = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(_preferencesPath));
                return preferences?.AutosaveEnabled ?? true;
            }
            catch
            {
                return true;
            }
        }

        private void SavePreference()
        {
            string directory = Path.GetDirectoryName(_preferencesPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string temporaryPath = _preferencesPath + ".tmp";
            File.WriteAllText(temporaryPath,
                JsonSerializer.Serialize(new Preferences { AutosaveEnabled = IsEnabled }, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, _preferencesPath, overwrite: true);
        }
    }
}
