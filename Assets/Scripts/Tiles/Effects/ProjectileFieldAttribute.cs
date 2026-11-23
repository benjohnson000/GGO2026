using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Marks <see cref="TileEffect"/>'s projectile field so the inspector hides it on effects whose
    /// <see cref="TileEffect.UsesProjectile"/> is false. Drawn by <c>ProjectileFieldDrawer</c> in the editor assembly.
    /// </summary>
    public class ProjectileFieldAttribute : PropertyAttribute
    {
    }
}
