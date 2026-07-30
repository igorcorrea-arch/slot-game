using Slots.Contracts;

namespace Slots.Simulator;

/// <summary>Batch simulation: N spins, long accumulators, summary at the end.</summary>
internal static class SimMode
{
    public static int Run(ISlotEngineFactory factory, long spins, long betCents, ulong seed)
    {
        ISlotEngine engine = factory.Create(seed);

        long totalBet = 0;
        long totalWin = 0;
        long hits = 0;
        long triggers = 0;
        long featureZero = 0;
        long maxWin = 0;

        Console.WriteLine($"\n  {spins:N0} spins · bet {betCents} cents · seed {seed}\n");

        long progressStep = Math.Max(spins / 10, 1);
        for (long i = 1; i <= spins; i++)
        {
            SpinResult result = engine.Spin(betCents);
            totalBet += betCents;
            totalWin += result.TotalWinCents;
            if (result.TotalWinCents > 0)
            {
                hits++;
            }
            if (result.Feature is { } feature)
            {
                triggers++;
                if (feature.AmountCents == 0)
                {
                    featureZero++;
                }
            }
            maxWin = Math.Max(maxWin, result.TotalWinCents);

            if (i % progressStep == 0)
            {
                double rtpSoFar = 100.0 * totalWin / totalBet;
                Console.WriteLine($"  {100 * i / spins,3}%  partial rtp {rtpSoFar,7:F3}%");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"  total bet          {totalBet:N0} cents");
        Console.WriteLine($"  total paid         {totalWin:N0} cents");
        Console.WriteLine($"  RTP                {100.0 * totalWin / totalBet:F4}%");
        Console.WriteLine($"  hit frequency      {100.0 * hits / spins:F2}%");
        if (triggers > 0)
        {
            Console.WriteLine($"  triggers           {triggers:N0}  (1 in {(double)spins / triggers:F0})");
            Console.WriteLine($"  feature pays zero  {100.0 * featureZero / triggers:F2}% of triggers");
        }
        Console.WriteLine($"  biggest win        {maxWin:N0} cents ({(double)maxWin / betCents:F0}× the bet)");
        Console.WriteLine();
        return 0;
    }
}
