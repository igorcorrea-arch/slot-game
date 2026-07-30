using Slots.Simulator;
using Slots.Contracts;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ─── Your engine ─────────────────────────────────────────────────────────────
// The simulator only knows the contract (Slots.Contracts.ISlotEngineFactory).
// The Slots.Engine project is already wired here: implement the contract in it.
ISlotEngineFactory factory = new Slots.Engine.SlotEngineFactory();
// ─────────────────────────────────────────────────────────────────────────────

return args switch
{
    ["cases", ..] => AcceptanceMode.Run(factory),
    ["sim", .. var rest] => SimMode.Run(
        factory,
        spins: rest.Length > 0 ? long.Parse(rest[0]) : 1_000_000,
        betCents: rest.Length > 1 ? long.Parse(rest[1]) : 100,
        seed: rest.Length > 2 ? ulong.Parse(rest[2]) : 12345
    ),
    ["play", .. var rest] => PlayMode.Run(
        factory,
        betCents: rest.Length > 0 ? long.Parse(rest[0]) : 100,
        seed: rest.Length > 1 ? ulong.Parse(rest[1]) : (ulong)Environment.TickCount64
    ),
    _ => Help(),
};

static int Help()
{
    Console.WriteLine(
        """
        Hold & Spin — challenge simulator

        usage:
          dotnet run cases                        checks the base-game acceptance cases
          dotnet run sim   [spins] [bet] [seed]   batch simulation   (default: 1000000 100 12345)
          dotnet run play  [bet] [seed]           play spin by spin  (default: 100, clock seed)

        bet in cents, multiple of 10, minimum 10.
        """
    );
    return 1;
}
