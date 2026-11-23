using UnityEditor;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Draws <see cref="TileEffect"/>'s projectile field normally, or not at all (zero height) when the effect that owns
    /// it has <see cref="TileEffect.UsesProjectile"/> false. The stored value is kept either way.
    /// </summary>
    [CustomPropertyDrawer(typeof(ProjectileFieldAttribute))]
    public class ProjectileFieldDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return IsHidden(property) ? 0f : EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsHidden(property)) return;
            EditorGUI.PropertyField(position, property, label, true);
        }

        private static bool IsHidden(SerializedProperty property)
        {
            // The field sits directly inside the effect's managed reference, e.g. "effects.Array.data[0].projectile".
            string path = property.propertyPath;
            int dot = path.LastIndexOf('.');
            if (dot < 0) return false;

            SerializedProperty owner = property.serializedObject.FindProperty(path.Substring(0, dot));
            if (owner == null || owner.propertyType != SerializedPropertyType.ManagedReference) return false;

            // Several selected tiles can hold different effect types; show the field unless the shown one hides it.
            return owner.managedReferenceValue is TileEffect effect && !effect.UsesProjectile;
        }
    }
}
