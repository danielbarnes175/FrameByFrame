using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace FrameByFrame.src.Engine
{
    public sealed class BrushPresetStore
    {
        private const string DefaultLibraryFileName = "DefaultBrushes.json";
        private const string CustomLibraryFileName = "CustomBrushes.json";

        private readonly string _defaultPath;
        private readonly string _customPath;
        private readonly List<BrushPreset> _presets = new();
        private readonly List<BrushPreset> _customPresets = new();

        public IReadOnlyList<BrushPreset> Presets => _presets;
        public string LastError { get; private set; } = string.Empty;

        public BrushPresetStore(string customPath = CustomLibraryFileName, string? defaultPath = null)
        {
            _customPath = Path.GetFullPath(customPath);
            _defaultPath = Path.GetFullPath(defaultPath ??
                Path.Combine(AppContext.BaseDirectory, DefaultLibraryFileName));
            Load();
        }

        public BrushPreset Add(BrushPreset? source = null)
        {
            BrushPreset preset = source?.Clone($"Custom {_customPresets.Count + 1}") ??
                new BrushPreset { Name = $"Custom {_customPresets.Count + 1}" };
            preset.IsBuiltIn = false;
            preset.Normalize();
            _customPresets.Add(preset);
            _presets.Add(preset);
            Save();
            return preset;
        }

        public bool Remove(Guid id)
        {
            int customIndex = _customPresets.FindIndex(candidate => candidate.Id == id);
            if (customIndex < 0) return false;
            _customPresets.RemoveAt(customIndex);
            _presets.RemoveAll(candidate => candidate.Id == id);
            Save();
            return true;
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(_customPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                string temporary = _customPath + ".tmp";
                File.WriteAllText(temporary, JsonSerializer.Serialize(_customPresets,
                    new JsonSerializerOptions { WriteIndented = true }));
                File.Move(temporary, _customPath, overwrite: true);
                LastError = string.Empty;
            }
            catch (Exception ex)
            {
                LastError = $"Custom brushes could not be saved: {ex.Message}";
            }
        }

        private void Load()
        {
            List<string> errors = new();
            LoadDefaults(errors);
            LoadCustoms(errors);
            EnsureUniqueIds();
            LastError = string.Join(" ", errors);
        }

        private void LoadDefaults(List<string> errors)
        {
            try
            {
                List<BrushPreset> defaults = ReadPresets(_defaultPath);
                if (defaults.Count == 0) throw new InvalidDataException("The default brush library is empty.");
                foreach (BrushPreset preset in defaults)
                {
                    preset.Normalize();
                    preset.IsBuiltIn = true;
                    _presets.Add(preset);
                }
            }
            catch (Exception ex)
            {
                _presets.AddRange(BrushPreset.CreateBuiltIns());
                errors.Add($"Default brushes could not be loaded: {ex.Message}");
            }
        }

        private void LoadCustoms(List<string> errors)
        {
            if (!File.Exists(_customPath)) return;

            try
            {
                HashSet<string> reservedNames = _presets.Select(preset => preset.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (BrushPreset preset in ReadPresets(_customPath))
                {
                    preset.Normalize();
                    if (reservedNames.Contains(preset.Name)) continue;
                    preset.IsBuiltIn = false;
                    _customPresets.Add(preset);
                    _presets.Add(preset);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Custom brushes could not be loaded: {ex.Message}");
            }
        }

        private static List<BrushPreset> ReadPresets(string path)
        {
            List<BrushPreset>? loaded = JsonSerializer.Deserialize<List<BrushPreset>>(
                File.ReadAllText(path));
            return loaded?.Where(candidate => candidate != null).ToList() ?? new List<BrushPreset>();
        }

        private void EnsureUniqueIds()
        {
            HashSet<Guid> ids = new();
            foreach (BrushPreset preset in _presets)
                if (preset.Id == Guid.Empty || !ids.Add(preset.Id))
                {
                    preset.Id = Guid.NewGuid();
                    ids.Add(preset.Id);
                }
        }
    }
}
