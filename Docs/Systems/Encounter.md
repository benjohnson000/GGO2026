# Encounter (turn flow and enemy intents)

`Assets/Scripts/Encounter/`: the turn flow of one fight. The enemy telegraphs an intent with a countdown in
swaps, and its turn comes when the countdown reaches 0. It builds on [Combat](Combat.md): intents create
`CombatAction`s, and the countdown is combat state, so statuses and charms can change it.

| Type | Role |
|---|---|
| `Encounter` | Plain C#. Owns the `Combat`, picks intents, ticks the countdown, runs enemy turns. |
| `EnemyDataSO` | A `CombatantDataSO` with a list of `EnemyIntent`s and a list of `RewardSource`s offered after a win (default 10 coins; see [Rewards](Rewards.md)). `CreateRewards(rng)` rolls them. **Create › GGO2026 › Enemy**. |
| `EnemyIntent` | Serializable: optional `label`, `countdown`, `weight` and a `[SerializeReference]` list of `IntentAction`s. |
| `IntentAction` (`Intents/`) | One thing an intent does: `AttackIntent`, `BlockIntent`, `ApplyStatusIntent`. |
| `IntentChangedEvent` | A `CombatEvent` logged when a new intent is picked. |

## Public API

```csharp
Encounter encounter = new Encounter(combat, enemyData.Intents, rng);
List<CombatEvent> events = encounter.Begin();        // once: picks the first intent
List<CombatEvent> events = encounter.AdvanceSwap();  // after every accepted swap has fully resolved
EnemyIntent intent = encounter.CurrentIntent;
bool over = encounter.IsOver, won = encounter.PlayerWon;
```

## Turn flow

`AdvanceSwap()`:

1. Does nothing if the fight is over.
2. Runs `EndSwapAction` (per-swap triggers such as per-swap poison). Stops here if someone died or the
   enemy has no intent.
3. Ticks the enemy's countdown by 1 (`ChangeCountdownAction(enemy, -1)`).
4. If the countdown is now 0 (checked after resolving, since a listener may have changed it), the enemy
   takes its turn:

```
StartTurn(Enemy)     // enemy block lost, enemy per-turn poison ticks
intent actions       // queued in order, then resolved together
StartTurn(Player)    // player block lost, player per-turn poison ticks
pick next intent     // IntentChangedEvent, then SetCountdownAction(enemy, intent.Countdown)
```

Each step stops if someone has died. Swaps that only tick the countdown are not turns, so block and
per-turn poison are untouched by them (DESIGN.md › Turns).

Passives (not built yet) will fire between the board resolving and `AdvanceSwap`, as DESIGN.md's turn
order requires.

## Countdown

`Combatant.IntentCountdown` holds the swaps left. Only the enemy uses it. It changes only through actions,
which log a `CountdownChangedEvent` and fire `CombatListener.OnCountdownChanged`:

- `SetCountdownAction(target, swaps)`: used when a new intent is picked.
- `ChangeCountdownAction(target, swaps)`: adds swaps (negative removes them). It never goes below 0.

**Stun** (DESIGN.md: stun adds swaps) should be a status that queues `ChangeCountdownAction(Owner, n)`.
The status itself is not built yet. Charms such as "when an enemy attack countdown reaches 1, gain 3 block"
use `OnCountdownChanged`.

## Picking intents

Random by weight each time: an intent with `weight` 2 is twice as likely as one with weight 1. The same
intent can come up twice in a row. With no intents (or a plain `CombatantDataSO` instead of an
`EnemyDataSO`) the enemy never acts and a warning is logged.

`EnemyIntent.Describe()` returns the `label` if set, otherwise its actions' descriptions joined
("Attack 6, Block 4"), or "Wait" if it has none.

## Intent actions

| Action | Fields | Does |
|---|---|---|
| `AttackIntent` | `amount` | `DealDamageAction(enemy, player, amount)` |
| `BlockIntent` | `amount` | `GainBlockAction(enemy, amount)`. Lost at the start of the enemy's next turn. |
| `ApplyStatusIntent` | `status`, `stacks`, `target` (Opponent = player, Owner = enemy) | `ApplyStatusAction` |

To add one, subclass `IntentAction` (`[Serializable]`, parameterless constructor, tuning fields only) and
implement `CreateAction` and `Describe`. It appears in the inspector's type picker automatically.

## Views

- `EncounterView` builds the `Encounter` in `Awake` and calls `Begin()` in `Start`. It exposes
  `AdvanceSwap()` and `IsOver` for `BoardView`. Its optional `victoryRoot`/`defeatRoot` are shown when a
  `CombatantDiedEvent` plays. When the enemy dies it waits `rewardDelay` (0.75 s), then opens the UI scene's
  [`RewardScreen`](Rewards.md) with the enemy's rewards; Skip/Continue calls `Restart()`. `Restart()` reloads the
  scene through `SceneController`; a button's OnClick can also call it.
- `BoardView` calls `encounter.AdvanceSwap()` right after an accepted `TrySwap` and plays those events
  when the swap's animations finish and every projectile has landed (`EncounterView.IsPlaying`).
  It then calls `EncounterView.Settle()` so the [health bars'](Combat.md#health-bar) underfills catch up.
  It stops accepting input once the fight is over.
- `CombatantView.intentText` (enemy only) shows "Attack 6 in 3".
- **Debug** (⋮ menu on `EncounterView`, Play mode): *Advance Swap* ticks the countdown without a swap;
  *Delay Enemy Intent* adds `debugAmount` swaps.

## Not built yet

- The Stun status, enemy tiles, affixes, and scripted or fixed intent patterns.
- A pause or animation before the enemy acts; enemy turn events show instantly after the board settles.
- A next encounter after a win: the reward screen's Continue reloads the same fight.
