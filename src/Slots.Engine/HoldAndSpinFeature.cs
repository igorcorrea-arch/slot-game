using Slots.Rng;
using Slots.Contracts;

namespace Slots.Engine;

public class HoldAndSpinFeature {
    private readonly IRng _rng;
    private readonly PrizeSelector _prizeSelector;
    private readonly int _coinChancePercent;
    private readonly int _initialLives;
    private readonly int _grandPrizeTenths;

    public HoldAndSpinFeature(
        IRng rng,
        IReadOnlyList<CoinPrize> prizes,
        int coinChancePercent,
        int initialLives,
        int grandPrizeTenths)
    {
        if (coinChancePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(coinChancePercent));

        if (initialLives <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialLives));
        
        if (grandPrizeTenths < 0)
            throw new ArgumentOutOfRangeException(nameof(grandPrizeTenths));

        _rng = rng;
        _prizeSelector = new PrizeSelector(rng, prizes);
        _coinChancePercent = coinChancePercent;
        _initialLives = initialLives;
        _grandPrizeTenths = grandPrizeTenths;
    }

    public FeatureResult Execute(long betCents)
    {
        var state = new HoldAndSpinState(_initialLives);

        var board = new CoinPrize?[15];
        
        var steps = new List<FeatureStep>();

        while (!state.IsFinished)
        {
            var newCoins = new List<ScriptedCoin>();

            for (int position = 0; position < board.Length; position++)
            {
                if (state.LockedCells[position])
                    continue;

                uint roll = _rng.Next(100);

                if (roll < _coinChancePercent)
                {
                    newCoins.Add(new ScriptedCoin(position, _prizeSelector.Draw()));
                }
            }
            ApplyRespin(state, board, steps, newCoins);
        }
        long amount = FeaturePayoutCalculatorFacade.Calculate(board, betCents, _grandPrizeTenths);
        
        steps.Add(new FeatureStep(
            Description: $"Final board - Win: {amount} cents",
            Window: BuildFinalWindow(board)
        ));

        return new FeatureResult(
            Name: "HoldAndSpin",
            Steps: steps,
            AmountCents: amount
        );
    }
    
    
    public static FeatureResult ExecuteScripted(IReadOnlyList<ScriptedRespin> respins,
        long betCents,
        int initialLives,
        int grandPrizeTenths
        ) {
        var state = new HoldAndSpinState(initialLives);
        var board = new CoinPrize?[15];
        var steps = new List<FeatureStep>();

        foreach (var respin in respins)
        {
            if (state.IsFinished)
                throw new InvalidOperationException(
                    "Script continues after feature ended.");

            ApplyRespin(state, board, steps, respin.Coins);
        }

        if (!state.IsFinished)
            throw new InvalidOperationException(
                "Script ended before feature finished.");

        long amount = FeaturePayoutCalculatorFacade.Calculate(board, betCents, grandPrizeTenths);
        
        steps.Add(new FeatureStep(
            Description: $"Final board - Win: {amount} cents",
            Window: BuildFinalWindow(board)
        ));

        return new FeatureResult(
            Name: "HoldAndSpin",
            Steps: steps,
            AmountCents: amount
        );
    }

    
    
    private static void ApplyRespin(
        HoldAndSpinState state,
        CoinPrize?[] board,
        List<FeatureStep> steps,
        IReadOnlyList<ScriptedCoin> coins)
    {
        var positions = coins
            .Select(coin => coin.Position)
            .ToList();

        state.ApplyRespin(positions);

        foreach (var coin in coins)
        {
            board[coin.Position] = coin.Prize;
        }

        steps.Add(new FeatureStep(
            Description:
            $"Respin {state.RespinCount}: " +
            $"{positions.Count} new coins, " +
            $"{state.Lives} lives remaining",
            Window: null
        ));
    }
    
    
    private static IReadOnlyList<IReadOnlyList<string>> BuildFinalWindow(CoinPrize?[] board) {
        var window = new List<IReadOnlyList<string>>();

        for (int reel = 0; reel < 5; reel++) {
            var column = new List<string>();

            for (int row = 0; row < 3; row++) {
                int position = reel * 3 + row;
                CoinPrize? coin = board[position];

                string symbol = coin is null
                    ? "EMPTY"
                    : coin.Kind == "mult"
                        ? $"MULT_{coin.ValueTenths}"
                        : $"CASH_{coin.ValueTenths}";

                column.Add(symbol);
            }
            window.Add(column);
        }
        return window;
    }
    
}