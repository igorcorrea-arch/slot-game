using Slots.Rng;

namespace Slots.Engine.Tests;

public class SlotEngineIntegrationTests
{
    [Fact]
    public void SpinWithStops_ShouldNotConsumeRng()
    {
        // Arrange
        var rng = new ThrowingRng();

        var reels = new List<List<string>>();

        for (int i = 0; i < 5; i++)
        {
            reels.Add(new List<string>
            {
                "SC", "H1", "H2"
            });
        }

        var paytable = new Dictionary<string, Dictionary<int, int>>
        {
            ["H1"] = new() { [3] = 10, [4] = 28, [5] = 96 },
            ["H2"] = new() { [3] = 5, [4] = 13, [5] = 45 },
            ["SC"] = new() { [3] = 30, [4] = 150, [5] = 1000 }
        };

        var prizes = new List<CoinPrize>
        {
            new("cash", 3, 100)
        };

        var engine = new SlotEngine(
            rng,
            reels,
            paytable,
            prizes,
            new GameConfig());

        // Act
        var result = engine.SpinWithStops(
            new[] { 0, 0, 0, 0, 0 },
            100);

        // Assert
        Assert.Null(result.Feature);
        Assert.Equal(11410, result.TotalWinCents);
    }
    
    [Fact]
    public void Spin_WithScatters_ShouldExecuteFeature()
    {
        var reels = new List<List<string>>();

        for (int i = 0; i < 5; i++)
        {
            reels.Add(new List<string>
            {
                "SC", "H1", "H2"
            });
        }

        var paytable = new Dictionary<string, Dictionary<int, int>>
        {
            ["H1"] = new() { [3] = 10, [4] = 28, [5] = 96 },
            ["H2"] = new() { [3] = 5, [4] = 13, [5] = 45 },
            ["SC"] = new() { [3] = 30, [4] = 150, [5] = 1000 }
        };

        var prizes = new List<CoinPrize>
        {
            new("cash", 3, 100)
        };

        // 5 sorteios para os stops, todos na posição 0.
        // Depois, 45 sorteios com resultado 99:
        // nenhuma moeda cai durante os 3 respins.
        var values = new List<uint>();

        values.AddRange(Enumerable.Repeat(0u, 5));
        values.AddRange(Enumerable.Repeat(99u, 45));

        var rng = new SequenceRng(values.ToArray());

        var engine = new SlotEngine(
            rng,
            reels,
            paytable,
            prizes,
            new GameConfig());

        var result = engine.Spin(100);

        Assert.NotNull(result.Feature);
        Assert.Equal("HoldAndSpin", result.Feature.Name);

        Assert.Equal(4, result.Feature.Steps.Count);
        Assert.Equal(0, result.Feature.AmountCents);

        Assert.Equal(11410, result.TotalWinCents);
        Assert.Equal(50, rng.CallCount);
    }

    private sealed class ThrowingRng : IRng
    {
        public uint NextUInt()
        {
            throw new Exception("RNG should not be called!");
        }

        public uint Next(uint maxExclusive)
        {
            throw new Exception("RNG should not be called!");
        }
    }
    
    private sealed class SequenceRng : IRng
    {
        private readonly Queue<uint> _values;

        public int CallCount { get; private set; }

        public SequenceRng(params uint[] values)
        {
            _values = new Queue<uint>(values);
        }

        public uint NextUInt()
        {
            throw new NotSupportedException();
        }

        public uint Next(uint maxExclusive)
        {
            CallCount++;

            if (_values.Count == 0)
                throw new InvalidOperationException(
                    "RNG sequence exhausted.");

            uint value = _values.Dequeue();

            if (value >= maxExclusive)
                throw new InvalidOperationException(
                    $"RNG value {value} exceeds limit {maxExclusive}.");

            return value;
        }
    }
}