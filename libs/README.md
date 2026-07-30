# DLL reference

The same content is in the documentation XML (IntelliSense). Monetary values are always
integers in **cents**.

## `Slots.Contracts` — the engine contract

The contract does not know the game rules: what pays, when a bonus triggers, and how the
bet is validated are engine decisions.

### Interfaces

```csharp
public interface ISlotEngineFactory
{
    // Creates an engine from a seed. Same seed → same sequence of results, always.
    ISlotEngine Create(ulong seed);
}

public interface ISlotEngine
{
    // Full spin: draws the outcome, evaluates wins, and runs any bonus stages that
    // trigger. Invalid bet for the game rules → ArgumentException.
    SpinResult Spin(long betCents);

    // Evaluates a spin with forced reel stops — the basis for reproducible acceptance
    // cases. Must not consume RNG; stages that depend on draws (bonus, features) do not
    // run, and SpinResult.Feature is null.
    SpinResult SpinWithStops(IReadOnlyList<int> stops, long betCents);
}
```

### Result types

```csharp
public sealed record SpinResult(
    IReadOnlyList<int> Stops,                       // drawn (or forced) stop for each reel
    IReadOnlyList<IReadOnlyList<string>> Window,    // displayed symbols, indexed [reel][row]
    IReadOnlyList<WinResult> Wins,                  // one item per win on the spin
    FeatureResult? Feature,                         // null when no bonus ran
    long TotalWinCents                              // sum of everything the spin paid, feature included
);

public sealed record WinResult(
    string Name,        // win source: a symbol, "SC"…
    string Detail,      // how the amount was formed, e.g.: "4 reels · 2 ways · table 13"
    long AmountCents
);

public sealed record FeatureResult(
    string Name,                        // e.g.: "Hold & Spin"
    IReadOnlyList<FeatureStep> Steps,   // step by step, in the order they happened
    long AmountCents                    // total paid by the stage
);

public sealed record FeatureStep(
    string Description,                              // what happened in the step, as text
    IReadOnlyList<IReadOnlyList<string>>? Window     // grid state after the step, [reel][row]; optional
);
```

When the bonus has a grid, the final state is the `Window` of the last step that provides one.
Display fields (`Detail`, `Description`, `Window`) exist so the simulator can print the
spin — the engine describes what happened and the simulator only displays it.

## `Slots.Rng` — the randomness source

```csharp
public interface IRng
{
    // Next 32 raw bits from the generator.
    uint NextUInt();

    // Uniform integer greater than or equal to 0 and less than maxExclusive — that is,
    // from 0 to maxExclusive − 1. The value of maxExclusive is never drawn.
    uint Next(uint maxExclusive);
}

public sealed class Pcg32 : IRng
{
    // Same seed → same sequence, on any machine and runtime version.
    public Pcg32(ulong seed, ulong sequence = 1);
}
```

Use `Pcg32` as the engine's randomness source, **injected** via the constructor with an
explicit seed — never a global RNG. Example:

```csharp
public ISlotEngine Create(ulong seed) => new SlotEngine(config, new Pcg32(seed));
```
