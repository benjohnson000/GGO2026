using UnityEngine;

namespace ggo2026
{
    /// <summary>
    /// The underfill's behaviour, with no presentation: it stays where health was while hits land, then waits and
    /// drains to the current health once <see cref="Settle"/> says effects have resolved. Values are in health.
    /// </summary>
    public class HealthBarModel
    {
        private readonly float holdDuration;
        private readonly float drainDuration;

        private float elapsed;
        private float drainFrom;
        private float drainTo;

        public int Health { get; private set; }

        /// <summary>Where the underfill ends. Never below <see cref="Health"/>.</summary>
        public float Underfill { get; private set; }

        /// <summary>True while waiting or draining, when <see cref="Advance"/> needs calling.</summary>
        public bool IsSettling { get; private set; }

        public HealthBarModel(float holdDuration, float drainDuration)
        {
            this.holdDuration = holdDuration;
            this.drainDuration = drainDuration;
        }

        /// <summary>Jumps to <paramref name="health"/> with no underfill showing.</summary>
        public void Reset(int health)
        {
            Health = health;
            Underfill = health;
            IsSettling = false;
        }

        public void SetHealth(int health)
        {
            // A new hit stops the drain where it is until effects resolve again.
            if (health < Health) IsSettling = false;

            Health = health;
            Underfill = Mathf.Max(Underfill, health);
        }

        /// <summary>Effects have resolved. Does nothing if there is nothing to drain or it is already draining.</summary>
        public void Settle()
        {
            if (IsSettling || Underfill <= Health) return;

            IsSettling = true;
            elapsed = 0f;
            drainFrom = Underfill;
            drainTo = Health;
        }

        public void Advance(float deltaTime)
        {
            if (!IsSettling) return;

            elapsed += deltaTime;
            float draining = elapsed - holdDuration;
            if (draining < 0f) return;

            float progress = drainDuration > 0f ? Mathf.Clamp01(draining / drainDuration) : 1f;
            // Healing mid-drain can meet the underfill before it gets to where it was heading.
            Underfill = Mathf.Max(Health, Mathf.SmoothStep(drainFrom, drainTo, progress));
            if (progress < 1f && Underfill > Health) return;

            Underfill = Health;
            IsSettling = false;
        }
    }
}
