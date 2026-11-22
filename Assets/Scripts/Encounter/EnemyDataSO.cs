using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>An enemy: name and health (from <see cref="CombatantDataSO"/>) plus the intents it picks from at random.</summary>
    [CreateAssetMenu(menuName = "GGO2026/Enemy", fileName = "new Enemy")]
    public class EnemyDataSO : CombatantDataSO
    {
        [Tooltip("Picked at random, by weight, each time the previous intent fires. With none, the enemy never acts.")]
        [SerializeField] private List<EnemyIntent> intents = new List<EnemyIntent>();

        public IReadOnlyList<EnemyIntent> Intents => intents;
    }
}
