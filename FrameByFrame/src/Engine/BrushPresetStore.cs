using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FrameByFrame.src.Engine
{
    public sealed class BrushPresetStore
    {
        private readonly string _path;
        private readonly List<BrushPreset> _presets = new();

        public IReadOnlyList<BrushPreset> Presets => _presets;
        public string LastError { get; private set; } = string.Empty;

        public BrushPresetStore(string path = "Brushes.json")
        {
            _path = Path.GetFullPath(path);
            Load();
        }

        public BrushPreset Add(BrushPreset source = null)
        {
            BrushPreset preset = source?.Clone($"Custom {_presets.Count + 1}") ??
                new BrushPreset { Name = $"Custom {_presets.Count + 1}" };
            preset.Normalize();
            _presets.Add(preset);
            Save();
            return preset;
        }

        public bool Remove(Guid id)
        {
            if (_presets.Count <= 1) return false;
            int index = _presets.FindIndex(candidate => candidate.Id == id);
            if (index < 0) return false;
            _presets.RemoveAt(index);
            Save();
            return true;
        }

        public void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                string temporary = _path + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(_presets,
                    new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temporary, _path, overwrite: true);
                LastError = string.Empty;
            }
            catch (Exception ex)
            {
                LastError = $"Brush presets could not be saved: {ex.Message}";
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    List<BrushPreset> loaded = JsonSerializer.Deserialize<List<BrushPreset>>(
                        File.ReadAllText(_path));
                    if (loaded != null) _presets.AddRange(loaded.Where(candidate => candidate != null));
                }
                foreach (BrushPreset preset in _presets) preset.Normalize();
                if (_presets.Count == 0) _presets.Add(new BrushPreset());
                if (_presets.Select(preset => preset.Id).Distinct().Count() != _presets.Count)
                    foreach (BrushPreset preset in _presets) preset.Id = Guid.NewGuid();
                LastError = string.Empty;
            }
            catch (Exception ex)
            {
                _presets.Clear();
                _presets.Add(new BrushPreset());
                LastError = $"Brush presets could not be loaded: {ex.Message}";
            }
        }
    }
}
