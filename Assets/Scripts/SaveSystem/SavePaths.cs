using System.IO;
using System.Linq;
using UnityEngine;

namespace Rival.SaveSystem
{
    /// <summary>
    /// All on-disk paths in one place so formats stay consistent and the root
    /// can move (tests, custom installs) without touching callers.
    /// </summary>
    public static class SavePaths
    {
        public static string RootDirectory => Path.Combine(Application.persistentDataPath, "RivalSaves");

        public static string ProfileFile(string actor) =>
            Path.Combine(RootDirectory, "profiles", Sanitize(actor) + ".json");

        public static string TelemetryLogFile(string sessionId) =>
            Path.Combine(RootDirectory, "telemetry", "session-" + Sanitize(sessionId) + ".json");

        public static string LoadoutFile() =>
            Path.Combine(RootDirectory, "loadout.json");

        /// <summary>Creates the directory containing a target file path.</summary>
        public static void EnsureDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        }

        private static string Sanitize(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = new System.Text.StringBuilder(name.Length);
            foreach (var c in name) sanitized.Append(invalid.Contains(c) ? '_' : c);
            return sanitized.ToString();
        }
    }
}
