using Slots.Contracts;
using Slots.Rng;

namespace Slots.Engine;

/// <summary>
/// Your engine. Implement the contract here — how you organize the rest of the project
/// (configuration, evaluation, feature) is your decision.
/// </summary>
public sealed class SlotEngine(
    IRng rng,
    List<List<string>> reels,
    Dictionary<string, Dictionary<int,int>> payTable,
    List<CoinPrize> prizes,
    GameConfig config
    ) : ISlotEngine {
    private readonly IRng _rng = rng;

    public SpinResult Spin(long betCents) {
        if (betCents < 10 || betCents % 10 != 0)
            throw new ArgumentException("Invalid bet.");

        var stops = new int[reels.Count];

        for (int i = 0; i < reels.Count; i++)
        {
            stops[i] = (int)_rng.Next(
                (uint)reels[i].Count);
        }

        // Calcula os pagamentos do Base Game.
        var baseResult = SpinWithStops(stops, betCents);

        // Conta os Scatters na janela sorteada.
        int scatterCount = baseResult.Window.Sum(
            reel => reel.Count(symbol => symbol == "SC"));

        // Sem bônus: devolvemos o resultado normal.
        if (scatterCount < config.ScatterTriggerCount)
            return baseResult;

        // Com bônus: executamos o Hold & Spin.
        var feature = new HoldAndSpinFeature(_rng,
            prizes,
            config.HoldAndSpin.CoinChancePercent,
            config.HoldAndSpin.InitialLives,
            config.HoldAndSpin.GrandPrizeTenths);
        FeatureResult featureResult = feature.Execute(betCents);

        long totalWin = checked(
            baseResult.TotalWinCents + featureResult.AmountCents);

        return new SpinResult(
            Stops: baseResult.Stops,
            Window: baseResult.Window,
            Wins: baseResult.Wins,
            Feature: featureResult,
            TotalWinCents: totalWin
        );
    }

    public SpinResult SpinWithStops(IReadOnlyList<int> stops, long betCents) {
        if (betCents < 10 || betCents % 10 != 0) {
            throw new ArgumentException("invalid bet amount");
        }

        if (stops is null || stops.Count != reels.Count) {
            throw new ArgumentException("invalid stops");
        }

        for (int i = 0; i < stops.Count; i++) {
            if (stops[i] < 0 || stops[i] >= reels[i].Count)
                throw new ArgumentException("stops out of bounds");
        }
        
        var window = BuildWindow(stops);
        
        var wins = WaysEvaluator.Evaluate(window, payTable, betCents);
        var scatterBonus = ScatterEvaluator.Evaluate(window, payTable, betCents);

        if (scatterBonus is not null) {
            wins.Add(scatterBonus);    
        }
        
        long totalWin = 0;
        
        foreach (var win in wins) {
            totalWin += win.AmountCents;
        }

        return new SpinResult(
            Stops: stops,
            Window: window,
            Wins: wins,
            Feature: null,
            TotalWinCents: totalWin
        );
    }
    
    private List<IReadOnlyList<string>> BuildWindow(IReadOnlyList<int> stops) {
        var window = new List<IReadOnlyList<string>>();

        for (int reel = 0; reel < reels.Count; reel++)
        {
            var visibleSymbols = new List<string>();

            for (int row = 0; row < 3; row++)
            {
                int position =
                    (stops[reel] + row) % reels[reel].Count;

                visibleSymbols.Add(reels[reel][position]);
            }

            window.Add(visibleSymbols);
        }
        return window;
    }
}
