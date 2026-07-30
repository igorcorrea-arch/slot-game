using Slots.Contracts;
using Slots.Rng;

namespace Slots.Engine;

/// <summary>
/// Entry point for your engine: the simulator creates the engine through here.
/// Same seed → same sequence of results, always.
/// </summary>
public sealed class SlotEngineFactory : ISlotEngineFactory
{
    public ISlotEngine Create(ulong seed)
    {
        // Load your configuration here (CSVs are at <output>/data)
        // and inject the RNG into the engine. Internal structure is entirely yours.
        return new SlotEngine(new Pcg32(seed));
    }
}
