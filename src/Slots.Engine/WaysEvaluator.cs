using Slots.Contracts;

namespace Slots.Engine;

public class WaysEvaluator {
    public static List<WinResult> Evaluate(
        IReadOnlyList<IReadOnlyList<string>> window,
        Dictionary<string, Dictionary<int, int>> paytable,
        long betCents
        ) {
        var wins = new List<WinResult>();

        long unit = betCents / 10;

        foreach (var entry in paytable) {
            string symbol = entry.Key;

            // Scatter possui uma regra própria.
            if (symbol == "SC")
                continue;

            int consecutiveReels = 0;
            long ways = 1;

            foreach (var reel in window) {
                int matches = reel.Count(s => s == symbol || s == "WL");

                if (matches == 0) {
                   break;
                }

                consecutiveReels++;
                ways *= matches;
            }

            if (consecutiveReels < 3) {
                continue;
            }

            if (!paytable[symbol].TryGetValue(
                    consecutiveReels, out int payment))
            {
                throw new InvalidOperationException(
                    $"Missing paytable entry: symbol={symbol}, " +
                    $"reels={consecutiveReels}. " +
                    $"Available counts: " +
                    $"{string.Join(", ", paytable[symbol].Keys)}");
            }

            var amount = checked(unit * payment * ways);

            wins.Add(new WinResult(
                Name: symbol,
                Detail: $"{consecutiveReels} reels, {ways} ways",
                AmountCents: amount
            ));
        }

        return wins;
    }
}