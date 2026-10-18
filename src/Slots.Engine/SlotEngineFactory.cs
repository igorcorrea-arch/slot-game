using Slots.Contracts;
using Slots.Rng;
using System.Text.Json;

namespace Slots.Engine;

/// <summary>
/// Entry point for your engine: the simulator creates the engine through here.
/// Same seed → same sequence of results, always.
/// </summary>
public sealed class SlotEngineFactory : ISlotEngineFactory
{
    public ISlotEngine Create(ulong seed)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "data", "game-config.json");
        var configJson = File.ReadAllText(configPath);

        var config = JsonSerializer.Deserialize<GameConfig>(configJson, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            }
        ) ?? throw new InvalidOperationException(
            "Invalid game configuration."
        );
        
        var reelsPath = Path.Combine(AppContext.BaseDirectory, "data", "reels-base.csv");
        var reels = ReelLoader.Load(reelsPath);
        Console.WriteLine($"Reels loaded: {string.Join(", ", reels.Select(r => r.Count))}");
        
        var payTablePath = Path.Combine(AppContext.BaseDirectory, "data", "paytable.csv");
        var payTable = PayTableLoader.Load(payTablePath);
        Console.WriteLine($"PayTable loaded: {payTable.Count} symbols");
        
        var prizesPath = Path.Combine(AppContext.BaseDirectory, "data", "prizes.csv");
        var prizes = PrizeLoader.Load(prizesPath);
        
        return new SlotEngine(new Pcg32(seed), reels, payTable, prizes, config);
    }
}
