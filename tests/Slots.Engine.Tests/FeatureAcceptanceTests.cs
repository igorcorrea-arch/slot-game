using Slots.Contracts;

namespace Slots.Engine.Tests;

public class FeatureAcceptanceTests
{
    [Fact]
    public void F01_NormalCycle_ShouldPay360Cents()
    {
        // Arrange
        var state = new HoldAndSpinState(3);
        var board = new CoinPrize?[15];

        board[0] = new CoinPrize("cash", 3, 21);
        board[1] = new CoinPrize("cash", 15, 12);

        state.ApplyRespin(new[] { 0, 1 });

        Assert.Equal(3, state.Lives);
        Assert.Equal(2, state.LockedCount);

        state.ApplyRespin(Array.Empty<int>());

        Assert.Equal(2, state.Lives);

        board[2] = new CoinPrize("mult", 2, 6);

        state.ApplyRespin(new[] { 2 });

        Assert.Equal(3, state.Lives);
        Assert.Equal(3, state.LockedCount);

        state.ApplyRespin(Array.Empty<int>());
        state.ApplyRespin(Array.Empty<int>());
        state.ApplyRespin(Array.Empty<int>());

        // Assert
        Assert.Equal(0, state.Lives);
        Assert.Equal(6, state.RespinCount);
        Assert.True(state.IsFinished);

        long payout = FeaturePayoutCalculatorFacade.Calculate(board, 100, 5000);

        Assert.Equal(360, payout);
    }
    
    [Fact]
    public void F02_ThreeEmptyRespins_ShouldPayZero()
    {
        // Arrange
        var state = new HoldAndSpinState(3);
        var board = new CoinPrize?[15];

        // Act
        state.ApplyRespin(Array.Empty<int>());
        state.ApplyRespin(Array.Empty<int>());
        state.ApplyRespin(Array.Empty<int>());

        long payout = FeaturePayoutCalculatorFacade.Calculate(board, 100, 5000);

        // Assert
        Assert.True(state.IsFinished);
        Assert.Equal(0, state.Lives);
        Assert.Equal(3, state.RespinCount);
        Assert.Equal(0, state.LockedCount);
        Assert.Equal(0, payout);
    }
    
    [Fact]
    public void F02_ScriptedFeature_ShouldPayZero()
    {
        var script = new List<ScriptedRespin>
        {
            new(Array.Empty<ScriptedCoin>()),
            new(Array.Empty<ScriptedCoin>()),
            new(Array.Empty<ScriptedCoin>())
        };

        FeatureResult result = HoldAndSpinFeature.ExecuteScripted(script, 100, 3, 5000);

        Assert.Equal("HoldAndSpin", result.Name);
        Assert.Equal(4, result.Steps.Count);
        Assert.Equal(0, result.AmountCents);
    }
    
    [Fact]
    public void F03_FullGrid_ShouldAwardGrand()
    {
        var script = new List<ScriptedRespin>();

        script.Add(new ScriptedRespin(
            Enumerable.Range(0, 5)
                .Select(i => new ScriptedCoin(
                    i, new CoinPrize("cash", 6, 1)))
                .ToList()
        ));

        script.Add(new ScriptedRespin(
            Enumerable.Range(5, 5)
                .Select(i => new ScriptedCoin(
                    i, new CoinPrize("cash", 9, 1)))
                .ToList()
        ));

        var lastCoins = Enumerable.Range(10, 4)
            .Select(i => new ScriptedCoin(
                i, new CoinPrize("cash", 15, 1)))
            .ToList();

        lastCoins.Add(new ScriptedCoin(
            14, new CoinPrize("mult", 2, 1)));

        script.Add(new ScriptedRespin(lastCoins));

        var result = HoldAndSpinFeature.ExecuteScripted(script, 100, 3, 5000);

        Assert.Equal(52700, result.AmountCents);
        Assert.Equal(4, result.Steps.Count);
    }
    
    
    [Fact]
    public void F04_MinimumBet_ShouldScaleFeature()
    {
        var script = new List<ScriptedRespin>();

        script.Add(new ScriptedRespin(
            Enumerable.Range(0, 5)
                .Select(i => new ScriptedCoin(
                    i, new CoinPrize("cash", 300, 1)))
                .ToList()
        ));

        script.Add(new ScriptedRespin(
            Enumerable.Range(5, 5)
                .Select(i => new ScriptedCoin(
                    i, new CoinPrize("cash", 300, 1)))
                .ToList()
        ));

        var lastCoins = Enumerable.Range(10, 3)
            .Select(i => new ScriptedCoin(
                i, new CoinPrize("cash", 300, 1)))
            .ToList();

        lastCoins.Add(new ScriptedCoin(
            13, new CoinPrize("mult", 3, 1)));

        lastCoins.Add(new ScriptedCoin(
            14, new CoinPrize("mult", 3, 1)));

        script.Add(new ScriptedRespin(lastCoins));

        var result = HoldAndSpinFeature.ExecuteScripted(script, 10, 3, 5000);

        Assert.Equal(40100, result.AmountCents);
        Assert.Equal(4, result.Steps.Count);
        
    }

}