namespace Slots.Engine;

public sealed class GameConfig
{
    public HoldAndSpinConfig HoldAndSpin { get; set; } = new();

    public int ScatterTriggerCount { get; set; } = 3;
}

public sealed class HoldAndSpinConfig
{
    public int InitialLives { get; set; } = 3;

    public int CoinChancePercent { get; set; } = 8;

    public int GrandPrizeTenths { get; set; } = 5000;
}