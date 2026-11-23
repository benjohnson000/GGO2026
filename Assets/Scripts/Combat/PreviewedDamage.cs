namespace ggo2026
{
    /// <summary>The health one status is set to take, as part of a <see cref="DamagePreview"/>.</summary>
    public readonly struct PreviewedDamage
    {
        public StatusDataSO Status { get; }
        public int HealthLost { get; }

        public PreviewedDamage(StatusDataSO status, int healthLost)
        {
            Status = status;
            HealthLost = healthLost;
        }
    }
}
