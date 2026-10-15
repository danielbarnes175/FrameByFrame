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
            if (index < 0 || _presets[index].IsBuiltIn) return false;
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
                    string json = File.ReadAllText(_path);
                    List<BrushPreset> loaded = JsonSerializer.Deserialize<List<BrushPreset>>(
                        json);
                    if (loaded != null) _presets.AddRange(loaded.Where(candidate => candidate != null));

                    // The original editor stored shape/size controls rather than a tip mask.
                    // Preserve those user entries as editable masks and add the new visual library once.
                    if (json.Contains("\"Shape\"", StringComparison.Ordinal))
                    {
                        foreach (BrushPreset builtIn in BrushPreset.CreateBuiltIns())
                            if (!_presets.Any(preset => string.Equals(preset.Name, builtIn.Name,
                                StringComparison.OrdinalIgnoreCase)))
                                _presets.Add(builtIn);
                    }
                }
                foreach (BrushPreset preset in _presets) preset.Normalize();
                MarkRecognizedBuiltIns();
                if (_presets.Count == 0)
                {
                    _presets.AddRange(BrushPreset.CreateBuiltIns());
                }
                if (_presets.Select(preset => preset.Id).Distinct().Count() != _presets.Count)
                    foreach (BrushPreset preset in _presets) preset.Id = Guid.NewGuid();
                LastError = string.Empty;
            }
            catch (Exception ex)
            {
                _presets.Clear();
                _presets.AddRange(BrushPreset.CreateBuiltIns());
                LastError = $"Brush presets could not be loaded: {ex.Message}";
            }
        }

        private void MarkRecognizedBuiltIns()
        {
            foreach (BrushPreset preset in _presets)
            foreach (BrushPreset builtIn in BrushPreset.CreateBuiltIns())
                if (string.Equals(preset.Name, builtIn.Name, StringComparison.OrdinalIgnoreCase) &&
                    preset.TipWidth == builtIn.TipWidth && preset.TipHeight == builtIn.TipHeight &&
                    preset.TipAlpha.SequenceEqual(builtIn.TipAlpha))
                {
                    preset.IsBuiltIn = true;
                    preset.TileTip = builtIn.TileTip;
                    break;
                }
        }
    }
}
