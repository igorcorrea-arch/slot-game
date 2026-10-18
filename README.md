# Slot Math Engine

A deterministic slot game engine implemented in C# / .NET 10.

## Implementation

The engine implements:

- A 5-reel, 3-row base game with configurable reel strips.
- Ways evaluation with Wild substitution.
- Scatter evaluation and Hold & Spin triggering.
- Hold & Spin with locked coin positions, respins, multipliers, and Grand awards.
- Deterministic random number generation using the provided PCG32 implementation.
- External CSV data and JSON configuration.

## Architecture

`SlotEngine` coordinates the base game and bonus feature.

`WaysEvaluator` and `ScatterEvaluator` handle the base game payouts.

`HoldAndSpinFeature` manages the bonus flow, using `HoldAndSpinState` for state transitions and `FeaturePayoutCalculatorFacade` for payout calculations.

The engine is instantiated through `SlotEngineFactory`, which loads the game data and configuration.

## Build

Requires .NET 10 SDK.

```bash
dotnet build src/Slots.Simulator/Slots.Simulator.csproj -c Release
```

## Tests

```bash
dotnet test tests/Slots.Engine.Tests/Slots.Engine.Tests.csproj
```

Tests cover base game calculations, bonus state transitions, prize selection, payout calculations, and scripted Hold & Spin scenarios.

## Configuration

Game parameters are defined in `game-config.json`, including:

- Initial Hold & Spin lives
- Coin appearance probability
- Grand prize value
- Scatter trigger count

Reel strips, paytable, and prize weights are loaded from CSV files.

## Determinism

The engine uses the supplied seeded PCG32 RNG. For the same seed, configuration, and sequence of calls, it produces the same results.

`SpinWithStops()` evaluates supplied reel stops without consuming RNG or triggering the bonus feature.