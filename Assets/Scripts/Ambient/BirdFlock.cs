using System;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Scene-independent hooks into the ambient birds, so gameplay code such as charms can react to them or tune them
    /// without a reference to a <see cref="BirdSpawner"/>. Bonuses are additive: a charm adds its amount when gained
    /// and subtracts it when lost. They persist across scenes until changed or <see cref="ResetBonuses"/> is called.
    /// </summary>
    public static class BirdFlock
    {
        /// <summary>Birds currently perched, across all spawners.</summary>
        public static int PerchedCount { get; private set; }

        /// <summary>A bird has just landed and started perching.</summary>
        public static event Action<Bird> BirdPerched;

        /// <summary>A bird has stopped perching: it was scared off, its time ran out, or it was destroyed.</summary>
        public static event Action<Bird> BirdLeftPerch;

        /// <summary>Added to every spawner's max birds on screen at once.</summary>
        public static int BonusMaxBirds { get; set; }

        /// <summary>Added to the perch time of each bird when it arrives, in seconds.</summary>
        public static float BonusPerchSeconds { get; set; }

        /// <summary>Clears all bonuses, e.g. when a run ends.</summary>
        public static void ResetBonuses()
        {
            BonusMaxBirds = 0;
            BonusPerchSeconds = 0f;
        }

        internal static void NotifyPerched(Bird bird)
        {
            PerchedCount++;
            BirdPerched?.Invoke(bird);
        }

        internal static void NotifyLeftPerch(Bird bird)
        {
            PerchedCount = Mathf.Max(0, PerchedCount - 1);
            BirdLeftPerch?.Invoke(bird);
        }

        // Static state survives entering Play mode when domain reload is disabled, so clear it on every start.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PerchedCount = 0;
            BirdPerched = null;
            BirdLeftPerch = null;
            ResetBonuses();
        }
    }
}
