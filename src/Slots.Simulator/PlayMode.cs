using Slots.Contracts;

namespace Slots.Simulator;

/// <summary>Spin-by-spin play: shows the display, payouts, and feature step-by-step.</summary>
internal static class PlayMode
{
    private enum Command
    {
        Spin,
        ChangeBet,
        Quit,
    }

    public static int Run(ISlotEngineFactory factory, long betCents, ulong seed)
    {
        ISlotEngine engine = factory.Create(seed);

        long totalBet = 0;
        long totalWin = 0;
        int spins = 0;

        Console.WriteLine($"\n  bet {betCents} cents · seed {seed}");
        Console.WriteLine("  [space] spin · [b] change bet · [q] quit");

        while (true)
        {
            Command command = NextCommand();
            if (command == Command.Quit)
            {
                break;
            }

            if (command == Command.ChangeBet)
            {
                Console.Write("\n  new bet in cents (multiple of 10, minimum 10): ");
                if (long.TryParse(Console.ReadLine(), out long newBet) && newBet >= 10 && newBet % 10 == 0)
                {
                    betCents = newBet;
                    Console.WriteLine($"  bet is now {betCents} cents");
                }
                else
                {
                    Console.WriteLine("  invalid bet — kept at " + betCents);
                }
                continue;
            }

            SpinResult result = engine.Spin(betCents);
            spins++;
            totalBet += betCents;
            totalWin += result.TotalWinCents;

            PrintSpin(result);
            Console.WriteLine(
                $"\n  spin {spins} · bet {totalBet} · paid {totalWin} · balance {totalWin - totalBet:+0;-0;0} cents"
            );
        }

        if (spins > 0)
        {
            Console.WriteLine(
                $"\n  session: {spins} spins · observed RTP {100.0 * totalWin / totalBet:F1}%\n"
            );
        }
        return 0;
    }

    /// <summary>
    /// Space spins, B changes the bet, Q quits; other keys are ignored. With redirected
    /// input (no interactive console), each line becomes a spin and "q" ends the session.
    /// </summary>
    private static Command NextCommand()
    {
        if (Console.IsInputRedirected)
        {
            string? line = Console.ReadLine();
            return line is null || line.Trim().Equals("q", StringComparison.OrdinalIgnoreCase)
                ? Command.Quit
                : Command.Spin;
        }

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Spacebar:
                    return Command.Spin;
                case ConsoleKey.B:
                    return Command.ChangeBet;
                case ConsoleKey.Q:
                    return Command.Quit;
            }
        }
    }

    private static void PrintSpin(SpinResult result)
    {
        Console.WriteLine($"\n  stops [{string.Join(", ", result.Stops)}]");
        Console.WriteLine(GridRenderer.Render(result.Window));

        if (result.Wins.Count == 0)
        {
            Console.WriteLine("  no base-game win");
        }
        foreach (WinResult win in result.Wins)
        {
            Console.WriteLine($"  {win.Name} · {win.Detail} → {win.AmountCents} cents");
        }

        if (result.Feature is { } feature)
        {
            Console.WriteLine($"\n  ★ {feature.Name.ToUpperInvariant()} ★");
            foreach (FeatureStep step in feature.Steps)
            {
                Console.WriteLine($"  {step.Description}");
                if (step.Window is { } window)
                {
                    Console.WriteLine(GridRenderer.Render(window));
                }
            }
            Console.WriteLine($"  feature win: {feature.AmountCents} cents");
        }

        Console.WriteLine($"\n  ── total spin win: {result.TotalWinCents} cents ──");
    }
}
