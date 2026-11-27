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
        /// <summary>
        /// A visual yet to launch in the current pop run: a line clear's blast, a bomb's explosion circle, or one
        /// projectile from a laser or colour bomb to <see cref="Target"/>.
        /// </summary>
        private struct PendingBlast
        {
            public TileEffectActivatedEvent Effect;
            public float StartTime;
            public bool HasTarget;
            public Vector2Int Target;
        }

        [SerializeField] private BoardConfigSO config;

        [Tooltip("Tiles in the pool at the start. Optional: with no deck every tile is a random-colour filler.")]
        [SerializeField] private StartingDeckSO startingDeck;

        [SerializeField] private BoardViewSettingsSO settings;

        [Tooltip("Optional. The fight tile effects such as damage and block act on; without it they do nothing.")]
        [SerializeField] private EncounterView encounter;

        private readonly Dictionary<Tile, TileView> views = new Dictionary<Tile, TileView>();
        private List<BoardEvent> pendingEvents;

        // The enemy's countdown and turn after a swap, shown once the swap's board events have played.
        private List<CombatEvent> pendingTurnEvents;

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
        private readonly List<float> popStartTimes = new List<float>();
        private readonly List<PendingBlast> pendingBlasts = new List<PendingBlast>();

        // Pops played so far in the current playback (one swap and all its cascades), for the rising pop pitch.
        private int popChainCount;

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
            if (settings.Palette == null) Debug.LogError($"{name}: {settings.name} has no palette assigned, so tiles will be white.", settings);
            if (settings.TileMaterial == null || !settings.TileMaterial.HasProperty("_OutlineThickness"))
            {
                Debug.LogError($"{name}: {settings.name} needs a Tile Material using the GGO2026/Tile Outline shader, or tile outlines are not drawn.", settings);
            }

            // InputManager lives in another scene and may wake after this object's OnEnable.
            TrySubscribe();
            if (!subscribed) Debug.LogError($"{name}: no InputManager found, so the board can't receive input.", this);

            System.Random rng = new System.Random();
            TilePool pool = new TilePool(config.FillerTiles, rng);
            if (!pool.HasFillerTiles) Debug.LogWarning($"{name}: {config.name} has no valid filler tiles, so fillers use plain tiles of a random colour.", config);
            if (startingDeck != null) startingDeck.AddTo(pool);

            Board = new Board(config.Width, config.Height, pool, rng, new ComboTable(config.ComboRules));
            // EncounterView builds its Combat in Awake, so it is ready here.
            if (encounter != null) Board.Combat = encounter.Combat;
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
        /// Animates events in order. Consecutive destroy events pop one after another in rapid succession, and consecutive fall and spawn
        /// events drop together, so each cascade step reads as one beat.
        /// </summary>
        internal IEnumerator PlayEvents(List<BoardEvent> events)
        {
            // Each playback is one swap with all its cascades, so the pop pitch keeps rising until it ends.
            popChainCount = 0;

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
                else if (boardEvent is ComboCreatedEvent)
                {
                    int end = i;
                    while (end < events.Count && events[end] is ComboCreatedEvent) end++;
                    yield return AnimateComboSpawn(events, i, end);
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

            // Let every hit land before the enemy acts and before input returns.
            while (encounter != null && encounter.IsPlaying) yield return null;

            if (pendingTurnEvents != null)
            {
                List<CombatEvent> turnEvents = pendingTurnEvents;
                pendingTurnEvents = null;
                if (encounter != null) encounter.Play(turnEvents);
            }

            // Only here is the whole chain over: the encounter's queue also empties between cascades.
            if (encounter != null) encounter.Settle();
        }

        private static bool IsPopEvent(BoardEvent boardEvent)
        {
            // Tiles hit by effects pop together with the match that set them off.
            return boardEvent is MatchEvent || boardEvent is TileDestroyedEvent || boardEvent is TileEffectActivatedEvent;
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

        /// <summary>
        /// Pops a run's destroyed tiles one at a time, <see cref="BoardViewSettingsSO.PopInterval"/> apart, in the order
        /// the board resolved them. Each tile shrinks over <see cref="BoardViewSettingsSO.PopDuration"/> from its own
        /// start, so pops overlap. Tiles hit by a line clear or laser pop as its blast reaches them instead.
        /// A tile's combat results play as soon as it has disappeared.
        /// </summary>
        private IEnumerator AnimatePop(List<BoardEvent> events, int start, int end)
        {
            // Times are worked out up front, so the rhythm doesn't drift with frame rate.
            float blastsEnd = SchedulePops(events, start, end);

            Vector3 startScale = Vector3.one * (settings.CellSize * settings.TileScale);
            float elapsed = 0f;
            int started = 0;
            // Effects have their own position in the run: the board logs them after all the destroys that queued them.
            int nextEffect = start;

            while (true)
            {
                // Start every pop that is due; several can start in one frame.
                while (started < popping.Count && elapsed >= popStartTimes[started])
                {
                    PlayPopSound(popChainCount++);
                    started++;
                }

                LaunchDueBlasts(elapsed);

                bool running = started < popping.Count;
                for (int i = 0; i < started; i++)
                {
                    float t = Mathf.Clamp01((elapsed - popStartTimes[i]) / settings.PopDuration);
                    popping[i].transform.localScale = startScale * (1f - t * t);
                    if (t < 1f) running = true;
                    else views.Remove(popping[i].Tile); // A tile leaves views only once it has fully disappeared.
                }

                nextEffect = PlayEffectsOfVanishedTiles(events, nextEffect, end);

                if (!running && elapsed >= blastsEnd) break;
                yield return null;
                elapsed += Time.deltaTime;
            }

            foreach (TileView view in popping) Destroy(view.gameObject);
            popping.Clear();
            popStartTimes.Clear();
        }

        /// <summary>
        /// Fills the pop and blast times for a run and returns when the last blast reaches the board's edge.
        /// Start times never decrease along the event list, so tiles vanish in the order their effects fired. That is
        /// why a blast that hits tiles waits for every pop before it to have started.
        /// </summary>
        private float SchedulePops(List<BoardEvent> events, int start, int end)
        {
            popping.Clear();
            popStartTimes.Clear();
            pendingBlasts.Clear();

            float nextBeat = 0f;
            float lastPopStart = 0f;
            float blastsEnd = 0f;
            float blastStart = 0f;
            TileEffectActivatedEvent blast = null;
            ProjectileStyleSO blastStyle = null;
            int blastHits = 0;
            ExplosionActivatedEvent explosion = null;
            float explosionStart = 0f;

            for (int i = start; i < end; i++)
            {
                if (events[i] is TileEffectActivatedEvent activated)
                {
                    // A bomb's circle bursts out on the next pop beat, and its tiles pop ring by ring as it reaches them.
                    explosion = activated as ExplosionActivatedEvent;
                    if (explosion != null)
                    {
                        explosionStart = nextBeat;
                        pendingBlasts.Add(new PendingBlast { Effect = activated, StartTime = explosionStart });
                        blastsEnd = Mathf.Max(blastsEnd, explosionStart);
                    }

                    // The board logs an effect's destroys straight after it, up to the next effect.
                    blast = IsBlast(activated) ? activated : null;
                    if (blast == null) continue;

                    blastStart = GetVanishTime(blast.Tile);
                    // A blast with nothing left to hit can't reorder anything, so it needn't wait.
                    bool hitsTiles = i + 1 < end && events[i + 1] is TileDestroyedEvent;
                    if (hitsTiles) blastStart = Mathf.Max(blastStart, lastPopStart);

                    blastStyle = GetProjectileStyle(blast);
                    blastHits = 0;

                    if (blast is LineClearActivatedEvent line)
                    {
                        pendingBlasts.Add(new PendingBlast { Effect = blast, StartTime = blastStart });
                        GetArmLengths(line, out float back, out float forward);
                        blastsEnd = Mathf.Max(blastsEnd, blastStart + Mathf.Max(back, forward) / settings.LineBlastSpeed);
                    }
                }
                else if (events[i] is TileDestroyedEvent destroyed && views.TryGetValue(destroyed.Tile, out TileView view))
                {
                    float popStart = nextBeat;
                    if (explosion != null)
                    {
                        popStart = explosionStart + GetExplosionReachTime(explosion, destroyed.Cell);
                    }
                    else if (blast is LineClearActivatedEvent)
                    {
                        int distance = Mathf.Abs(destroyed.Cell.x - blast.Cell.x) + Mathf.Abs(destroyed.Cell.y - blast.Cell.y);
                        popStart = blastStart + distance / settings.LineBlastSpeed;
                    }
                    else if (blast != null)
                    {
                        // One projectile per hit, launched PopInterval apart so they keep the pop rhythm, each
                        // target popping as its own projectile lands.
                        float launch = blastStart + blastHits * settings.PopInterval;
                        blastHits++;
                        pendingBlasts.Add(new PendingBlast { Effect = blast, StartTime = launch, HasTarget = true, Target = destroyed.Cell });
                        popStart = launch + (blastStyle != null ? blastStyle.FlightTime : 0f);
                        blastsEnd = Mathf.Max(blastsEnd, popStart);
                    }

                    popping.Add(view);
                    popStartTimes.Add(popStart);
                    lastPopStart = popStart;
                    nextBeat = popStart + settings.PopInterval;
                }
            }

            return blastsEnd;
        }

        /// <summary>When a tile scheduled in this run will have disappeared.</summary>
        private float GetVanishTime(Tile tile)
        {
            for (int i = 0; i < popping.Count; i++)
            {
                if (popping[i].Tile == tile) return popStartTimes[i] + settings.PopDuration;
            }
            return 0f;
        }

        /// <summary>
        /// True for effects whose hits pop as a travelling visual reaches them rather than at <see cref="BoardViewSettingsSO.PopInterval"/>:
        /// line clears, lasers, and colour bombs that have a projectile style with a prefab.
        /// </summary>
        private bool IsBlast(TileEffectActivatedEvent activated)
        {
            if (activated is LineClearActivatedEvent || activated is LaserActivatedEvent) return true;

            ProjectileStyleSO style = GetProjectileStyle(activated);
            return style != null && style.Prefab != null;
        }

        /// <summary>
        /// The style of the projectiles an effect sends to the tiles it hits: its own <see cref="TileEffect.Projectile"/>;
        /// for a laser without one, the board's default. Null for effects that send none (a colour bomb without one, line clears…).
        /// </summary>
        private ProjectileStyleSO GetProjectileStyle(TileEffectActivatedEvent activated)
        {
            if (!(activated is LaserActivatedEvent) && !(activated is ColourBombActivatedEvent)) return null;

            ProjectileStyleSO style = activated.Effect.Projectile;
            if (style == null && activated is LaserActivatedEvent) style = settings.DefaultLaserProjectile;
            return style;
        }

        /// <summary>Cells from a line clear's centre to the board's edge on each side.</summary>
        private void GetArmLengths(LineClearActivatedEvent line, out float back, out float forward)
        {
            bool row = line.Axis == LineAxis.Row;
            int index = row ? line.Cell.x : line.Cell.y;
            int length = row ? Board.Width : Board.Height;
            back = index + 0.5f;
            forward = length - index - 0.5f;
        }

        private void LaunchDueBlasts(float elapsed)
        {
            // Not sorted by time: a blast with nothing to hit can launch before one logged earlier.
            for (int i = pendingBlasts.Count - 1; i >= 0; i--)
            {
                PendingBlast pending = pendingBlasts[i];
                if (elapsed < pending.StartTime) continue;

                if (pending.HasTarget) LaunchProjectile(pending.Effect, pending.Target);
                else if (pending.Effect is LineClearActivatedEvent line) LaunchLineBlast(line, elapsed - pending.StartTime);
                else if (pending.Effect is ExplosionActivatedEvent explosion) LaunchExplosion(explosion, elapsed - pending.StartTime);
                pendingBlasts.RemoveAt(i);
            }
        }

        /// <summary>
        /// Seconds after a bomb's circle spawns that its edge reaches <paramref name="cell"/>'s ring (square distance
        /// from the bomb), so every tile in a ring pops together. The board lists targets ring by ring, so these
        /// times never decrease.
        /// </summary>
        private float GetExplosionReachTime(ExplosionActivatedEvent explosion, Vector2Int cell)
        {
            Vector2Int offset = cell - explosion.Cell;
            int ring = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
            // The circle's full radius is Radius + 0.5 cells, matching the diameter in LaunchExplosion.
            float fraction = ring / (explosion.Radius + 0.5f);
            return ExplosionBlast.GetTimeToReach(fraction) * settings.ExplosionDuration;
        }

        /// <summary>
        /// Spawns a bomb's circle, <paramref name="age"/> seconds in, sized to cover its blast square.
        /// No visual without <see cref="BoardViewSettingsSO.ExplosionSprite"/>.
        /// </summary>
        private void LaunchExplosion(ExplosionActivatedEvent explosion, float age)
        {
            PlaySound(settings.ExplosionSound);

            Sprite circle = settings.ExplosionSprite;
            if (circle == null) return;

            float diameter = (explosion.Radius * 2 + 1) * settings.CellSize;
            ExplosionBlast.Create(circle, transform, CellToLocal(explosion.Cell), diameter, settings.ExplosionDuration,
                GetEffectColour(explosion.Tile), age);
        }

        /// <summary>
        /// Sends one projectile in the effect's style from its tile to <paramref name="target"/>. Unlike a line blast it
        /// can't start partway through its flight, so it may land up to a frame after its target starts to pop.
        /// </summary>
        private void LaunchProjectile(TileEffectActivatedEvent effect, Vector2Int target)
        {
            ProjectileStyleSO style = GetProjectileStyle(effect);
            if (style == null || style.Prefab == null) return;

            Vector3 origin = transform.TransformPoint(CellToLocal(effect.Cell));
            // Parented to the board so it unloads with it.
            DamageProjectile projectile = style.Spawn(origin, transform);
            projectile.Launch(style, origin, transform.TransformPoint(CellToLocal(target)), GetEffectColour(effect.Tile));
        }

        /// <summary>Plays a line clear's sound and spawns its two arms, <paramref name="age"/> seconds into their flight.</summary>
        private void LaunchLineBlast(LineClearActivatedEvent line, float age)
        {
            PlaySound(settings.LineBlastSound);

            LineBlast prefab = settings.LineBlastPrefab;
            if (prefab == null) return;

            GetArmLengths(line, out float back, out float forward);
            Vector2 direction = line.Axis == LineAxis.Row ? Vector2.right : Vector2.up;
            Vector3 origin = CellToLocal(line.Cell);
            float speed = settings.LineBlastSpeed * settings.CellSize;
            float fade = settings.LineBlastFadeTime;
            Color colour = GetEffectColour(line.Tile);

            LineBlast.Create(prefab, transform, origin, -direction, back * settings.CellSize, speed, fade, colour, age);
            LineBlast.Create(prefab, transform, origin, direction, forward * settings.CellSize, speed, fade, colour, age);
        }

        /// <summary>
        /// Sends combat results to the encounter, from <paramref name="next"/> up to the first effect whose tile is still
        /// visible, and returns where it stopped. Tiles vanish in the order their effects fired, so this never holds a
        /// vanished tile back, and combat events stay in the order they resolved.
        /// </summary>
        private int PlayEffectsOfVanishedTiles(List<BoardEvent> events, int next, int end)
        {
            for (; next < end; next++)
            {
                if (!(events[next] is TileEffectActivatedEvent activated) || activated.CombatEvents.Count == 0) continue;
                if (views.ContainsKey(activated.Tile)) break;

                // From the tile's cell, so hits can fly from the tile to their target.
                if (encounter != null)
                {
                    Vector3 origin = transform.TransformPoint(CellToLocal(activated.Cell));
                    encounter.Play(activated.CombatEvents, origin, GetEffectColour(activated.Tile), activated.Effect.Projectile);
                }
            }

            return next;
        }

        /// <summary>The colour of a tile's blast and projectiles: the one its shape is drawn in.</summary>
        private Color GetEffectColour(Tile tile)
        {
            // Only tiles with data have effects, so a tile that fired always has some.
            return tile.Data.GetShapeColor(settings.Palette);
        }

        /// <summary>
        /// Plays the pop sound for the <paramref name="index"/>th tile popped since the swap, pitched up a step per tile.
        /// Silent with no sound profile (or no clips in it) or no SFXManager loaded.
        /// </summary>
        private void PlayPopSound(int index)
        {
            float pitch = Mathf.Min(1f + index * settings.PopPitchStep, settings.PopPitchMax);
            PlaySound(settings.PopSound, pitch);
        }

        private static void PlaySound(SoundProfileSO sound, float pitch = 1f)
        {
            if (sound == null || !sound.HasClips) return;

            SFXManager sfx = SFXManager.instance;
            if (sfx != null) sfx.PlaySound(sound, pitch);
        }

        /// <summary>New combo tiles grow in at their cell, the reverse of a pop.</summary>
        private IEnumerator AnimateComboSpawn(List<BoardEvent> events, int start, int end)
        {
            popping.Clear();
            for (int i = start; i < end; i++)
            {
                ComboCreatedEvent created = (ComboCreatedEvent)events[i];
                TileView view = TileView.Create(transform, created.Tile, CellToLocal(created.Cell), settings);
                views[created.Tile] = view;
                view.transform.localScale = Vector3.zero;
                popping.Add(view);
            }

            Vector3 fullScale = Vector3.one * (settings.CellSize * settings.TileScale);
            float elapsed = 0f;
            while (elapsed < settings.PopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settings.PopDuration);
                foreach (TileView view in popping) view.transform.localScale = fullScale * Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }

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
            return Board != null && HasState && currentStateKey == EBoardState.Idle && (encounter == null || !encounter.IsOver);
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

            List<BoardEvent> events = Board.TrySwap(dragCell, target);
            // The swap has fully resolved, so the enemy's countdown ticks now (DESIGN.md › turn order).
            // Passives will fire between the two once they exist.
            if (encounter != null && IsAcceptedSwap(events)) pendingTurnEvents = encounter.AdvanceSwap();
            Play(events);
        }

        private static bool IsAcceptedSwap(List<BoardEvent> events)
        {
            return events.Count > 0 && events[0] is SwapEvent swap && swap.Accepted;
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
