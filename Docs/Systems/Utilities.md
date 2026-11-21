# Utilities (`benjohnson`)

`Assets/Scripts/Utilities/` — reusable, game-agnostic infrastructure. **Never reference `ggo2026` code
from here.** Use these before writing new infrastructure.

| System | Files | Use it for |
|---|---|---|
| [Singleton](#singletont) | `Singleton.cs` | One global instance of a MonoBehaviour |
| [State machine](#state-machine) | `StateMachine/` | Behaviour modes on a MonoBehaviour |
| [Scenes](#scene-loading) | `SceneController.cs`, `PersistenceCheck.cs` | Loading utility and environment scenes |
| [Transitions](#transitions) | `Transitions/` | Screen fades/wipes during scene changes |
| [Input](#input) | `InputManager.cs` | Pointer press / move / release events |
| [Audio](#audio) | `Sound/` | One-shot SFX and looping music |
| [Save/load](#saveload) | `SaveLoad/SaveLoadSystem.cs` | JSON files in `persistentDataPath` |
| [Misc](#misc) | `Utilities.cs`, `CanvasCameraAssigner.cs`, `MatchWorldCanvasToCamera.cs`, `DisableOnStart.cs` | Small helpers |

## Singleton<T>

```csharp
public class MyManager : Singleton<MyManager>
{
    protected override void Awake() { base.Awake(); if (instance != this) return; /* init */ }
    protected override void OnDestroy() { base.OnDestroy(); /* cleanup */ }
}
// MyManager.instance
```

- First instance to `Awake` wins; duplicates destroy their GameObject.
- Overrides **must** call `base.Awake()` / `base.OnDestroy()`. After `base.Awake()`, return early if
  `instance != this` (see `InputManager`).
- `dontDestroyOnLoad` is optional and only needed outside the additive persistent-scene setup.
- Instances in other additive scenes may not exist yet in your `Awake`/`OnEnable`; retry in `Start`.

## State machine

```csharp
public enum EDoorState { Closed, Open }

public class Door : StateMachine<EDoorState>
{
    private void Awake()
    {
        states.Add(EDoorState.Closed, new DoorClosedState(EDoorState.Closed, this));
        states.Add(EDoorState.Open,   new DoorOpenState(EDoorState.Open, this));
        TransitionToState(EDoorState.Closed);
    }
}

public class DoorClosedState : BaseState<EDoorState>
{
    public DoorClosedState(EDoorState key, StateMachine<EDoorState> ctx) : base(key, ctx) { }
    public override void Enter() { }
    public override void Update() { }
    public override void Exit() { }
}
```

- Register states before the first `TransitionToState`.
- `TransitionToState(key, time)` exits the current state, waits `time` seconds (or **one frame** when 0),
  then enters the new one. A new request cancels a pending one. During the gap `HasState` is false and
  `Update` is not forwarded.
- `currentStateKey`, `HasState` for queries. Tick `logState` in the inspector to log transitions.
- `StateMachine.Update` is `protected` and non-virtual — a subclass must not declare its own `Update`
  (it would hide the base one). Put per-frame work in the state's `Update`.
- Real example: `BoardView` with `BoardIdleState` / `BoardAnimatingState`.

## Scene loading

`SceneController` (in `PersistentScene`) distinguishes:

- **Utility scenes** — loaded additively and kept: `LoadUtility(name)`, `UnloadUtility(name)`.
  The inspector list `startupUtilityScenes` is loaded on `Start`.
- **Environment scene** — exactly one at a time, swapped behind a transition:
  `LoadEnvironment(name, transitionInfo = null)`. Ignored while a transition is running.
  `startupEnvironmentScene` is loaded on `Start`.

Always change gameplay scenes through `LoadEnvironment`, never `SceneManager` directly. Scenes must be
in Build Settings.

`PersistenceCheck` (prefab at `Assets/Scripts/Utilities/PersistenceCheck.prefab`): drop it in any scene
so pressing Play there reloads build index 0 (`PersistentScene`) when no `SceneController` exists.

## Transitions

```
SceneController.LoadEnvironment
  └─ TransitionController.Transition(info, switchScene)
        close behaviour (instantiate → Trigger(duration, colour) → …)
        switchScene coroutine (unload old env, load new env)
        destroy close; instantiate open → Trigger → destroy
```

- `TransitionController` is a `Singleton` on a `Canvas` (in `PersistentScene`). Behaviours are
  instantiated under it.
- `TransitionInfoSO` (**Create › Transition**) holds a `close` and an `open` entry, each with a
  `TransitionBehavior` prefab, colour and duration. The `SceneController` has a `defaultTransition`.
- Built-in behaviours (`Transitions/Prefabs/`): `FadeCloseTransition`, `FadeOpenTransition`,
  `WipeTransition` (open/close, horizontal/vertical). They use unscaled time, so they run while paused.
- New effect → subclass `TransitionBehavior`, implement `IEnumerator Trigger(float duration, Color color)`,
  make a prefab, reference it from a `TransitionInfoSO`.
- `TransitionTrigger` component: set `sceneName` + `transition`, call `TransitionToEnvironment()` from a
  UnityEvent (e.g. a UI Button).
- `try/finally` guarantees the overlay is destroyed and the lock released even if loading fails.

## Input

`InputManager` (in `GameUtilities`) wraps the Input System so gameplay never touches it directly.

```csharp
InputManager.instance.PointerPressed  += OnPressed;   // Action<Vector2> screen pixels
InputManager.instance.PointerMoved    += OnMoved;
InputManager.instance.PointerReleased += OnReleased;
Vector2 pos = InputManager.instance.PointerPosition;
```

- Actions are created in code (`<Pointer>/press`, `<Pointer>/position`), so mouse, touch and pen all work
  and no input asset needs wiring.
- Always unsubscribe in `OnDisable`. Because the manager lives in another scene, subscribe again in
  `Start` if it was null in `OnEnable` (pattern in `BoardView.TrySubscribe`).
- Only pointer input exists today. Add keyboard/gamepad actions here rather than reading devices elsewhere.

## Audio

Both are `Singleton`s that need an `AudioSource` assigned. **Neither is placed in a scene yet** — add them
to `GameUtilities` (or another utility scene) before use.

```csharp
[SerializeField] private SoundProfile matchSound;   // clips[] + volume, random clip per play
SFXManager.instance.PlaySound(matchSound);
SFXManager.instance.PlaySoundAtPosition(matchSound, worldPos);

MusicManager.instance.PlayMusic(clip, 0.8f);  // loops; per-track volume × master volume
MusicManager.instance.SetMusicVolume(0.5f);
```

## Save/load

Static `SaveLoadSystem`, JSON via `JsonUtility` in `Application.persistentDataPath/<name>.json`.

```csharp
SaveLoadSystem.Save(data, "run");                       // atomic: temp file then swap
if (SaveLoadSystem.TryLoad("run", out RunSave data)) { }
SaveLoadSystem.Exists("run"); SaveLoadSystem.Delete("run");
```

- Data must be a `[Serializable]` class/struct. No dictionaries, no top-level arrays/lists (wrap them).
  `ScriptableObject` references don't serialize meaningfully — store IDs/names instead.
- Prefer `TryLoad` over `Load` (which returns `null`/default silently on failure).
- File names are validated; invalid characters throw `ArgumentException`.
- On WebGL, `persistentDataPath` is IndexedDB-backed — test saving there if WebGL is chosen.

## Misc

| Type | What it does |
|---|---|
| `Utilities.SpriteUtils.GetPixels(sprite)` | Pixels of a sprite slice (texture must be readable) |
| `Utilities.InputUtils.MouseScreenPosition / MouseWorldPosition` | Direct mouse reads. Prefer `InputManager` for gameplay |
| `Utilities.RandomUtils.RandomEnumValue<T>()` | Random enum value |
| `CanvasCameraAssigner` | Waits for `Camera.main` (which lives in the `Camera` scene) and assigns it to the canvas |
| `MatchWorldCanvasToCamera` | `[ExecuteAlways]` — sizes/centres a world-space canvas to fill the camera view |
| `DisableOnStart` | Deactivates its GameObject in `Start` (handy for editor-only helpers) |
