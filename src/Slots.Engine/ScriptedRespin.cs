namespace Slots.Engine;

    public sealed record ScriptedCoin(
        int Position,
        CoinPrize Prize
    );

    public sealed record ScriptedRespin(
        IReadOnlyList<ScriptedCoin> Coins
    );