using System.Collections;
using System.Collections.Generic;
using benjohnson;
using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// Builds a <see cref="Board"/>, turns pointer drags into swaps, and animates the events the board returns.
    /// The board is centred on this transform, one cell per <see cref="BoardViewSettingsSO.CellSize"/> world units.
    /// Input comes from <see cref="InputManager"/>, which must exist in a loaded scene.
    /// </summary>
    public class BoardView : StateMachine<EBoardState>
    {
        [SerializeField] private BoardConfigSO config;

        [Tooltip("Tiles in the pool at the start. Optional: with no deck every tile is a random-colour filler.")]
        [SerializeField] private StartingDeckSO startingDeck;

        [SerializeField] private BoardViewSettingsSO settings;

        private readonly Dictionary<Tile, TileView> views = new Dictionary<Tile, TileView>();
        private List<BoardEvent> pendingEvents;

        private Camera cam;
        private bool subscribed;

        private bool dragging;
        private Vector2Int dragCell;
        private Vector2 dragStartLocal;

        // Reused by every move animation. Only one animation runs at a time.
        private readonly List<Transform> moveTargets = new List<Transform>();
        private readonly List<Vector3> moveFrom = new List<Vector3>();
        private readonly List<Vector3> moveTo = new List<Vector3>();
        private readonly List<float> moveDurations = new List<float>();
        private readonly List<TileView> popping = new List<TileView>();

        /// <summary>The board rules, or null before Start.</summary>
        public Board Board { get; private set; }

        private void Awake()
        {
            states.Add(EBoardState.Idle, new BoardIdleState(EBoardState.Idle, this));
            states.Add(EBoardState.Animating, new BoardAnimatingState(EBoardState.Animating, this));
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            if (config == null || settings == null)
            {
                Debug.LogError($"{name}: BoardView needs a BoardConfigSO and a BoardViewSettingsSO assigned.", this);
                enabled = false;
                return;
            }

            // InputManager lives in another scene and may wake after this object's OnEnable.
            TrySubscribe();
            if (!subscribed) Debug.LogError($"{name}: no InputManager found, so the board can't receive input.", this);

            System.Random rng = new System.Random();
            TilePool pool = new TilePool(config.FillerColours, rng);
            if (startingDeck != null) startingDeck.AddTo(pool);

            Board = new Board(config.Width, config.Height, pool, rng);
            Play(Board.Fill());
        }

        private void OnDisable()
        {
            dragging = false;
            if (!subscribed) return;

            InputManager input = InputManager.instance;
            if (input != null)
            {
                input.PointerPressed -= OnPointerPressed;
                input.PointerMoved -= OnPointerMoved;
                input.PointerReleased -= OnPointerReleased;
            }
            subscribed = false;
        }

        /// <summary>Queues events for playback. Called by Board operations; input is ignored until playback ends.</summary>
        private void Play(List<BoardEvent> events)
        {
            if (events == null || events.Count == 0) return;

            pendingEvents = events;
            TransitionToState(EBoardState.Animating);
        }

        /// <summary>Hands the queued events to the animating state.</summary>
        internal List<BoardEvent> TakePendingEvents()
        {
            List<BoardEvent> events = pendingEvents ?? new List<BoardEvent>();
            pendingEvents = null;
            return events;
        }

        /// <summary>
        /// Animates events in order. Consecutive destroy events pop together, and consecutive fall and spawn
        /// events drop together, so each cascade step reads as one beat.
        /// </summary>
        internal IEnumerator PlayEvents(List<BoardEvent> events)
        {
            int i = 0;
            while (i < events.Count)
            {
                BoardEvent boardEvent = events[i];

                if (boardEvent is SwapEvent swap)
                {
                    yield return AnimateSwap(swap);
                    i++;
                }
                else if (boardEvent is BoardShuffledEvent shuffle)
                {
                    yield return AnimateShuffle(shuffle);
                    i++;
                }
                else if (IsPopEvent(boardEvent))
                {
                    int end = i;
                    while (end < events.Count && IsPopEvent(events[end])) end++;
                    yield return AnimatePop(events, i, end);
                    i = end;
                }
                else if (IsFallEvent(boardEvent))
                {
                    int end = i;
                    while (end < events.Count && IsFallEvent(events[end])) end++;
                    yield return AnimateFall(events, i, end);
                    i = end;
                }
                else
                {
                    i++;
                }
            }
        }

        private static bool IsPopEvent(BoardEvent boardEvent)
        {
            return boardEvent is MatchEvent || boardEvent is TileDestroyedEvent;
        }

        private static bool IsFallEvent(BoardEvent boardEvent)
        {
            return boardEvent is TileMovedEvent || boardEvent is TileSpawnedEvent;
        }

        private IEnumerator AnimateSwap(SwapEvent swap)
        {
            if (!views.TryGetValue(swap.TileA, out TileView viewA) || !views.TryGetValue(swap.TileB, out TileView viewB)) yield break;

            Vector3 posA = CellToLocal(swap.CellA);
            Vector3 posB = CellToLocal(swap.CellB);

            AddMove(viewA.transform, posA, posB, settings.SwapDuration);
            AddMove(viewB.transform, posB, posA, settings.SwapDuration);
            yield return RunMoves(false);

            if (swap.Accepted) yield break;

            AddMove(viewA.transform, posB, posA, settings.SwapDuration);
            AddMove(viewB.transform, posA, posB, settings.SwapDuration);
            yield return RunMoves(false);
        }

        private IEnumerator AnimatePop(List<BoardEvent> events, int start, int end)
        {
            popping.Clear();
            for (int i = start; i < end; i++)
            {
                if (events[i] is TileDestroyedEvent destroyed && views.TryGetValue(destroyed.Tile, out TileView view))
                {
                    popping.Add(view);
                    views.Remove(destroyed.Tile);
                }
            }

            if (popping.Count == 0) yield break;

            Vector3 startScale = Vector3.one * (settings.CellSize * settings.TileScale);
            float elapsed = 0f;
            while (elapsed < settings.PopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settings.PopDuration);
                foreach (TileView view in popping) view.transform.localScale = startScale * (1f - t * t);
                yield return null;
            }

            foreach (TileView view in popping) Destroy(view.gameObject);
            popping.Clear();
        }

        private IEnumerator AnimateFall(List<BoardEvent> events, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (events[i] is TileMovedEvent moved)
                {
                    if (!views.TryGetValue(moved.Tile, out TileView view)) continue;
                    AddFall(view.transform, view.transform.localPosition, CellToLocal(moved.To));
                }
                else if (events[i] is TileSpawnedEvent spawned)
                {
                    Vector3 from = CellToLocal(spawned.SpawnCell);
                    TileView view = TileView.Create(transform, spawned.Tile, from, settings);
                    views[spawned.Tile] = view;
                    AddFall(view.transform, from, CellToLocal(spawned.Cell));
                }
            }

            yield return RunMoves(true);
        }

        private IEnumerator AnimateShuffle(BoardShuffledEvent shuffle)
        {
            foreach (TileMove move in shuffle.Moves)
            {
                if (!views.TryGetValue(move.Tile, out TileView view)) continue;
                AddMove(view.transform, view.transform.localPosition, CellToLocal(move.To), settings.ShuffleDuration);
            }

            yield return RunMoves(false);
        }

        private void AddFall(Transform target, Vector3 from, Vector3 to)
        {
            // Duration from distance, so every tile falls at the same speed.
            float cells = Vector3.Distance(from, to) / settings.CellSize;
            AddMove(target, from, to, cells / settings.FallSpeed);
        }

        private void AddMove(Transform target, Vector3 from, Vector3 to, float duration)
        {
            moveTargets.Add(target);
            moveFrom.Add(from);
            moveTo.Add(to);
            moveDurations.Add(duration);
        }

        /// <summary>Runs every queued move at once and clears the queue. Ease-in suits falling; otherwise smoothstep.</summary>
        private IEnumerator RunMoves(bool easeIn)
        {
            float longest = 0f;
            foreach (float duration in moveDurations) longest = Mathf.Max(longest, duration);

            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                for (int i = 0; i < moveTargets.Count; i++)
                {
                    if (moveTargets[i] == null) continue;

                    float t = moveDurations[i] > 0f ? Mathf.Clamp01(elapsed / moveDurations[i]) : 1f;
                    t = easeIn ? t * t : Mathf.SmoothStep(0f, 1f, t);
                    moveTargets[i].localPosition = Vector3.LerpUnclamped(moveFrom[i], moveTo[i], t);
                }

                if (elapsed >= longest) break;
                yield return null;
            }

            moveTargets.Clear();
            moveFrom.Clear();
            moveTo.Clear();
            moveDurations.Clear();
        }

        private void TrySubscribe()
        {
            if (subscribed) return;

            InputManager input = InputManager.instance;
            if (input == null) return;

            input.PointerPressed += OnPointerPressed;
            input.PointerMoved += OnPointerMoved;
            input.PointerReleased += OnPointerReleased;
            subscribed = true;
        }

        private bool CanAcceptInput()
        {
            return Board != null && HasState && currentStateKey == EBoardState.Idle;
        }

        private void OnPointerPressed(Vector2 screenPosition)
        {
            if (!CanAcceptInput()) return;
            if (!TryScreenToLocal(screenPosition, out Vector2 local)) return;
            if (!TryLocalToCell(local, out Vector2Int cell)) return;

            dragging = true;
            dragCell = cell;
            dragStartLocal = local;
        }

        private void OnPointerMoved(Vector2 screenPosition)
        {
            if (!dragging) return;
            if (!CanAcceptInput())
            {
                dragging = false;
                return;
            }
            if (!TryScreenToLocal(screenPosition, out Vector2 local)) return;

            Vector2 delta = local - dragStartLocal;
            if (delta.magnitude < settings.DragThreshold * settings.CellSize) return;

            // One swap per drag, toward whichever axis the drag leans on most.
            dragging = false;
            Vector2Int direction = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? new Vector2Int(delta.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, delta.y > 0f ? 1 : -1);

            Vector2Int target = dragCell + direction;
            if (!Board.IsInBounds(target)) return;

            Play(Board.TrySwap(dragCell, target));
        }

        private void OnPointerReleased(Vector2 screenPosition)
        {
            dragging = false;
        }

        /// <summary>Projects a screen point onto the board's plane, in this transform's local space.</summary>
        private bool TryScreenToLocal(Vector2 screenPosition, out Vector2 local)
        {
            local = Vector2.zero;
            if (cam == null) cam = Camera.main;
            if (cam == null) return false;

            Ray ray = cam.ScreenPointToRay(screenPosition);
            Plane plane = new Plane(transform.forward, transform.position);
            if (!plane.Raycast(ray, out float distance)) return false;

            local = transform.InverseTransformPoint(ray.GetPoint(distance));
            return true;
        }

        private bool TryLocalToCell(Vector2 local, out Vector2Int cell)
        {
            cell = new Vector2Int(
                Mathf.FloorToInt(local.x / settings.CellSize + Board.Width * 0.5f),
                Mathf.FloorToInt(local.y / settings.CellSize + Board.Height * 0.5f));
            return Board.IsInBounds(cell);
        }

        /// <summary>Centre of a cell in local space. Also used for the virtual spawn cells above the board.</summary>
        private Vector3 CellToLocal(Vector2Int cell)
        {
            return new Vector3(
                (cell.x - (Board.Width - 1) * 0.5f) * settings.CellSize,
                (cell.y - (Board.Height - 1) * 0.5f) * settings.CellSize,
                0f);
        }
    }
}
