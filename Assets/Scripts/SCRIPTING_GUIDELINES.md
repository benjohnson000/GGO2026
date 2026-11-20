# Scripting Guidelines

Practices to follow when creating or modifying scripts in this project. When in doubt, match the surrounding code.

## 1. Before You Start

- Read the existing script(s) you are touching and any neighbours in the same folder.
- Check `Utilities/` for something reusable (`Singleton<T>`, `StateMachine<T>`, `SceneController`, `SFXManager`, `MusicManager`, `SaveLoadSystem`, transitions) before writing new infrastructure.
- Keep changes scoped to the task. Do not reformat or refactor unrelated code in the same change.
- Never edit `.meta` files by hand. Move/rename scripts inside the Unity Editor (or move the `.cs` and its `.meta` together) so GUID references survive.

## 2. Project Structure

| Folder | Purpose | Namespace |
|---|---|---|
| `Scripts/Cards/` | Card game logic, data, and views | `ggo2026` |
| `Scripts/Utilities/` | Reusable, game-agnostic systems | `benjohnson` |

- One top-level type per file; the file name matches the type name.
- Put new code in the folder matching its purpose. Create a subfolder when a system grows to several related files (see `StateMachine/`, `Transitions/`, `Sound/`, `SaveLoad/`).
- Game-specific code must not live in `Utilities/`, and `Utilities/` must not depend on game code.

## 3. Naming & Style

- **Types, methods, properties, enums:** `PascalCase`.
- **Fields (private/serialized), locals, parameters:** `camelCase`. No `m_` or `_` prefixes.
- **Constants:** `PascalCase` (e.g. `PrefabPath`).
- **Interfaces:** prefix with `I`. **Enums used as state keys:** prefix type with `E` where used as a generic argument.
- 4-space indentation, Allman braces (opening brace on its own line).
- Single-statement guards may stay on one line (`if (x == null) return;`).
- Always declare accessibility explicitly (`private`, `public`, `protected`).
- Prefer `private` by default; expose the minimum needed.

## 4. Namespaces

- Every script lives in a namespace: `ggo2026` for game code, `benjohnson` for shared utilities.
- Do not leave scripts in the global namespace.

## 5. MonoBehaviour Conventions

- Expose inspector fields with `[SerializeField] private`, not `public`. Add `[Tooltip]` when the purpose isn't obvious.
- Use `Awake` for self-initialisation, `Start` for anything that depends on other objects being ready, `OnEnable`/`OnDisable` to subscribe/unsubscribe to events.
- Always unsubscribe from events/delegates you subscribed to. Pair every `+=` with a `-=`.
- Subclasses overriding `Awake`/`OnDestroy` of a base (e.g. `Singleton<T>`) **must call `base.Awake()` / `base.OnDestroy()`**.
- Do not use `Update` for work that can be event-driven or coroutine-driven.
- Avoid `Find*`, `GetComponent*` and `Camera.main` in per-frame code; cache the result.
- Lazy-fetch optional references with a null check, as in `CardGameObject.GetRenderer()`.
- Keep data and presentation separate: plain C# classes for data (`Card`), MonoBehaviours for views (`CardGameObject`) that hold a reference to the data.

## 6. Patterns Used in This Project

- **Singletons:** inherit `Singleton<T>`; access via `T.instance`. Don't hand-roll static instances.
- **State machines:** inherit `StateMachine<EState>` and `BaseState<EState>`; register states in `Awake` before calling `TransitionToState`. Drive behaviour changes through state transitions, not scattered bool flags.
- **Factories:** use a static `Create(...)` method for prefab-backed objects (see `CardGameObject.Create`) so loading and binding live in one place.
- **Config / tuning data:** use ScriptableObjects (see `TransitionInfoSO`) rather than hard-coded values.
- **Scene loading and transitions:** go through `SceneController` / `TransitionController`, not `SceneManager` directly.
- **Audio:** go through `SFXManager` / `MusicManager`.
- **Persistence:** go through `SaveLoadSystem`.

## 7. Error Handling & Logging

- Validate inputs and references at boundaries; fail early with a clear `Debug.LogError` that names the object and the problem, and pass the context object (`Debug.LogError(msg, this)`).
- Don't swallow exceptions silently.
- Don't leave stray `Debug.Log` calls in committed code. Gate diagnostic logging behind a serialized flag (see `logState` in `StateMachine`).
- Return `null`/early after logging an error rather than continuing in a broken state.

## 8. Performance & Memory

- Avoid per-frame allocations: no `new` collections, LINQ, string concatenation, or boxing in `Update`/hot paths.
- Reuse and cache collections where practical.
- Prefer coroutines or async-style waits over polling.
- Load assets with serialized references where possible; use `Resources.Load` only for deliberate, documented cases (and note the expected path in a comment, as `CardGameObject` does).
- Destroy what you instantiate, and clean up in `OnDestroy`.

## 9. Documentation

- Add `/// <summary>` XML comments to public types and non-obvious public/protected members.
- Comment the **why** (constraints, gotchas, ordering requirements), not the **what**.
- Document required usage contracts, such as "subclasses must call `base.Awake()`".

## 10. Modifying Existing Scripts

- Preserve public APIs unless the task requires changing them; if you change one, update all callers.
- Renaming a serialized field loses its inspector value. Use `[FormerlySerializedAs("oldName")]` when renaming.
- Don't change an enum's existing values/order if it is serialized in scenes, prefabs, or save data; append new values instead.
- Changes to save data structures must stay backward compatible, or include a migration for existing saves.
- After editing, confirm the Unity project compiles with no errors or new warnings and that the affected scene/prefab still works.

## 11. Before Committing

- [ ] Compiles cleanly in Unity; no new console errors or warnings
- [ ] Correct namespace, file name matches type name
- [ ] No unused `using` directives, dead code, or debug logging
- [ ] Events unsubscribed; no leaked coroutines
- [ ] Inspector references still assigned in affected scenes/prefabs
- [ ] `.cs` and `.meta` files committed together
- [ ] Manually tested in Play mode
