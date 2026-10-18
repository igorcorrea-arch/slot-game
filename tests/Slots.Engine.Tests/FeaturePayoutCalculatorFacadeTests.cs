namespace Slots.Engine.Tests;

public class FeaturePayoutCalculatorFacadeTests
{
    [Fact]
    public void Calculate_ShouldSumCashAndApplyMultipliers()
    {
        var board = new CoinPrize?[15];

        board[0] = new CoinPrize("cash", 3, 21);
        board[1] = new CoinPrize("cash", 15, 12);
        board[2] = new CoinPrize("mult", 2, 6);
        board[3] = new CoinPrize("mult", 3, 2);

        long result = FeaturePayoutCalculatorFacade.Calculate(board, 100, 5000);

        Assert.Equal(1080, result);
    }

    [Fact]
    public void Calculate_ShouldAwardGrandForFullBoard()
    {
        var board = new CoinPrize?[15];

        for (int i = 0; i < board.Length; i++)
            board[i] = new CoinPrize("cash", 3, 21);

        long result = FeaturePayoutCalculatorFacade.Calculate(board, 100, 5000);

        Assert.Equal(50450, result);
    }

    [Fact]
    public void Calculate_EmptyBoard_ShouldPayZero()
    {
        var board = new CoinPrize?[15];

        long result = FeaturePayoutCalculatorFacade.Calculate(board, 100, 5000);

        Assert.Equal(0, result);
    }
}