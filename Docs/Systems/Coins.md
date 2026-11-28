# Coins

`Assets/Scripts/Coins/`: the player's currency. The rules are a plain C# purse that changes instantly; the
counter in the UI scene's top bar shows changes in step with playback, so coins from a tile count up as they
land on the icon.

| Type | Role |
|---|---|
| `CoinPurse` | `Amount`, `Gain(amount)`, `CanAfford(cost)`, `TrySpend(cost)`, `Changed(old, new)`. Rules only |
| `CoinsView` | Singleton on the UI scene's top bar `Coins` object. Owns the run's purse, shows the count, flies coins in |
| `GainCoinsEffect` (`Tiles/Effects/`) | Tile effect: gain `amount` coins (default 1) |
| `CoinsGainedEvent` (`Board/Events/`) | `TileEffectActivatedEvent` subclass with `Amount`, emitted by `GainCoinsEffect` |

## Where coins come from

| Source | Rules | Shown |
|---|---|---|
| A tile with `GainCoinsEffect` | Board adds `Amount` to `Board.Coins` as it logs the `CoinsGainedEvent` | Flies from the tile to the counter once the tile has popped (`BoardView.PlayEffectsOfVanishedTiles`) |
| Defeating an enemy | A `CoinRewardSource` in `EnemyDataSO.rewards` (default 10) becomes a row on the [reward screen](Rewards.md); `CoinReward` adds the coins when the row is clicked | Flies from the clicked row to the counter |

To add another source: call `CoinsView.instance.Purse.Gain(n)` where the rules decide it, then
`CoinsView.instance.ShowGain(n, worldOrigin, colour, style)` when the presentation reaches it (or
`ShowGain(n)` for no flight). Every gain needs a `ShowGain`, or the counter falls behind the purse.

## Ownership and lifetime

- There is no run state yet, so `CoinsView` owns the `CoinPurse` (made in `Awake` from `startingCoins`). The
  UI scene stays loaded, so coins carry over between fights and restarts. Move the purse to run state when it
  exists.
- `BoardView` gives the board the purse (`Board.Coins`) in `Start` and again before each swap if it was missing,
  since the UI scene may load after `Game`. Without it, `GainCoinsEffect` returns null and does nothing.
- `CoinReward` looks up `CoinsView.instance` when claimed. Without one it logs a warning and the row stays.

## CoinsView

The number is `shown`, not the live purse: gains appear through `ShowGain`, spends (`TrySpend`) appear at once
through `Purse.Changed`.

`ShowGain(amount, origin, colour, style = null)` splits the amount over up to `maxProjectilesPerGain`
projectiles, launched `launchInterval` apart. Each is a [`DamageProjectile`](Combat.md#views) flown in a
[projectile style](Combat.md#projectile-styles): the tile effect's own `Projectile`, else `defaultProjectile`.
Each adds its share to the count when it lands and pulses the icon. With no style or prefab, the count goes up
at once. Projectiles are moved to the UI scene, so they still arrive if the fight's scene unloads.

| Field | Default | Meaning |
|---|---|---|
| `startingCoins` | 0 | Coins at the start |
| `countText` | – | TMP text showing the count |
| `numberFormat` | `000` | `int.ToString` format. `0` for no padding |
| `target` | this object | Where coins land: the coin icon |
| `pulseTarget` | `target` | Scaled up to `pulseScale` (1.25) and back over `pulseDuration` (0.15 s) as each coin lands |
| `collectSound` | – | Optional `SoundProfileSO`, played through `SFXManager` each time coins are added to the count (each landing coin, or once for an instant gain) |
| `defaultProjectile` | – | Style used when the source has none |
| `maxProjectilesPerGain` | 10 | Most projectiles for one gain |
| `launchInterval` | 0.06 s | Gap between a gain's projectiles |

Tile coins are tinted by the tile's shape colour when the style's `tintWithTileColour` is on; turn it off on a
coin style to keep gold art gold. Reward-screen coins pass white.

**Debug:** in Play mode, the `CoinsView` component's ⋮ menu has *Debug: Gain 10 Coins*, which flies 10 coins
from the world origin.

## Scene setup (UI scene)

1. Add `CoinsView` to `Canvas › TopBarMenu › Coins`.
2. Assign `countText` to the `Text (Normal)` child and `target` to `Icon`.
3. Make a coin projectile prefab: a `SpriteRenderer` (coin sprite) with a `DamageProjectile`, optionally a trail.
   Give its renderers a sorting layer/order that draws above the UI canvas.
4. Create a **Create › GGO2026 › Projectile Style** for it (turn off `tintWithTileColour` for gold art, add an
   impact sound if wanted) and assign it to `defaultProjectile`.

For a coin tile: add a `GainCoinsEffect` to a tile's effects in the tile inspector, and optionally its own
`Projectile` style.

## Not built yet

- Spending (shop, rewards), and saving coins between sessions.
- Charm hooks on coin gains ("whenever you gain coins…"). `CoinPurse.Changed` is the place to start.
