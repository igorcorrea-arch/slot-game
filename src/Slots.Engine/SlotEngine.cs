using Slots.Contracts;
using Slots.Rng;

namespace Slots.Engine;

/// <summary>
/// Your engine. Implement the contract here — how you organize the rest of the project
/// (configuration, evaluation, feature) is your decision.
/// </summary>
public sealed class SlotEngine(IRng rng) : ISlotEngine
{
    private readonly IRng _rng = rng;

    public SpinResult Spin(long betCents)
    {
        // Full spin: draw stops, build the window, pay ways and scatter,
        // and if the feature triggers, run it to completion.
        throw new NotImplementedException("implement the full spin");
    }

    public SpinResult SpinWithStops(IReadOnlyList<int> stops, long betCents)
    {
        // Forced stops, without consuming RNG — this is how the simulator's `cases`
        // mode checks G01–G07. Stages that depend on draws do not run.
        throw new NotImplementedException("implement evaluation with forced stops");
    }
}
