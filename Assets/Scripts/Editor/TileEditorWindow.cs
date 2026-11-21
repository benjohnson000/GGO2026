using System.IO;
using UnityEditor;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Window for authoring new tiles. Edits an in-memory draft with the same inspector as <see cref="TileDataSO"/>,
    /// then saves a copy as an asset, so several similar tiles can be made in a row.
    /// </summary>
    public class TileEditorWindow : EditorWindow
    {
        private const string DefaultFolder = "Assets/Tiles/Data";

        [SerializeField] private string folder = DefaultFolder;

        private TileDataSO draft;
        private Editor draftEditor;
        private Vector2 scroll;

        [MenuItem("GGO2026/Tile Editor")]
        private static void Open()
        {
            GetWindow<TileEditorWindow>("Tile Editor");
        }

        private void OnEnable()
        {
            NewDraft();
        }

        private void OnDisable()
        {
            DestroyDraft();
        }

        private void OnGUI()
        {
            if (draftEditor == null) NewDraft();

            EditorGUILayout.BeginHorizontal();
            folder = EditorGUILayout.TextField("Save Folder", folder);
            if (GUILayout.Button("...", GUILayout.Width(28f))) PickFolder();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            draftEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUILayout.Height(28f), GUILayout.Width(80f))) NewDraft();
            if (GUILayout.Button("Create Tile Asset", GUILayout.Height(28f))) CreateAsset();
            EditorGUILayout.EndHorizontal();
        }

        private void NewDraft()
        {
            DestroyDraft();
            draft = CreateInstance<TileDataSO>();
            draft.name = "New Tile";
            draft.hideFlags = HideFlags.DontSave;
            draftEditor = Editor.CreateEditor(draft);
        }

        private void DestroyDraft()
        {
            if (draftEditor != null) DestroyImmediate(draftEditor);
            if (draft != null) DestroyImmediate(draft);
            draftEditor = null;
            draft = null;
        }

        private void CreateAsset()
        {
            if (draft.Shape == null && !draft.HasCustomSprite)
            {
                ShowNotification(new GUIContent("Pick a shape or a custom sprite first."));
                return;
            }
            if (!folder.StartsWith("Assets"))
            {
                ShowNotification(new GUIContent("Save folder must be inside Assets."));
                return;
            }

            EnsureFolder(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{TileNameSync.GetAssetName(draft.Colour, draft.DisplayName)}.asset");

            TileDataSO asset = Instantiate(draft);
            asset.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            EditorGUIUtility.PingObject(asset);
            ShowNotification(new GUIContent($"Created {Path.GetFileName(path)}"));
        }

        private void PickFolder()
        {
            string picked = EditorUtility.OpenFolderPanel("Tile Save Folder", folder, "");
            if (string.IsNullOrEmpty(picked)) return;

            string projectRoot = Path.GetFullPath(Application.dataPath + "/..").Replace('\\', '/') + "/";
            picked = picked.Replace('\\', '/');
            if (picked.StartsWith(projectRoot)) folder = picked.Substring(projectRoot.Length);
            else ShowNotification(new GUIContent("Pick a folder inside this project's Assets."));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = "Assets";
            foreach (string part in path.Split('/'))
            {
                if (part == "Assets" || string.IsNullOrEmpty(part)) continue;
                string next = $"{parent}/{part}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(parent, part);
                parent = next;
            }
        }
    }
}
