using System.Collections.Generic;
using benjohnson;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ggo2026
{
    /// <summary>
    /// Builds an <see cref="Encounter"/> (and its <see cref="Combat"/>) from combatant data and shows it on two
    /// <see cref="CombatantView"/>s, the player on the left of the board and the enemy on the right. A
    /// <see cref="BoardView"/> that references this component applies tile effects to the fight and calls
    /// <see cref="AdvanceSwap"/> after each accepted swap, which runs the enemy's intents.
    /// </summary>
    public class EncounterView : MonoBehaviour
    {
        [SerializeField] private CombatantDataSO playerData;

        [Tooltip("Use an Enemy asset (Create › GGO2026 › Enemy) so the enemy has intents.")]
        [SerializeField] private CombatantDataSO enemyData;

        [SerializeField] private CombatantView playerView;
        [SerializeField] private CombatantView enemyView;

        [Tooltip("Optional. Shown when the enemy dies.")]
        [SerializeField] private GameObject victoryRoot;

        [Tooltip("Optional. Shown when the player dies.")]
        [SerializeField] private GameObject defeatRoot;

        [Tooltip("Optional. Projectile flown from a tile to whoever it affects (damage, block, heal, status, countdown) when the " +
                 "effect has no Projectile of its own; the change shows when it lands. Without either, changes show instantly.")]
        [SerializeField] private ProjectileStyleSO defaultProjectile;

        [Header("Debug")]
        [Min(1)]
        [Tooltip("Amount used by the Debug context menu actions.")]
        [SerializeField] private int debugAmount = 5;

        [Tooltip("Status applied by the Debug: Apply Status actions.")]
        [SerializeField] private StatusDataSO debugStatus;

        /// <summary>The fight rules, or null before Awake (or if the data is missing).</summary>
        public Combat Combat => Encounter?.Combat;

        /// <summary>The turn flow, or null before Awake (or if the data is missing).</summary>
        public Encounter Encounter { get; private set; }

        public bool IsOver => Encounter != null && Encounter.IsOver;

        /// <summary>True while events are waiting to be shown, e.g. behind a projectile still in flight.</summary>
        public bool IsPlaying => queue.Count > 0;

        // Events waiting to be shown, in order. An event is shown only once it and everything before it is ready,
        // so a projectile still in flight holds back later events and the views never show them out of order.
        private readonly Queue<QueuedEvent> queue = new Queue<QueuedEvent>();
        private bool settlePending;

        private class QueuedEvent
        {
            public CombatEvent Event;
            public bool Ready;
        }

        // Built in Awake, from this component's own data only, so other objects can read it in their Start.
        private void Awake()
        {
            if (playerData == null || enemyData == null)
            {
                Debug.LogError($"{name}: EncounterView needs player and enemy CombatantDataSOs assigned.", this);
                enabled = false;
                return;
            }

            EnemyDataSO enemy = enemyData as EnemyDataSO;
            if (enemy == null) Debug.LogWarning($"{name}: {enemyData.name} is not an Enemy asset, so the enemy has no intents.", enemyData);

            // Health carry-over between fights needs run state, which does not exist yet; both start at full health.
            Combat combat = new Combat(playerData.CreateCombatant(), enemyData.CreateCombatant());
            Encounter = new Encounter(combat, enemy != null ? enemy.Intents : null, new System.Random());
        }

        private void Start()
        {
            if (Encounter == null) return;

            if (victoryRoot != null) victoryRoot.SetActive(false);
            if (defeatRoot != null) defeatRoot.SetActive(false);
            if (playerView != null) playerView.Bind(Combat.Player, Combat.PreviewDamage(Combat.Player));
            if (enemyView != null) enemyView.Bind(Combat.Enemy, Combat.PreviewDamage(Combat.Enemy));
            Play(Encounter.Begin());
        }

        /// <summary>
        /// Ticks the enemy's countdown after an accepted swap and runs its turn if the intent fires.
        /// Returns the events for the caller to <see cref="Play"/> once the board has finished animating.
        /// </summary>
        public List<CombatEvent> AdvanceSwap()
        {
            return Encounter != null ? Encounter.AdvanceSwap() : new List<CombatEvent>();
        }

        /// <summary>Shows events from the fight, in order. Instant unless queued behind a projectile still in flight.</summary>
        public void Play(IReadOnlyList<CombatEvent> events)
        {
            Enqueue(events, null, Color.white, null);
        }

        /// <summary>
        /// Shows events caused by a tile at world position <paramref name="origin"/>. Each change to a combatant (see
        /// <see cref="FliesToTarget"/>) launches a projectile from there to that combatant and shows once it lands;
        /// events after it wait for it. Projectiles use <paramref name="style"/> (the firing effect's
        /// <see cref="TileEffect.Projectile"/>), or <see cref="defaultProjectile"/> when null, and take <paramref name="tileColour"/>.
        /// </summary>
        public void Play(IReadOnlyList<CombatEvent> events, Vector3 origin, Color tileColour, ProjectileStyleSO style = null)
        {
            Enqueue(events, origin, tileColour, style != null ? style : defaultProjectile);
        }

        /// <summary>
        /// Call when a whole chain of effects has been played. Once the last of it is shown, the health bars' underfills
        /// catch up. An empty queue can't be used for this, as it also empties between cascades.
        /// </summary>
        public void Settle()
        {
            settlePending = true;
            Flush();
        }

        private void Enqueue(IReadOnlyList<CombatEvent> events, Vector3? origin, Color tileColour, ProjectileStyleSO style)
        {
            foreach (CombatEvent combatEvent in events)
            {
                QueuedEvent queued = new QueuedEvent { Event = combatEvent, Ready = true };
                if (origin.HasValue && FliesToTarget(combatEvent) && TryLaunch(combatEvent, origin.Value, tileColour, style, queued))
                {
                    queued.Ready = false;
                }
                queue.Enqueue(queued);
            }
            Flush();
        }

        /// <summary>True for changes done to a combatant. Deaths, turn starts and new intents only follow from those.</summary>
        private static bool FliesToTarget(CombatEvent combatEvent)
        {
            return combatEvent is DamageEvent || combatEvent is BlockChangedEvent || combatEvent is HealedEvent
                || combatEvent is StatusChangedEvent || combatEvent is CountdownChangedEvent;
        }

        /// <summary>Launches a projectile at the event's target that marks <paramref name="queued"/> ready when it lands.</summary>
        private bool TryLaunch(CombatEvent combatEvent, Vector3 origin, Color tileColour, ProjectileStyleSO style, QueuedEvent queued)
        {
            if (style == null || style.Prefab == null) return false;
            CombatantView targetView = ViewFor(combatEvent.Target);
            if (targetView == null) return false;

            DamageProjectile projectile = style.Spawn(origin);
            // Keep it in this fight's scene rather than whichever scene is active, so it unloads with the fight.
            SceneManager.MoveGameObjectToScene(projectile.gameObject, gameObject.scene);
            projectile.Launch(style, origin, targetView.HitPoint, tileColour, () =>
            {
                // A projectile cut short by the scene unloading must not update UI that is being destroyed.
                if (this == null || !gameObject.scene.isLoaded) return;

                queued.Ready = true;
                Flush();
            });
            return true;
        }

        private CombatantView ViewFor(Combatant combatant)
        {
            if (playerView != null && playerView.Combatant == combatant) return playerView;
            if (enemyView != null && enemyView.Combatant == combatant) return enemyView;
            return null;
        }

        /// <summary>Shows queued events from the front until one is still waiting.</summary>
        private void Flush()
        {
            while (queue.Count > 0 && queue.Peek().Ready)
            {
                Show(queue.Dequeue().Event);
            }

            if (!settlePending || queue.Count > 0) return;

            settlePending = false;
            if (playerView != null) playerView.Settle();
            if (enemyView != null) enemyView.Settle();
        }

        private void Show(CombatEvent combatEvent)
        {
            if (playerView != null) playerView.Apply(combatEvent);
            if (enemyView != null) enemyView.Apply(combatEvent);

            if (combatEvent is CombatantDiedEvent died) ShowResult(died.Target == Combat.Enemy);
        }

        /// <summary>Reloads this scene to start the fight again. Hook a restart button's OnClick to this.</summary>
        public void Restart()
        {
            SceneController scenes = SceneController.instance;
            if (scenes == null)
            {
                Debug.LogError($"{name}: no SceneController found, so the fight can't restart. Press Play from PersistentScene.", this);
                return;
            }
            scenes.LoadEnvironment(gameObject.scene.name);
        }

        private void ShowResult(bool playerWon)
        {
            if (playerWon && victoryRoot != null) victoryRoot.SetActive(true);
            if (!playerWon && defeatRoot != null) defeatRoot.SetActive(true);
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

        [ContextMenu("Debug: Delay Enemy Intent")]
        private void DebugDelayIntent() => DebugRun(() => new ChangeCountdownAction(Combat.Enemy, debugAmount));

        [ContextMenu("Debug: Advance Swap")]
        private void DebugAdvanceSwap()
        {
            if (Encounter == null)
            {
                Debug.LogWarning($"{name}: enter Play mode before using the debug actions.", this);
                return;
            }
            Play(Encounter.AdvanceSwap());
            Settle();
        }

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
            Settle();
        }
    }
}
