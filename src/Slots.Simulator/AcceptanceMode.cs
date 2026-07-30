using Slots.Contracts;

namespace Slots.Simulator;

/// <summary>
/// Base-game acceptance cases (Section 6.1 of the brief), checked via forced stops
/// against the wired engine. Feature cases (F01–F04) use an event script, which is an
/// internal test point of each engine — cover them in your test suite.
/// </summary>
internal static class AcceptanceMode
{
    private sealed record BaseCase(string Id, int[] Stops, long BetCents, long ExpectedCents);

    private static readonly BaseCase[] BaseCases =
    [
        new("G01", [54, 86, 60, 48, 88], 100, 0),
        new("G02", [40, 73, 41, 92, 92], 100, 20),
        new("G03", [83, 56, 69, 70, 46], 100, 110),
        new("G04", [90, 11, 61, 87, 75], 100, 1460),
        new("G05", [30, 72, 41, 62, 72], 100, 410),
        new("G06", [90, 11, 61, 87, 75], 10, 146),
        new("G07", [90, 11, 61, 87, 75], 500, 7300),
    ];

    public static int Run(ISlotEngineFactory factory)
    {
        ISlotEngine engine = factory.Create(seed: 0);
        bool allOk = true;

        Console.WriteLine("\n  base game (forced stops):");
        foreach (BaseCase c in BaseCases)
        {
            SpinResult result = engine.SpinWithStops(c.Stops, c.BetCents);
            bool ok = result.TotalWinCents == c.ExpectedCents;
            allOk &= ok;
            Console.WriteLine(
                $"  {c.Id}  bet={c.BetCents,4}  got={result.TotalWinCents,6}  expected={c.ExpectedCents,6}  {(ok ? "OK" : "FAILED")}"
            );
        }

        Console.WriteLine(allOk ? "\n  G01–G07 pass." : "\n  THERE ARE FAILURES.");
        Console.WriteLine("  F01–F04 (feature script) are not checked by the simulator: cover them in your tests.\n");
        return allOk ? 0 : 1;
    }
}
