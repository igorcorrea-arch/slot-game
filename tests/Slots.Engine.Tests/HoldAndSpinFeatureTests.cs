using Slots.Rng;
using Slots.Contracts;

namespace Slots.Engine.Tests;

public class HoldAndSpinFeatureTests
{
    [Fact]
    public void Execute_WithNoCoins_ShouldReturnZeroWin()
    {
        var rng = new NoCoinRng();

        var prizes = new List<CoinPrize>
        {
            new("cash", 3, 100)
        };

        var feature = new HoldAndSpinFeature(rng, prizes,8, 3, 5000);

        var result = feature.Execute(100);

        Assert.Equal("HoldAndSpin", result.Name);
        Assert.Equal(0, result.AmountCents);
        Assert.Equal(4, result.Steps.Count);

        // 15 posições vazias × 3 respins
        Assert.Equal(45, rng.CallCount);
    }

    private sealed class NoCoinRng : IRng
    {
        public int CallCount { get; private set; }

        public uint NextUInt() => 99;

        public uint Next(uint maxExclusive)
        {
            CallCount++;
            return maxExclusive - 1;
        }
    }
}