using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FrameByFrame.src.Engine
{
    public sealed class AppDataPaths
    {
        private const string ApplicationName = "FrameByFrame";
        private const string LegacyMigrationMarkerName = ".legacy-migration-complete";
        private static AppDataPaths _current = CreateDefault();

        public static AppDataPaths Current => _current;
        public string Root { get; }
        public string PreferencesDirectory { get; }
        public string PreferencesFile { get; }
        public string BrushesDirectory { get; }
        public string CustomBrushesFile { get; }
        public string ProjectsDirectory { get; }
        public string CacheDirectory { get; }
        public string TemporaryDirectory { get; }
        public IReadOnlyList<string> MigrationErrors => _migrationErrors;
        private readonly List<string> _migrationErrors = new();

        public AppDataPaths(string root, IEnumerable<string>? legacyRoots = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(root);
            Root = Path.GetFullPath(root);
            PreferencesDirectory = Path.Combine(Root, "Preferences");
            PreferencesFile = Path.Combine(PreferencesDirectory, "Settings.json");
            BrushesDirectory = Path.Combine(Root, "Brushes");
            CustomBrushesFile = Path.Combine(BrushesDirectory, "CustomBrushes.json");
            ProjectsDirectory = Path.Combine(Root, "Projects");
            CacheDirectory = Path.Combine(Root, "Cache");
            TemporaryDirectory = Path.Combine(Root, "Temp");
            EnsureDirectories();
            MigrateLegacyDataOnce(legacyRoots ?? Array.Empty<string>());
        }

        internal static IDisposable UseForTests(string root, IEnumerable<string>? legacyRoots = null)
        {
            AppDataPaths previous = _current;
            _current = new AppDataPaths(root, legacyRoots);
            return new RestoreCurrent(previous);
        }

        private static AppDataPaths CreateDefault()
        {
            string? platformRoot = OperatingSystem.IsLinux()
                ? Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                : null;
            if (string.IsNullOrWhiteSpace(platformRoot))
                platformRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(platformRoot))
                platformRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(platformRoot))
                throw new InvalidOperationException("The operating system did not provide a writable user-data directory.");

            string root = Path.Combine(platformRoot, ApplicationName);
            string[] legacyRoots =
            {
                Directory.GetCurrentDirectory(),
                Path.Combine(Directory.GetCurrentDirectory(), ApplicationName),
                AppContext.BaseDirectory
            };
            return new AppDataPaths(root, legacyRoots);
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(PreferencesDirectory);
            Directory.CreateDirectory(BrushesDirectory);
            Directory.CreateDirectory(ProjectsDirectory);
            Directory.CreateDirectory(CacheDirectory);
            Directory.CreateDirectory(TemporaryDirectory);
        }

        private void MigrateLegacyDataOnce(IEnumerable<string> legacyRoots)
        {
            string marker = Path.Combine(Root, LegacyMigrationMarkerName);
            if (File.Exists(marker)) return;

            // Versions before the marker existed may already have migrated data. Treat any
            // managed file as proof so renamed or removed legacy projects are not restored.
            if (!ContainsManagedData()) MigrateLegacyData(legacyRoots);
            try { File.WriteAllText(marker, string.Empty); }
            catch (Exception ex)
            {
                _migrationErrors.Add($"Legacy migration could not be marked complete: {ex.Message}");
            }
        }

        private bool ContainsManagedData() =>
            File.Exists(PreferencesFile) || File.Exists(CustomBrushesFile) ||
            Directory.EnumerateFiles(ProjectsDirectory, "*.fbf", SearchOption.TopDirectoryOnly).Any();

        private void MigrateLegacyData(IEnumerable<string> legacyRoots)
        {
            foreach (string legacyRoot in legacyRoots.Where(path => !string.IsNullOrWhiteSpace(path))
                         .Select(Path.GetFullPath).Distinct(StringComparer.Ordinal))
            {
                try
                {
                    if (IsSameOrNestedPath(legacyRoot, Root)) continue;
                    CopyIfMissing(Path.Combine(legacyRoot, "Settings.json"), PreferencesFile);
                    CopyIfMissing(Path.Combine(legacyRoot, "CustomBrushes.json"), CustomBrushesFile);
                    CopyIfMissing(Path.Combine(legacyRoot, "Brushes.json"), CustomBrushesFile);
                    CopyProjectsIfMissing(Path.Combine(legacyRoot, "Projects"));
                }
                catch (Exception ex)
                {
                    _migrationErrors.Add($"Legacy data at '{legacyRoot}' could not be copied: {ex.Message}");
                }
            }
        }

        private void CopyProjectsIfMissing(string legacyProjects)
        {
            if (!Directory.Exists(legacyProjects)) return;
            foreach (string source in Directory.EnumerateFiles(legacyProjects, "*.fbf", SearchOption.TopDirectoryOnly))
                CopyIfMissing(source, Path.Combine(ProjectsDirectory, Path.GetFileName(source)));
        }

        private static void CopyIfMissing(string source, string destination)
        {
            if (!File.Exists(source) || File.Exists(destination)) return;
            string? directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            try { File.Copy(source, destination, overwrite: false); }
            catch (IOException) when (File.Exists(destination)) { }
        }

        private static bool IsSameOrNestedPath(string candidate, string parent)
        {
            string relative = Path.GetRelativePath(parent, candidate);
            return relative == "." || (!Path.IsPathRooted(relative) && relative != ".." &&
                !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        }

        private sealed class RestoreCurrent : IDisposable
        {
            private AppDataPaths? _previous;
            public RestoreCurrent(AppDataPaths previous) => _previous = previous;
            public void Dispose()
            {
                if (_previous == null) return;
                _current = _previous;
                _previous = null;
            }
        }
    }
}
