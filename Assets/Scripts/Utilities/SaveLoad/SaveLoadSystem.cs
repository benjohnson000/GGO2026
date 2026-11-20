using System;
using System.IO;
using UnityEngine;

namespace benjohnson
{
    /// <summary>
    /// JSON save files in Application.persistentDataPath.
    /// Uses JsonUtility, so data must be a [Serializable] class/struct wrapping any lists or arrays
    /// (no dictionaries, no top-level collections).
    /// </summary>
    public static class SaveLoadSystem
    {
        private static string saveFolder => Application.persistentDataPath;

        private static string GetPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException($"Invalid save file name: '{fileName}'", nameof(fileName));

            return Path.Combine(saveFolder, $"{fileName}.json");
        }

        /// <summary>
        /// Writes the data to a temp file then swaps it in, so a crash mid-write can't corrupt an existing save.
        /// Returns false (and logs) on failure.
        /// </summary>
        public static bool Save<T>(T data, string fileName)
        {
            string path = GetPath(fileName);
            string tempPath = path + ".tmp";
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(tempPath, json);

                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save '{path}': {e.Message}");
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* best effort */ }
                return false;
            }
        }

        /// <summary>
        /// Loads the data if the file exists and parses. Prefer this over Load, which returns null for classes.
        /// </summary>
        public static bool TryLoad<T>(string fileName, out T data)
        {
            data = default;
            string path = GetPath(fileName);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"Save file not found: {path}");
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<T>(File.ReadAllText(path));
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load '{path}': {e.Message}");
                data = default;
                return false;
            }
        }

        /// <summary>
        /// Returns default(T) (null for classes) if the file is missing or unreadable. Check Exists or use TryLoad.
        /// </summary>
        public static T Load<T>(string fileName)
        {
            TryLoad(fileName, out T data);
            return data;
        }

        public static void Delete(string fileName)
        {
            string path = GetPath(fileName);
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete '{path}': {e.Message}");
            }
        }

        public static bool Exists(string fileName)
        {
            return File.Exists(GetPath(fileName));
        }
    }
}
