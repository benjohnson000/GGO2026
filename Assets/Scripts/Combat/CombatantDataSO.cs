using UnityEngine;

namespace ggo2026
{
    /// <summary>Starting data for a player or enemy. Enemy intents and affixes will be added here or in a subclass later.</summary>
    [CreateAssetMenu(menuName = "GGO2026/Combatant", fileName = "new Combatant")]
    public class CombatantDataSO : ScriptableObject
    {
        [SerializeField] private string displayName;

        [Min(1)]
        [SerializeField] private int maxHealth = 50;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public int MaxHealth => maxHealth;

        /// <param name="health">Health carried over from an earlier fight. Negative means full health.</param>
        public Combatant CreateCombatant(int health = -1)
        {
            return new Combatant(DisplayName, maxHealth, health);
        }
    }
}
