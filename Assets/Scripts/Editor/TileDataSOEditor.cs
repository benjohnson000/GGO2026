using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ggo2026
{
    /// <summary>Inspector for <see cref="TileDataSO"/>: live preview plus click-to-pick grids for the shape, pattern and icon sheets.</summary>
    [CustomEditor(typeof(TileDataSO))]
    public class TileDataSOEditor : Editor
    {
        // Drawn at the largest whole-pixel scale that fits; 128 shows 32px shapes at 4x on a 100% display.
        private const float PreviewSize = 128f;

        // Static so foldouts stay open when switching between tiles.
        private static bool showShapes = true;
        private static bool showPatterns = true;
        private static bool showIcons = true;

        private SerializedProperty displayName;
        private SerializedProperty colour;
        private SerializedProperty customSprite;
        private SerializedProperty shape;
        private SerializedProperty shapeColorSource;
        private SerializedProperty shapeColor;
        private SerializedProperty pattern;
        private SerializedProperty patternColorSource;
        private SerializedProperty patternColor;
        private SerializedProperty icon;
        private SerializedProperty iconColorSource;
        private SerializedProperty iconColor;

        private void OnEnable()
        {
            displayName = serializedObject.FindProperty("displayName");
            colour = serializedObject.FindProperty("colour");
            customSprite = serializedObject.FindProperty("customSprite");
            shape = serializedObject.FindProperty("shape");
            shapeColorSource = serializedObject.FindProperty("shapeColorSource");
            shapeColor = serializedObject.FindProperty("shapeColor");
            pattern = serializedObject.FindProperty("pattern");
            patternColorSource = serializedObject.FindProperty("patternColorSource");
            patternColor = serializedObject.FindProperty("patternColor");
            icon = serializedObject.FindProperty("icon");
            iconColorSource = serializedObject.FindProperty("iconColorSource");
            iconColor = serializedObject.FindProperty("iconColor");

            // Tiles made before names were synced may still be empty; TileNameSync handles new and renamed assets.
            if (EditorUtility.IsPersistent(target) && string.IsNullOrEmpty(displayName.stringValue))
            {
                displayName.stringValue = TileNameSync.StripColourPrefix(target.name);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            TileColour tileColour = (TileColour)colour.intValue;
            Sprite shapeSprite = shape.objectReferenceValue as Sprite;
            Color shapeTint = ResolveTint(shapeColorSource, shapeColor, tileColour);
            Color patternTint = ResolveTint(patternColorSource, patternColor, tileColour);
            Color iconTint = ResolveTint(iconColorSource, iconColor, tileColour);

            Sprite custom = customSprite.objectReferenceValue as Sprite;
            bool hasCustom = custom != null;

            // A custom sprite is drawn alone and untinted, as on the board.
            if (hasCustom) TileEditorGUI.Preview(custom, null, null, Color.white, Color.white, Color.white, PreviewSize);
            else TileEditorGUI.Preview(shapeSprite, pattern.objectReferenceValue as Sprite, icon.objectReferenceValue as Sprite, shapeTint, patternTint, iconTint, PreviewSize);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(hasCustom))
            {
                if (GUILayout.Button(new GUIContent("Randomize", "Pick a random colour, shape, pattern (or none) and icon."), GUILayout.Width(PreviewSize))) Randomize();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            DrawNameField();
            EditorGUILayout.PropertyField(colour);
            bool nameChanged = EditorGUI.EndChangeCheck();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Effect", "Not implemented yet");
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(customSprite);
            if (hasCustom)
            {
                EditorGUILayout.HelpBox("Custom sprite: drawn untinted with the outline. Shape, pattern and icon are ignored but kept, so clearing it restores them.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(hasCustom))
            {
                DrawLayers(shapeSprite, shapeTint, patternTint, iconTint);
            }

            EditorGUILayout.Space();
            DrawPreviewSettings();

            serializedObject.ApplyModifiedProperties();

            // Renaming mid-GUI would invalidate this inspector's layout, so do it after the frame.
            if (nameChanged) RenameLater();
        }

        private void RenameLater()
        {
            TileDataSO tile = target as TileDataSO;
            if (tile == null || !EditorUtility.IsPersistent(tile)) return;
            EditorApplication.delayCall += () => TileNameSync.RenameToConvention(tile);
        }

        /// <summary>Shape, pattern and icon sections, each with its colour source and picker grid.</summary>
        private void DrawLayers(Sprite shapeSprite, Color shapeTint, Color patternTint, Color iconTint)
        {
            EditorGUILayout.Space();
            showShapes = EditorGUILayout.Foldout(showShapes, "Shape", true);
            if (showShapes)
            {
                EditorGUILayout.PropertyField(shape, GUIContent.none);
                DrawColorSource(shapeColorSource, shapeColor);
                Pick(shape, TileEditorGUI.GetSheetSprites(TileEditorGUI.ShapeSheetPath), shapeTint, false);
                if (shape.objectReferenceValue == null) EditorGUILayout.HelpBox("No shape: the board will draw the fallback sprite.", MessageType.Info);
            }

            EditorGUILayout.Space();
            showPatterns = EditorGUILayout.Foldout(showPatterns, "Pattern", true);
            if (showPatterns)
            {
                EditorGUILayout.PropertyField(pattern, GUIContent.none);
                DrawColorSource(patternColorSource, patternColor);
                Pick(pattern, TileEditorGUI.GetPatternOptions(), patternTint, true);
                if (pattern.objectReferenceValue != null && shapeSprite == null)
                {
                    EditorGUILayout.HelpBox("Patterns are masked to the shape, so they are not drawn without one.", MessageType.Info);
                }
            }

            EditorGUILayout.Space();
            showIcons = EditorGUILayout.Foldout(showIcons, "Icon", true);
            if (showIcons)
            {
                EditorGUILayout.PropertyField(icon, GUIContent.none);
                DrawColorSource(iconColorSource, iconColor);
                Pick(icon, TileEditorGUI.GetSheetSprites(TileEditorGUI.IconSheetPath), iconTint, true);
            }
        }

        /// <summary>Colour source dropdown plus the override colour on one line; the colour is only editable for Override.</summary>
        private static void DrawColorSource(SerializedProperty source, SerializedProperty overrideColor)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(source, new GUIContent("Color", "Palette colour 1/2/3 for the tile's colour, the palette's shared Highlight colour, or Override to use the colour on the right."));
            using (new EditorGUI.DisabledScope(source.intValue != (int)TileColourSource.Override))
            {
                EditorGUILayout.PropertyField(overrideColor, GUIContent.none, GUILayout.Width(60f));
            }
            EditorGUILayout.EndHorizontal();
        }

        private static Color ResolveTint(SerializedProperty source, SerializedProperty overrideColor, TileColour tileColour)
        {
            return TileDataSO.ResolveColor((TileColourSource)source.intValue, overrideColor.colorValue, tileColour, TileEditorGUI.PreviewPalette);
        }

        /// <summary>
        /// Name field (applied on Enter or losing focus, so the asset isn't renamed per keystroke), plus a button
        /// to rename the asset when it doesn't match <c>&lt;Colour&gt;-&lt;Name&gt;</c> (saved assets only).
        /// </summary>
        private void DrawNameField()
        {
            string wanted = TileNameSync.GetAssetName((TileColour)colour.intValue, displayName.stringValue);
            bool canRename = EditorUtility.IsPersistent(target) && target.name != wanted;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.DelayedTextField(displayName, new GUIContent("Name", "Shown to the player. The asset is renamed to <Colour>-<Name> when this or the colour changes."));
            using (new EditorGUI.DisabledScope(!canRename))
            {
                if (GUILayout.Button(new GUIContent("Rename Asset", $"Rename the asset to '{wanted}'."), EditorStyles.miniButton, GUILayout.Width(100f)))
                {
                    RenameLater();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Picks a random matchable colour, shape, pattern (or none) and icon. Applied through the serialized object, so it can be undone.</summary>
        private void Randomize()
        {
            colour.intValue = (int)TileColourUtils.All[Random.Range(0, TileColourUtils.All.Count)];
            shape.objectReferenceValue = PickRandom(TileEditorGUI.GetSheetSprites(TileEditorGUI.ShapeSheetPath), shape.objectReferenceValue as Sprite, false);
            pattern.objectReferenceValue = PickRandom(TileEditorGUI.GetPatternOptions(), pattern.objectReferenceValue as Sprite, true);
            icon.objectReferenceValue = PickRandom(TileEditorGUI.GetSheetSprites(TileEditorGUI.IconSheetPath), icon.objectReferenceValue as Sprite, false);
            RenameLater(); // The colour may have changed.
        }

        /// <summary>Random sprite from the list. With <paramref name="allowNone"/>, "none" is one more equally likely option.</summary>
        private static Sprite PickRandom(IReadOnlyList<Sprite> sprites, Sprite fallback, bool allowNone)
        {
            if (sprites.Count == 0) return fallback;
            int index = Random.Range(0, sprites.Count + (allowNone ? 1 : 0));
            return index < sprites.Count ? sprites[index] : null;
        }

        private static void Pick(SerializedProperty property, IReadOnlyList<Sprite> sprites, Color tint, bool allowNone)
        {
            Sprite current = property.objectReferenceValue as Sprite;
            Sprite picked = TileEditorGUI.SpriteGrid(current, sprites, tint, allowNone);
            if (picked != current) property.objectReferenceValue = picked;
        }

        private static void DrawPreviewSettings()
        {
            EditorGUILayout.BeginHorizontal();
            TileEditorGUI.PreviewPalette = (PaletteSO)EditorGUILayout.ObjectField(
                new GUIContent("Preview Palette", "Palette that colours this preview. Editor only; not saved on the tile."),
                TileEditorGUI.PreviewPalette, typeof(PaletteSO), false);
            if (GUILayout.Button(new GUIContent("Refresh", "Reload the sprite sheets and palette."), GUILayout.Width(60f))) TileEditorGUI.ClearCache();
            EditorGUILayout.EndHorizontal();
        }
    }
}
