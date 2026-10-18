using Slots.Contracts;

namespace Slots.Engine;

public static class ScatterEvaluator {
    public static WinResult? Evaluate(
        IReadOnlyList<IReadOnlyList<string>> window,
        Dictionary<string, Dictionary<int, int>> payTable,
        long betCents) {
        
        int scatterCount = window.Sum(reel => reel.Count(symbol => symbol == "SC"));

        if (scatterCount < 3)
            return null;

        int payment = payTable["SC"][scatterCount];

        long unit = betCents / 10;
        long amount = checked(unit * payment);

        return new WinResult(
            Name: "SC",
            Detail: $"{scatterCount} scatters",
            AmountCents: amount
        );
    }
}