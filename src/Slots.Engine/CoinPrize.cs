namespace Slots.Engine;

public sealed record CoinPrize(
    string Kind,
    int ValueTenths,
    int Weight
);