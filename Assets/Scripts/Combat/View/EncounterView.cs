using System.Collections.Generic;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Builds a <see cref="Combat"/> from combatant data and shows it on two <see cref="CombatantView"/>s,
    /// the player on the left of the board and the enemy on the right. Until tile effects and enemy intents exist,
    /// the fight is driven from this component's context menu (Debug: …) in Play mode.
    /// </summary>
    public class EncounterView : MonoBehaviour
    {
        [SerializeField] private CombatantDataSO playerData;
        [SerializeField] private CombatantDataSO enemyData;
        [SerializeField] private CombatantView playerView;
        [SerializeField] private CombatantView enemyView;

        [Header("Debug")]
        [Min(1)]
        [Tooltip("Amount used by the Debug context menu actions.")]
        [SerializeField] private int debugAmount = 5;

        [Tooltip("Status applied by the Debug: Apply Status actions.")]
        [SerializeField] private StatusDataSO debugStatus;

        /// <summary>The fight rules, or null before Start.</summary>
        public Combat Combat { get; private set; }

        private void Start()
        {
            if (playerData == null || enemyData == null)
            {
                Debug.LogError($"{name}: EncounterView needs player and enemy CombatantDataSOs assigned.", this);
                enabled = false;
                return;
            }

            // Health carry-over between fights needs run state, which does not exist yet; both start at full health.
            Combat = new Combat(playerData.CreateCombatant(), enemyData.CreateCombatant());
            if (playerView != null) playerView.Bind(Combat.Player);
            if (enemyView != null) enemyView.Bind(Combat.Enemy);
        }

        /// <summary>Shows events from the combat. Instant for now; animation comes with the shared board/combat timeline.</summary>
        public void Play(List<CombatEvent> events)
        {
            foreach (CombatEvent combatEvent in events)
            {
                if (playerView != null) playerView.Apply(combatEvent);
                if (enemyView != null) enemyView.Apply(combatEvent);
            }
        }

        [ContextMenu("Debug: Damage Enemy")]
        private void DebugDamageEnemy() => DebugRun(() => new DealDamageAction(Combat.Player, Combat.Enemy, debugAmount));

        [ContextMenu("Debug: Damage Player")]
        private void DebugDamagePlayer() => DebugRun(() => new DealDamageAction(Combat.Enemy, Combat.Player, debugAmount));

        [ContextMenu("Debug: Player Gains Block")]
        private void DebugPlayerBlock() => DebugRun(() => new GainBlockAction(Combat.Player, debugAmount));

        [ContextMenu("Debug: Heal Player")]
        private void DebugHealPlayer() => DebugRun(() => new HealAction(Combat.Player, debugAmount));

        [ContextMenu("Debug: Apply Status To Enemy")]
        private void DebugStatusEnemy() => DebugRun(() => new ApplyStatusAction(Combat.Enemy, debugStatus, debugAmount));

        [ContextMenu("Debug: Apply Status To Player")]
        private void DebugStatusPlayer() => DebugRun(() => new ApplyStatusAction(Combat.Player, debugStatus, debugAmount));

        [ContextMenu("Debug: Start Player Turn")]
        private void DebugPlayerTurn() => DebugRun(() => new StartTurnAction(Combat.Player));

        [ContextMenu("Debug: Start Enemy Turn")]
        private void DebugEnemyTurn() => DebugRun(() => new StartTurnAction(Combat.Enemy));

        private void DebugRun(System.Func<CombatAction> createAction)
        {
            if (Combat == null)
            {
                Debug.LogWarning($"{name}: enter Play mode before using the debug actions.", this);
                return;
            }

            CombatAction action = createAction();
            if (action is ApplyStatusAction && debugStatus == null)
            {
                Debug.LogWarning($"{name}: assign a Debug Status first.", this);
                return;
            }
            Play(Combat.Execute(action));
        }
    }
}
