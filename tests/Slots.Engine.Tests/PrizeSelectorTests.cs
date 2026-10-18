using Slots.Rng;

namespace Slots.Engine.Tests;

public class PrizeSelectorTests
{
    [Theory]
    [InlineData(0, "cash", 3)]
    [InlineData(20, "cash", 3)]
    [InlineData(21, "cash", 6)]
    [InlineData(55, "cash", 15)]
    [InlineData(91, "cash", 300)]
    [InlineData(92, "mult", 2)]
    [InlineData(98, "mult", 3)]
    [InlineData(99, "mult", 3)]
    public void Draw_ShouldRespectWeights(
        uint roll,
        string expectedKind,
        int expectedValue)
    {
        var prizes = new List<CoinPrize>
        {
            new("cash", 3, 21),
            new("cash", 6, 18),
            new("cash", 9, 14),
            new("cash", 15, 12),
            new("cash", 30, 11),
            new("cash", 60, 10),
            new("cash", 150, 5),
            new("cash", 300, 1),
            new("mult", 2, 6),
            new("mult", 3, 2)
        };

        var rng = new FixedRng(roll);
        var selector = new PrizeSelector(rng, prizes);

        CoinPrize result = selector.Draw();

        Assert.Equal(expectedKind, result.Kind);
        Assert.Equal(expectedValue, result.ValueTenths);
    }

    private sealed class FixedRng(uint value) : IRng
    {
        public uint NextUInt() => value;

        public uint Next(uint maxExclusive)
        {
            if (value >= maxExclusive)
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive));

            return value;
        }
    }
}