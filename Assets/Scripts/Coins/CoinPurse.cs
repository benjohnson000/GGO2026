using System;

namespace ggo2026
{
    /// <summary>
    /// The player's coins, with no presentation. Gains and spends apply instantly; <see cref="CoinsView"/> shows them
    /// in step with playback (coins from tiles count up as they land on the icon).
    /// </summary>
    public class CoinPurse
    {
        public int Amount { get; private set; }

        /// <summary>Raised after every change, with the old and new amounts.</summary>
        public event Action<int, int> Changed;

        public CoinPurse(int startingAmount = 0)
        {
            Amount = Math.Max(0, startingAmount);
        }

        /// <summary>Adds <paramref name="amount"/> coins. Does nothing for 0 or less.</summary>
        public void Gain(int amount)
        {
            if (amount <= 0) return;
            Set(Amount + amount);
        }

        public bool CanAfford(int cost)
        {
            return cost <= Amount;
        }

        /// <summary>Removes <paramref name="cost"/> coins if there are enough. Returns false (and changes nothing) otherwise.</summary>
        public bool TrySpend(int cost)
        {
            if (cost < 0 || !CanAfford(cost)) return false;
            if (cost > 0) Set(Amount - cost);
            return true;
        }

        private void Set(int amount)
        {
            int old = Amount;
            Amount = amount;
            Changed?.Invoke(old, amount);
        }
    }
}
