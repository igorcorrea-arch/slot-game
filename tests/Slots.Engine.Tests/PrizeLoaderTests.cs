namespace Slots.Engine.Tests;

public class PrizeLoaderTests
{
    [Fact]
    public void Load_ShouldReadAllPrizes()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "data",
            "prizes.csv");

        var prizes = PrizeLoader.Load(path);

        Assert.Equal(10, prizes.Count);
        Assert.Equal(100, prizes.Sum(p => p.Weight));

        Assert.Contains(prizes, p =>
            p.Kind == "cash" &&
            p.ValueTenths == 15 &&
            p.Weight == 12);

        Assert.Contains(prizes, p =>
            p.Kind == "mult" &&
            p.ValueTenths == 3 &&
            p.Weight == 2);
    }
}