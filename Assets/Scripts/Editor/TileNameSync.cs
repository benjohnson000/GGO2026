using System.IO;
using UnityEditor;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Keeps tile asset names in the form <c>&lt;Colour&gt;-&lt;Name&gt;</c>. The inspector renames the asset when
    /// the name or colour changes; this postprocessor fills an empty display name from the asset name on import.
    /// </summary>
    public class TileNameSync : AssetPostprocessor
    {
        private const string DisplayNameProperty = "displayName";
        private const string DefaultName = "New Tile";

        /// <summary>The asset file name (without extension) a tile with this colour and display name should have.</summary>
        public static string GetAssetName(TileColour colour, string displayName)
        {
            string name = string.IsNullOrWhiteSpace(displayName) ? DefaultName : displayName.Trim();
            return SanitiseFileName($"{colour}-{name}");
        }

        /// <summary>The asset name without a leading <c>&lt;Colour&gt;-</c>, if it has one.</summary>
        public static string StripColourPrefix(string assetName)
        {
            int dash = assetName.IndexOf('-');
            if (dash <= 0) return assetName;
            if (!System.Enum.TryParse(assetName.Substring(0, dash), out TileColour _)) return assetName;
            string rest = assetName.Substring(dash + 1).Trim();
            return string.IsNullOrEmpty(rest) ? assetName : rest;
        }

        /// <summary>Renames a saved tile asset to <see cref="GetAssetName"/>. Warns if the name is already taken.</summary>
        public static void RenameToConvention(TileDataSO tile)
        {
            if (tile == null || !EditorUtility.IsPersistent(tile)) return;

            string wanted = GetAssetName(tile.Colour, tile.DisplayName);
            if (tile.name == wanted) return;

            string path = AssetDatabase.GetAssetPath(tile);
            string error = AssetDatabase.RenameAsset(path, wanted);
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning($"Could not rename tile '{tile.name}' to '{wanted}': {error}", tile);
        }

        /// <summary>Replaces characters that are not allowed in file names with <c>_</c>.</summary>
        public static string SanitiseFileName(string name)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars()) name = name.Replace(invalid, '_');
            name = name.Trim();
            return string.IsNullOrEmpty(name) ? DefaultName : name;
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                FillEmptyName(path);
            }
        }

        /// <summary>Sets an empty display name to the asset name, minus any colour prefix.</summary>
        private static void FillEmptyName(string path)
        {
            if (!path.EndsWith(".asset")) return;
            TileDataSO tile = AssetDatabase.LoadAssetAtPath<TileDataSO>(path);
            if (tile == null) return;

            SerializedObject serializedTile = new SerializedObject(tile);
            SerializedProperty displayName = serializedTile.FindProperty(DisplayNameProperty);
            if (!string.IsNullOrEmpty(displayName.stringValue)) return;

            displayName.stringValue = StripColourPrefix(tile.name);
            serializedTile.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(tile);
        }
    }
}
