using Slots.Rng;

namespace Slots.Engine;

public class PrizeSelector
{
    private readonly IRng _rng;
    private readonly IReadOnlyList<CoinPrize> _prizes;
    private readonly int _totalWeight;

    public PrizeSelector(
        IRng rng,
        IReadOnlyList<CoinPrize> prizes)
    {
        _rng = rng;
        _prizes = prizes;

        _totalWeight = prizes.Sum(p => p.Weight);

        if (_totalWeight <= 0)
            throw new ArgumentException(
                "Total prize weight must be positive.");
    }

    public CoinPrize Draw()
    {
        uint roll = _rng.Next((uint)_totalWeight);

        int cumulativeWeight = 0;

        foreach (var prize in _prizes)
        {
            cumulativeWeight += prize.Weight;

            if (roll < cumulativeWeight)
                return prize;
        }

        throw new InvalidOperationException(
            "Prize selection failed.");
    }
}