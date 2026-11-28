# Rewards

`Assets/Scripts/Rewards/`: the list of rewards shown after a win. Each enemy lists what it gives; when it dies,
the UI scene's `RewardScreen` fills its list with one row per reward. Clicking a row collects it and removes the
row. The button under the list reads **Skip** while anything is left and **Continue** once everything is
collected. Either way it closes the screen and starts the next fight (for now, the same fight reloaded).

| Type | Role |
|---|---|
| `RewardSource` | Authoring data on `EnemyDataSO.rewards` (`[SerializeReference]`, type picker like intent actions). `Create(rng)` rolls a fresh `Reward` for one win, or null |
| `CoinRewardSource` | `min`/`max` coins (default 10/10), optional `icon` |
| `TileChoiceRewardSource` | `choices` (default 3) distinct tiles drawn at random from `options`, optional `icon` |
| `Reward` | Plain C#. `Title`, `Icon`, `IsClaimed`, `Claim(context)`, `Claimed` event. Collected once |
| `CoinReward` | Adds `Amount` to the run's purse and flies the coins from the row to the counter |
| `TileChoiceReward` | Opens the tile choice screen with its `Options`; collected once a tile is picked (`PickedTile`) |
| `RewardClaimContext` | What a reward may need when claimed: `Coins` (`CoinsView`), `TileChoice`, `Origin` (row's world position) |
| `ITileChoicePresenter` | Interface for the pick-a-tile screen: `Show(options, onPicked)`, `onPicked(null)` if closed without picking |
| `RewardScreen` | Singleton in the UI scene. `Show(rewards, onFinished)`, `Hide()`, `IsOpen`, `AllClaimed` |
| `RewardListItemView` | One row (on the `RewardListItem` prefab): title, optional icon, button |

## Flow

1. `EncounterView` shows the enemy's `CombatantDiedEvent`, shows `victoryRoot` if set, and waits `rewardDelay`
   (0.75 s).
2. It calls `EnemyDataSO.CreateRewards(rng)` and `RewardScreen.instance.Show(rewards, onFinished)`.
3. The player clicks rows. `RewardScreen` calls `reward.Claim(context)`; when the reward raises `Claimed` the
   row is destroyed and the button label is refreshed.
4. Skip/Continue hides the screen (uncollected rewards are lost) and calls `onFinished`, which runs
   `EncounterView.Restart()`.

Without a `RewardScreen` (UI scene not loaded) a warning is logged and the fight just ends.

## Adding a reward type

1. Subclass `Reward`: implement `Title` and `OnClaim(context)`, and call `MarkClaimed()` once it has actually been
   given (that may be later, e.g. after a choice screen).
2. Subclass `RewardSource` (`[Serializable]`, parameterless constructor, tuning fields only) and return the new
   reward from `Create`. It appears in the enemy inspector's type picker.
3. If it needs something new from the scene, add it to `RewardClaimContext` and fill it in
   `RewardScreen.OnItemClicked`.

## Scene setup (UI scene)

1. On the `RewardListItem` prefab root, add `RewardListItemView`. Assign `button` (the root's Button),
   `titleText` (`Text (Normal)`) and `icon` (`icon`).
2. Add `RewardScreen` to `Canvas › RewardScreen`. It must stay active, so the panel it toggles is a child:
   - `panel`: `RewardsList`
   - `listRoot`: `ListRoot`
   - `itemPrefab`: `RewardListItem`
   - `continueButton` and `continueLabel`: the Skip button and its TMP text
   - `tileChoiceScreen`: leave empty until the pick-a-tile screen exists
3. On each enemy asset, edit **Rewards**. New enemies start with 10 coins. Add a `TileChoiceRewardSource` and
   fill its `options` to offer tiles.
4. Clear `EncounterView.victoryRoot` in the Game scene if the old victory panel should no longer show.

## Not built yet

- The pick-a-tile screen (implement `ITileChoicePresenter` and assign it). Until then, clicking a tile reward logs a
  warning and leaves it in the list.
- Adding the picked tile to the deck: there is no run deck yet, since each fight builds its pool from a
  `StartingDeckSO`. `TileChoiceReward.PickedTile` holds the choice.
- Charm rewards (charms don't exist yet), a next fight other than reloading this one, and a reward pool or rarity
  tiers (see DESIGN.md › Open Questions).
