# Ino Games - Backend Assessment

> **🚫 DO NOT FORK THIS REPOSITORY**
> 
> This repository is provided for technical assessment purposes only.
> 
> **✅ Permitted:** Clone locally for evaluation only  
> **❌ Prohibited:** Fork, copy, share, or distribute

# Technical challenge — Hold & Spin

The full brief is in **`challenge-slot-math.md`**. Read it first.

## Requirements

- .NET 10 SDK

## What's in here

| Item | What it is |
|---|---|
| `challenge-slot-math.md` | The challenge brief |
| `reels-base.csv`, `paytable.csv`, `prizes.csv` | Raw game data |
| `src/Slots.Engine/` | **Your project.** Engine skeleton, already wired into the simulator — implement the contract here |
| `src/Slots.Simulator/` | The simulator (you don't need to change it) |
| `libs/Slots.Contracts.dll` | The contract your engine implements |
| `libs/Slots.Rng.dll` | The challenge RNG (PCG32) — inject it into your engine |
| `libs/README.md` | DLL reference: interfaces, types, and semantics (also in IntelliSense) |

## How to run

```bash
cd src/Slots.Simulator

dotnet run cases                      # checks cases G01–G07 against your engine
dotnet run sim 1000000 100 12345      # batch simulation: spins, bet (cents), seed
dotnet run play 100                   # play spin by spin
```

In `play` mode: **space** spins, `b` changes the bet, `q` quits.

Before you implement anything, `dotnet run cases` fails with `NotImplementedException` — that's the
starting point. The CSVs are copied to `<output>/data` automatically on build.

## Questions?

Any question — brief, kit, environment — feel free to ask.
Asking does not cost you points.

---

## ⚖️ License

This project is proprietary software owned by **Ino Games** and provided for technical assessment purposes only. 

**Important:** This code is restricted for evaluation use only. Commercial use, distribution, or sharing outside the assessment context is prohibited. See the [LICENSE](LICENSE) file for complete terms and conditions.

By accessing this repository, you acknowledge and agree to the licensing terms set forth by Ino Games.

---

© 2026 Ino Games - All Rights Reserved
