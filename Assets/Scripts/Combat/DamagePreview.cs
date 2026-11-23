using System;
using System.Collections.Generic;

namespace ggo2026
{
    /// <summary>
    /// The health a combatant is set to lose to its statuses when its next turn starts, after modifiers and any block
    /// it keeps. A snapshot: see <see cref="Combat.PreviewDamage"/>.
    /// </summary>
    public class DamagePreview
    {
        /// <summary>One entry per status that will cost health, in the order they will hit.</summary>
        public IReadOnlyList<PreviewedDamage> Hits { get; }

        internal static readonly DamagePreview None = new DamagePreview(Array.Empty<PreviewedDamage>());

        internal DamagePreview(IReadOnlyList<PreviewedDamage> hits)
        {
            Hits = hits;
        }
    }
}
