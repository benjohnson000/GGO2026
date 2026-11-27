# Ambient Birds

Decorative birds that fly in from off screen, perch on a point in the scene, then fly away. No gameplay effect on
their own, but [`BirdFlock`](#charm-hooks) exposes hooks for future charms.

Code: `Assets/Scripts/Ambient/` (`ggo2026`). Prefab: `Assets/Prefabs/Bird.prefab`.

## Pieces

- **`BirdSpawner`** (scene object). Every `spawnIntervalSeconds` (random min–max) it picks a random free landing
  point and spawns a random prefab from `birdPrefabs` there (an empty slot skips that spawn), up to
  `maxBirds + BirdFlock.BonusMaxBirds` at once. Each point holds one bird at a time. Landing points are
  the `landingPoints` list, or the spawner's children if the list is empty. Points are drawn as cyan gizmos.
  With no `viewCamera` assigned it waits for `Camera.main`, since the camera lives in the additively loaded `Camera`
  scene and may not exist when the spawner wakes.
- **`Bird`** (on the prefab root). `Land(landAt, perchSeconds, camera, gone)` moves it off screen along a random angle
  in `arriveAngleRange`, then a coroutine flies it in (slowing within `landingSlowRadius`), perches (flipping to look
  around every `lookAroundSeconds`), flies off along a random angle in `leaveAngleRange`, and destroys itself.
  `gone` runs from `OnDestroy`, which frees the point.
  While perched, it flies away early as soon as the pointer is over its `hoverArea` (a `Collider2D`, found in children
  if unset). This polls `InputManager.PointerPosition` each frame with `Collider2D.OverlapPoint`, because
  `OnMouseEnter` doesn't fire with the Input System. No collider means the bird can't be scared off.
- **`BirdFlock`** (static). Hooks for gameplay code; see below.

## Charm hooks

Charms aren't built yet. `BirdFlock` is static so a charm (a `CombatListener`, see [Combat](Combat.md)) can use the
birds without a reference to a scene object.

| Member | Use |
|---|---|
| `PerchedCount` | Birds perched right now, across all spawners |
| `BirdPerched`, `BirdLeftPerch` | Events with the `Bird`. "Left" also fires when a perched bird is destroyed |
| `BonusMaxBirds` | Added to every spawner's `maxBirds`. Still capped by the number of landing points |
| `BonusPerchSeconds` | Added to each bird's perch time when it spawns |
| `ResetBonuses()` | Clears the bonuses, e.g. at the end of a run |

Bonuses are additive, as DESIGN.md asks: a charm adds its amount when gained and subtracts it when lost. They persist
across scenes. All static state is cleared on entering Play mode, so it works with domain reload disabled.

Example, "strikes deal +1 damage per perched bird":

```csharp
public override void ModifyOutgoingDamage(Combat combat, DamageInfo damage)
{
    damage.Amount += BirdFlock.PerchedCount;
}
```

Since the count changes in real time, a damage preview can drift from the hit if a bird arrives or leaves in
between.

## Notes

- The prefab root is the bird's feet: the art is on a child (`gfx`) raised so its feet sit at the root. Place landing
  points where the feet should touch.
- Animation is sprite frames: `flapFrames` loop while flying and `idleFrames` loop while perched, each at its own
  frames per second. An empty list shows the prefab's sprite instead. The art is flipped to face the flight direction;
  set `spriteFacesRight` to match the art.
- "Off screen" is one screen diagonal plus `offscreenMargin` from the landing point, which needs an orthographic
  camera. Birds are spawned unparented, so they unload with the scene.
- Uses scaled time, so birds freeze when `Time.timeScale` is 0.
