# Technical challenge — Slot Game Developer (Backend)

**Deadline:** 7 calendar days from receipt

**Language:** C# (.NET 10).

**Kit materials:** `reels-base.csv`, `paytable.csv`, `prizes.csv`, the contract, the RNG, and the simulator — see "What you get"

---

## 1. Before you start

### We don't assume you know slots

This document contains **all** the domain you need. We evaluate rule modeling, not prior casino knowledge.

We recommend reading Section 2 before you start writing code.

### What you get

- **The three CSVs** with the raw game data (Section 4).
- **`Slots.Contracts.dll`** — the contract your engine implements to work with the simulator (Section 7, E2). Interfaces and types are documented in the assembly itself and, in markdown, in `libs/README.md`.
- **`Slots.Rng.dll`** — the challenge randomness source: a deterministic PCG32. Your engine must use it, injected as described in Section 2.7.
- **The `Slots.Engine` project** — your engine skeleton, already referencing the DLLs and wired into the simulator. Open the solution and implement the contract there; the project's internal structure is entirely yours.
- **The simulator code** (`Slots.Simulator`), with three modes:
  - `dotnet run cases` — checks the base-game acceptance cases (Section 6.1) against your engine;
  - `dotnet run sim [spins] [bet] [seed]` — batch simulation with RTP and statistics;
  - `dotnet run play [bet] [seed]` — play spin by spin, seeing the display and payouts.

### What we evaluate

- Your ability to turn a specification into a correct, testable model.
- **How you model the game configuration.** You get raw CSV data and rules in text; the configuration format is your decision.
- **Code clarity.** Someone who didn't write it — or you months later — needs to be able to read and change this code. We evaluate how carefully you treat that future reader.

### What we do **not** evaluate

- APIs, HTTP, databases, authentication, queues, containers — you don't need to build any of that.
- Graphical UI. You don't need to draw spinning reels.
- Extreme performance. Fast enough to simulate is enough.

### Practical rules

- **Ambiguity:** if something is ambiguous, decide, document the decision in the README, and move on. Recording explicit assumptions is a positive signal.
- **AI use:** allowed and does not need to be declared. In the live session you will run and explain your own code, so what matters is owning what you deliver.
- **Libraries:** free choice. The randomness source is the one provided in the kit (`Slots.Rng.dll`) — see Section 2.7.
- **Scope:** if time is tight, prefer delivering fewer items done well and note in the README what was left out.

---

## 2. Domain fundamentals

### 2.1 What a slot game is, mechanically

A slot is, in practice, a function:

```
spin(configuration, state, randomness) -> (new state, win)
```

The player bets, the engine draws an outcome, calculates the win, and returns. All the complexity sits in three places: **how the draw happens**, **how the win is calculated**, and **what state survives between spins**.

Every commercial slot is organized in two layers: a **base game** — the paid spin that runs all the time — and one or more **features**: special, rare modes triggered by base-game events, each with its own rules and draw model. This game has a base game and exactly one feature, described from Section 2.5 onward.

### 2.2 Reels and stop position

A common mistake for outsiders is to imagine that each screen cell gets a symbol drawn independently. **That is not how it works** — at least not on a normal spin.

Each column is a **reel**: a circular list of symbols with fixed order. The full name of the data is *reel strip*, but in practice everyone just says *reel*, and that is what we will do here. Example of a 12-position reel:

```
position: 0   1   2   3   4   5   6   7   8   9  10  11
symbol:   L1  L3  H2  L4  WL  L1  SC  L2  H1  L4  L3  L2
```

The random generator draws **one stop position per reel**. The screen shows that position and the next two, top to bottom, with *wrap-around*:

- Stop at 4 → the column shows `WL, L1, SC`
- Stop at 10 → the column shows `L3, L2, L1` (wraps to position 0)

Two important consequences:

1. Each symbol's frequency is controlled by **how many times it appears on the reel**. Game math is tuned by changing data, not hard-coded probabilities.
2. The reel is the unit of configuration. Changing a reel's composition changes the whole game without touching a line of logic.

Reels are independent of each other: each has its own composition and its own stop draw. **Reels do not all have the same length** — that is normal, and your engine must handle it.

> Keep the caveat from the start: this is the draw model for a **normal spin**. This game's feature uses a different model, described in Section 3.6. A well-built engine treats "draw an outcome" as an abstraction, not as a synonym for "draw reel stops".

### 2.3 The display and *ways*

The screen is a **5 reels × 3 rows** display. The pay mode is called **243 ways** (3⁵ = 243 possible left-to-right paths).

The rule is:

- A symbol forms a win if it appears on **consecutive reels starting mandatorily from reel 1**.
- **Vertical position does not matter**. Only whether the symbol is present on the reel.
- The number of *ways* is the **product** of the symbol counts on each reel in the sequence.
- The win is `paytable[symbol][reel_count] × number_of_ways`.

Example. Consider this display and follow only the symbol `H2`, highlighted:

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | **H2** | L1 | **H2** | L4 | L3 |
| **Row 2** | **H2** | **H2** | L2 | L1 | **H2** |
| **Row 3** | L4 | L3 | **H2** | L2 | L4 |
| **`H2` count** | **2** | **1** | **2** | **0** ← break | **1** (does not count) |

The sequence goes from reel 1 to reel 3 and **stops there**, because reel 4 has no `H2`. The `H2` on reel 5 **is left out**: the sequence must be consecutive reels from reel 1, and the gap on reel 4 ended it. Length `n = 3`.

```
ways     = 2 × 1 × 2 = 4
units    = 100 ÷ 10 = 10
win      = units × paytable[H2][3] × ways
         = 10 × 5 × 4
         = 200 cents
```

What is `units`? It is how many minimum bets (10 cents) fit into the bet placed — the factor that scales the table win to the spin's bet. Section 2.8 explains the convention in detail.

Notice that **vertical position was irrelevant** — the `H2`s are scattered across different rows and still form the 4 combinations. And that **a reel without the symbol ends the count for good**: the `H2` on reel 5 exists on the display, but does not reopen the sequence or increase the win.

If the symbol does not appear on reel 1, it pays nothing — there is no right-to-left pay in this game.

### 2.4 The three special symbols

- **Wild (`WL`)** substitutes for any paying symbol. It counts toward the count of *any* symbol, including several different symbols on the same spin. Wild does **not** substitute for scatter or coin.

- **Scatter (`SC`)** pays **by count, in any position on the display** — it does not need consecutive reels and does not multiply by ways. Besides paying, it is what triggers the feature. It is not substituted by wild.

- **Coin (`CN`)** — the prize coin. **It does not exist in the base game and does not appear on any reel.** It only appears inside the feature, and each one reveals on landing what it carries: a cash value or a multiplier. See Section 3.6.

### 2.5 Base game and feature

The game has two modes:

- **Base game:** the normal spin, paid by the player. Pays by ways and by scatter.
- **Hold & Spin (the *feature*):** triggered by 3 or more scatters. Here the game changes nature — there are no more ways, no more reel stops, and what matters is accumulating coins on a grid that locks in place.

**Feature respins cost the player nothing.** The only paid spin is the base-game spin that triggered it; everything the feature yields is additional win from that same spin.

### 2.6 RTP and paytable

Two terms this document uses:

- **RTP** (*return to player*): ratio of total paid to total bet, over the long run. It is a **design target** — the game configuration is calibrated to hit it.
- **Paytable**: win table by (symbol, count).

### 2.7 Determinism and RNG

Two conventions that are not obvious outside the industry:

1. **Every run starts from a seed.** Same seed and same sequence of operations produce exactly the same result, always, on any machine.

2. **The RNG is injected, never global.** No `Math.random()`, `rand()`, or equivalent scattered through the code.

Use the PCG32 provided in `Slots.Rng.dll` as the engine's randomness source — injected and with an explicit seed. You don't need to implement any RNG; what we evaluate is how it enters your engine, not the generator itself.

### 2.8 Money is always integer

This is the industry's most important convention and the one that surprises outsiders most: **in a game engine, money is never floating point.** Every amount is an integer in the smallest monetary unit — here, cents. A win of "13.00" is the integer `1300`.

**This challenge's convention.** The bet is a parameter, in cents, with a minimum of **10 cents**. Tables do not store cents: they store **tenths of the bet**. A value of `15` in the table means 1.5× the bet; a value of `1` means 0.1×.

Another way to read the same thing: each table value answers *"how many cents does this pay if the bet is the minimum?"*. For larger bets, just scale. That is where `units` comes from:

```
units = bet_in_cents ÷ 10
win   = units × table_value × (ways, when applicable)
```

`units` is **how many minimum bets fit into the bet placed** — the win scale factor:

| Bet | units | Meaning |
|---|---|---|
| 10 cents | 1 | 1× the minimum bet |
| 100 cents | 10 | 10× the minimum bet |
| 500 cents | 50 | 50× the minimum bet |

Acceptance cases `G04`, `G06`, and `G07` (Section 6) show this in practice: same stops, and the same base table value becomes `1460`, `146`, and `7300` cents as the bet is 100, 10, or 500. **The win scales linearly with the bet, and `units` is the only point where the bet enters the calculation.**

**The bet must be a multiple of 10 cents.** With that constraint, `units` is always an integer and the whole chain stays free of fractional division — there is never rounding and never a policy to define.

Floating point only appears at the end, if you report ratios or statistics — which are not money.

Choose your accumulator types carefully for the volume of spins you intend to simulate.

---

## 3. Game specification

### 3.1 Layout

- Grid of **5 reels × 3 rows** — 15 cells.
- **243 ways** in the base game, as in Section 2.3.
- **The bet is a spin parameter**, in cents, multiple of 10, minimum 10 cents. Examples and most acceptance cases use 100 cents as the reference.
- All wins are integers in cents, calculated as in Section 2.8.
- **The game RTP is 96.0%.** The provided configuration is already calibrated to that target — you don't need to balance anything.

### 3.2 Symbols

| Code | Role | Appears on reels? |
|---|---|---|
| `WL` | Wild | yes |
| `SC` | Scatter — pays and triggers the feature | yes |
| `H1`, `H2`, `H3` | High symbols (paying) | yes |
| `L1`, `L2`, `L3`, `L4` | Low symbols (paying) | yes |
| `CN` | Coin — prize coin; only exists inside the feature | **no** |

### 3.3 Paytable

Source: `paytable.csv`. Values in **tenths of the bet** — see Section 2.8. The column in parentheses shows the same value as a multiple of the bet, for readability only.

| Symbol | 3 | 4 | 5 |
|---|---|---|---|
| H1 | 10 *(1.0×)* | 28 *(2.8×)* | 96 *(9.6×)* |
| H2 | 5 *(0.5×)* | 13 *(1.3×)* | 45 *(4.5×)* |
| H3 | 4 *(0.4×)* | 10 *(1.0×)* | 30 *(3.0×)* |
| L1 | 2 *(0.2×)* | 4 *(0.4×)* | 14 *(1.4×)* |
| L2 | 2 *(0.2×)* | 3 *(0.3×)* | 11 *(1.1×)* |
| L3 | 1 *(0.1×)* | 3 *(0.3×)* | 9 *(0.9×)* |
| L4 | 1 *(0.1×)* | 2 *(0.2×)* | 8 *(0.8×)* |
| **SC** | **30** *(3.0×)* | **150** *(15×)* | **1000** *(100×)* |

`L3` and `L4` pay the same on 3 reels and only diverge from 4 onward — intentional, not a table error.

**Watch the scatter row.** For paying symbols, the value is **per way** and must be multiplied by the number of ways. For `SC`, you do not multiply by ways — only by bet units. Three scatters pay `units × 30` cents: with a bet of 100, that is 300 cents.

### 3.4 Evaluating paying symbols (ways)

1. For each paying symbol, count how many times it appears on each reel, counting **wilds** as occurrences of that symbol.
2. Walk left to right from reel 1 and stop at the first reel with a zero count. The sequence length is `n`.
3. If `n ≥ 3`, that symbol's win is `units × paytable[symbol][n] × (product of the counts on reels 1..n)`.
4. Each symbol pays **only its maximum length**. `H2` present on 4 reels pays only the 4-reel line, not also the 3-reel line.
5. Different symbols **stack**: the spin's ways win is the sum.
6. The same wild can count simultaneously for several symbols.
7. **Wild does not pay on its own.** It is not a paying symbol and has no paytable row.

### 3.5 Evaluating the scatter

1. Count how many `SC` appear on the whole display. Position and adjacency are irrelevant.
2. If the count is 3, 4, or 5, pay `units × SC row value` — **once, without multiplying by ways**.
3. The scatter win **adds** to the ways win of the same spin.
4. If the count is 3 or more, the spin also **triggers the feature**.

### 3.6 The feature: Hold & Spin

**Trigger:** 3 or more scatters. The ways win and the scatter win from that spin are paid normally, in addition to whatever the feature yields.

**Start.** The feature grid starts **completely empty** — scatters do not lock or turn into coins. The **lives** counter starts at **3**.

**Respins.** While there are lives and the grid is not full:

1. Each **unlocked** cell draws **independently** whether it receives a coin (`CN`). The chance is **8 in 100** per cell.

   > Notice that here **there is no reel stop**: the draw is per cell, not per column, and the coin does not come from any strip — it is generated by probability. This is the game's second sampling model.

2. Every new coin locks in that position and draws its contents from the prize table.
3. If **at least one** coin landed on the respin, lives **reset to 3**. If none landed, lives drop by 1.

**End.** The feature ends when lives reach zero **or** when all 15 cells are locked — whichever comes first.

> Because the grid starts empty, it is possible that no coin lands in three consecutive respins and **the feature pays zero**. That is expected, and your engine must handle that path.

**Prize table.** Each coin draws an entry from `prizes.csv`, with weights. There are two types:

- `cash` — a cash value, added to the total.
- `mult` — a multiplier. Does **not** add value; multiplies the total at the end. But it is a coin like any other: occupies a cell, counts toward filling the grid, and resets lives.

There is no separate symbol for the multiplier, because **there is a single draw from a single table**. The symbol is always `CN`; `cash` or `mult` is the prize it revealed on landing.

`cash` values are also in **tenths of the bet**.

| Type | Value | | Weight |
|---|---|---|---|
| cash | 3 | *(0.3×)* | 21 |
| cash | 6 | *(0.6×)* | 18 |
| cash | 9 | *(0.9×)* | 14 |
| cash | 15 | *(1.5×)* | 12 |
| cash | 30 | *(3.0×)* | 11 |
| cash | 60 | *(6.0×)* | 10 |
| cash | 150 | *(15×)* | 5 |
| cash | 300 | *(30×)* | 1 |
| mult | ×2 | | 6 |
| mult | ×3 | | 2 |

**Grand.** In casino games, jackpots usually come in a ladder of fixed tiers — Mini, Minor, Major, and **Grand**, the largest. This game has only the Grand tier, and it is reserved for filling the grid: if all 15 cells become locked, add **5,000 tenths** (500× the bet) to the win. **Grand is not multiplied**: multipliers apply only to `cash` coins; Grand is added afterward.

**Final feature calculation:**

```
win = units × (sum of cash × product of mults + grand, if any)
```

Three points that often get missed:

- **The multiplier applies to all `cash` on the grid, not only what comes after it.** The order in which coins landed is irrelevant: a `×2` that lands on the last respin multiplies equally the value that landed on the first. There is no "local" multiplier or per-coin multiplier.
- **Multipliers compose with each other by product, not by sum.** Two `×2` coins and one `×3` result in `×12`, not `×7`. If none land, the product is `1`.
- **Grand stays outside the multiplier.** It is added to the win after `cash` has already been multiplied.

### 3.7 Resolution order

Base-game spin:

1. Draw the stop for each of the 5 reels, from reel 1 to reel 5.
2. Build the display.
3. Evaluate ways wins.
4. Count scatters and pay the `SC` line, if applicable.
5. If there are 3 or more scatters, run the full feature.
6. Sum everything and return.

Feature:

1. Empty grid, lives = 3.
2. Repeat while lives > 0 and locked < 15:
   a. For each unlocked cell, draw whether it receives a coin.
   b. For each new coin, lock and draw its contents.
   c. If there was at least one new coin, lives = 3; otherwise, lives = lives − 1.
3. Multiply the sum of `cash` by the product of the multipliers.
4. If all 15 cells are locked, add the Grand.

---

## 4. Provided data

You receive **raw data**, not a ready-made configuration. Building the configuration is part of the challenge — see deliverable E1.

### `reels-base.csv`

Wide format: the first column is the position on the reel (zero-based), and each following column is a reel.

```csv
position,reel1,reel2,reel3,reel4,reel5
0,L2,L4,WL,L2,L2
1,H1,L1,H3,L4,L1
...
```

**Reels have different lengths**, so the last rows of the file have empty cells in the shorter columns. An empty cell means that reel has ended, not that there is a blank symbol.

### `paytable.csv` and `prizes.csv`

```csv
symbol,count,pay_tenths
H1,3,10
...
SC,3,30
```

```csv
kind,value_tenths,weight
cash,3,21
...
mult,2,6
```

Values are in **tenths of the bet**, not cents — the `_tenths` suffix on the columns reminds you of that. For `mult`, the `value_tenths` column stores the multiplier itself (2 means ×2), not a tenth.

All values are integers — read them as integers in all three files.

### Load check

After loading the reels, check your counts against the table below. If any number diverges, review the file reading before continuing.

| Reel | WL | SC | H1 | H2 | H3 | L1 | L2 | L3 | L4 | Total |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 0 | 2 | 8 | 10 | 11 | 14 | 16 | 16 | 16 | **93** |
| 2 | 5 | 3 | 8 | 10 | 11 | 14 | 16 | 16 | 16 | **99** |
| 3 | 5 | 3 | 8 | 10 | 11 | 14 | 16 | 16 | 16 | **99** |
| 4 | 5 | 3 | 8 | 10 | 11 | 14 | 16 | 16 | 16 | **99** |
| 5 | 0 | 2 | 8 | 10 | 11 | 14 | 16 | 16 | 16 | **93** |

---

## 5. Worked examples

### 5.1 Base game

Stops `[90, 11, 61, 87, 75]`, bet of **100 cents** — so `units = 10`. This is case `G04` from Section 6.

|  | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | L3 | H3 | L4 | L3 | L3 |
| **Row 2** | L3 | **WL** | L2 | L3 | H3 |
| **Row 3** | L4 | L3 | **WL** | H3 | L3 |

**Evaluating `L3`:**

| Reel | Count (L3 + WL) |
|---|---|
| 1 | 2 |
| 2 | 2 (one L3 and the wild) |
| 3 | 1 (the wild) |
| 4 | 2 |
| 5 | 2 |

`n = 5`, ways `= 2 × 2 × 1 × 2 × 2 = 16`, table `= 9` tenths, win `= 10 × 9 × 16 = 1440` cents.

**Evaluating `L4`:**

| Reel | Count (L4 + WL) |
|---|---|
| 1 | 1 |
| 2 | 1 (the wild) |
| 3 | 2 (one L4 and the wild) |
| 4 | 0 → sequence broken |

`n = 3`, ways `= 1 × 1 × 2 = 2`, table `= 1` tenth, win `= 10 × 1 × 2 = 20` cents.

**Both wilds were counted for both `L3` and `L4`.** That is correct.

There are no scatters on the display, so there is no scatter win and the feature does not trigger.

**Total spin win: `1440 + 20 = 1460` cents.**

Cases `G06` and `G07` repeat exactly these stops with bets of 10 and 500 cents, and the wins come out `146` and `7300` — proportional, with no rounding.

### 5.2 Feature

This is case `F01` from Section 6.2, with a bet of **100 cents**. `·` marks an empty cell; a locked cell shows the contents the coin revealed, **in tenths of the bet**.

**Start.** Empty grid, **lives = 3**.

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | · | · | · | · | · |
| **Row 2** | · | · | · | · | · |
| **Row 3** | · | · | · | · | · |

**Respin 1.** All 15 cells draw, each with an 8-in-100 chance. Two coins land, of `3` and `15` tenths.

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | · | **3** | · | · | · |
| **Row 2** | · | · | · | · | · |
| **Row 3** | · | · | · | **15** | · |

Coin landed → **lives reset to 3**. Locked: 2.

**Respin 2.** The 13 empty cells draw. Nothing lands. Grid unchanged, **lives = 2**.

**Respin 3.** One coin lands, and it is a `×2` multiplier.

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | · | **3** | · | · | · |
| **Row 2** | · | · | **×2** | · | · |
| **Row 3** | · | · | · | **15** | · |

Notice three things: **the `×2` coin occupies a cell like any other** and now counts toward filling the grid; **it resets lives to 3**, even without adding any value; and **it will multiply the two coins that landed before it**, not only what comes after. Locked: 3.

**Respins 4, 5, and 6.** Nothing lands on any of them. Lives drop to 2, then 1, then 0. The feature ends.

**Final calculation:**

```
cash sum        = 3 + 15 = 18 tenths
mult product    = 2
units           = 100 ÷ 10 = 10
locked = 3 of 15  ->  grid not full, no Grand

win = 10 × (18 × 2) = 360 cents
```

Twelve cells stayed empty. That is normal — filling the grid is rare.

---

## 6. Acceptance cases

Your engine must reproduce the eleven cases exactly. Include them in your test suite.

The simulator's `cases` mode checks G01–G07 via the contract (forced stops). Feature cases (F01–F04) depend on the event script, which is an internal test point of your engine — they are verified by your tests.

### 6.1 Base game

Specified by **stop position and bet**, not by seed — they do not depend on RNG. `G06` and `G07` repeat the stops from `G04` with different bets: use them to check that the bet flows through the whole chain.

#### G01 — No win: no symbol reaches 3 reels and there are fewer than 3 scatters.

Stops: `[54, 86, 60, 48, 88]` · Bet: **100 cents** (units = 10)

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | H1 | L4 | L3 | L3 | L1 |
| **Row 2** | H2 | L3 | L4 | L1 | H2 |
| **Row 3** | L2 | L1 | L2 | WL | L2 |

Scatters on display: **0**

No symbol reaches 3 consecutive reels from reel 1.

Ways: `0` · Scatter: `0`

**Total spin win (excluding feature): `0` cents**

---

#### G02 — Small 3-reel win.

Stops: `[40, 73, 41, 92, 92]` · Bet: **100 cents** (units = 10)

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | L4 | L4 | L2 | H3 | L3 |
| **Row 2** | H3 | L4 | L4 | H1 | L2 |
| **Row 3** | L1 | L2 | SC | L2 | L1 |

Scatters on display: **1**

| Symbol | Reels | Ways | Table (tenths) | Win (cents) |
|---|---|---|---|---|
| L4 | 3 | 2 | 1 | 20 |

Ways: `20` · Scatter: `0`

**Total spin win (excluding feature): `20` cents**

---

#### G03 — Wild expanding the ways count.

Stops: `[83, 56, 69, 70, 46]` · Bet: **100 cents** (units = 10)

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | L4 | SC | H1 | L2 | SC |
| **Row 2** | H1 | WL | L2 | L1 | L4 |
| **Row 3** | L1 | L1 | L4 | L2 | L4 |

Scatters on display: **2**

| Symbol | Reels | Ways | Table (tenths) | Win (cents) |
|---|---|---|---|---|
| H1 | 3 | 1 | 10 | 100 |
| L4 | 3 | 1 | 1 | 10 |

Ways: `110` · Scatter: `0`

**Total spin win (excluding feature): `110` cents**

---

#### G04 — Two symbols pay on the same spin.

Stops: `[90, 11, 61, 87, 75]` · Bet: **100 cents** (units = 10)

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | L3 | H3 | L4 | L3 | L3 |
| **Row 2** | L3 | WL | L2 | L3 | H3 |
| **Row 3** | L4 | L3 | WL | H3 | L3 |

Scatters on display: **0**

| Symbol | Reels | Ways | Table (tenths) | Win (cents) |
|---|---|---|---|---|
| L3 | 5 | 16 | 9 | 1440 |
| L4 | 3 | 2 | 1 | 20 |

Ways: `1460` · Scatter: `0`

**Total spin win (excluding feature): `1460` cents**

---

#### G05 — 3 scatters: they pay **and** trigger Hold & Spin. The ways win pays normally.

Stops: `[30, 72, 41, 62, 72]` · Bet: **100 cents** (units = 10)

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | L1 | L2 | L2 | L2 | H2 |
| **Row 2** | SC | L4 | L4 | SC | L3 |
| **Row 3** | L2 | L4 | SC | L1 | L2 |

Scatters on display: **3** → **triggers the feature**

| Symbol | Reels | Ways | Table (tenths) | Win (cents) |
|---|---|---|---|---|
| L2 | 5 | 1 | 11 | 110 |

Ways: `110` · Scatter: `300`

**Total spin win (excluding feature): `410` cents**

---

#### G06 — **Same stops as G04, with minimum bet.** All values drop to one tenth.

Stops: `[90, 11, 61, 87, 75]` · Bet: **10 cents** (units = 1)

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | L3 | H3 | L4 | L3 | L3 |
| **Row 2** | L3 | WL | L2 | L3 | H3 |
| **Row 3** | L4 | L3 | WL | H3 | L3 |

Scatters on display: **0**

| Symbol | Reels | Ways | Table (tenths) | Win (cents) |
|---|---|---|---|---|
| L3 | 5 | 16 | 9 | 144 |
| L4 | 3 | 2 | 1 | 2 |

Ways: `146` · Scatter: `0`

**Total spin win (excluding feature): `146` cents**

---

#### G07 — **Same stops as G04, with a 5.00 bet.** All values multiply by 5.

Stops: `[90, 11, 61, 87, 75]` · Bet: **500 cents** (units = 50)

| | Reel 1 | Reel 2 | Reel 3 | Reel 4 | Reel 5 |
|---|:---:|:---:|:---:|:---:|:---:|
| **Row 1** | L3 | H3 | L4 | L3 | L3 |
| **Row 2** | L3 | WL | L2 | L3 | H3 |
| **Row 3** | L4 | L3 | WL | H3 | L3 |

Scatters on display: **0**

| Symbol | Reels | Ways | Table (tenths) | Win (cents) |
|---|---|---|---|---|
| L3 | 5 | 16 | 9 | 7200 |
| L4 | 3 | 2 | 1 | 100 |

Ways: `7300` · Scatter: `0`

**Total spin win (excluding feature): `7300` cents**

---

### 6.2 Feature

Specified by **event script**, not by seed. Each case says what landed on each respin. Your engine must produce the same total when fed that sequence — which requires the feature state machine to be testable without depending on RNG.

#### F01 — Normal cycle: lives reset whenever a new coin lands.

Bet: **100 cents** (units = 10). Empty grid, lives = 3.

| Respin | New coins (tenths) | Lives after | Locked |
|---|---|---|---|
| 1 | 3, 15 | 3 | 2 |
| 2 | none | 2 | 2 |
| 3 | ×2 | 3 | 3 |
| 4 | none | 2 | 3 |
| 5 | none | 1 | 3 |
| 6 | none | 0 | 3 |

**Calculation:** `cash` sum = `18` tenths · `mult` product = `2` · gross = `10 × (18 × 2)` = `360` cents

**Feature win: `360` cents**

---

#### F02 — Feature that pays zero. The grid starts empty and nothing lands in three respins.

Bet: **100 cents** (units = 10). Empty grid, lives = 3.

| Respin | New coins (tenths) | Lives after | Locked |
|---|---|---|---|
| 1 | none | 2 | 0 |
| 2 | none | 1 | 0 |
| 3 | none | 0 | 0 |

**Calculation:** `cash` sum = `0` tenths · `mult` product = `1` · gross = `10 × (0 × 1)` = `0` cents

**Feature win: `0` cents**

---

#### F03 — Full grid: pays the Grand.

Bet: **100 cents** (units = 10). Empty grid, lives = 3.

| Respin | New coins (tenths) | Lives after | Locked |
|---|---|---|---|
| 1 | 6, 6, 6, 6, 6 | 3 | 5 |
| 2 | 9, 9, 9, 9, 9 | 3 | 10 |
| 3 | 15, 15, 15, 15, ×2 | 3 | 15 |

Full grid → Grand of **5000** tenths, added **after** the multipliers.

**Calculation:** `cash` sum = `135` tenths · `mult` product = `2` · Grand = `5000` · gross = `10 × (135 × 2 + 5000)` = `52700` cents

**Feature win: `52700` cents**

---

#### F04 — **With minimum bet.** The bet flows through the whole feature: `cash`, Grand, and multipliers scale together.

Bet: **10 cents** (units = 1). Empty grid, lives = 3.

| Respin | New coins (tenths) | Lives after | Locked |
|---|---|---|---|
| 1 | 300, 300, 300, 300, 300 | 3 | 5 |
| 2 | 300, 300, 300, 300, 300 | 3 | 10 |
| 3 | 300, 300, 300, ×3, ×3 | 3 | 15 |

Full grid → Grand of **5000** tenths, added **after** the multipliers.

**Calculation:** `cash` sum = `3900` tenths · `mult` product = `9` · Grand = `5000` · gross = `1 × (3900 × 9 + 5000)` = `40100` cents

**Feature win: `40100` cents**

---

## 7. Deliverables

### E1 — Game configuration

Turn the raw data and the rules from Section 3 into a **declarative configuration** — a file, in whatever format you judge appropriate. Your engine must load it.

The criterion is this: **changing the configuration should change the game without recompiling logic.** That also applies to the rules in Section 3.6 — how many scatters trigger, how many lives, the coin chance per cell, the Grand value. Those numbers must come from configuration, not be hard-coded.

Briefly explain in the README the schema choices: what became a parameter, what stayed implicit, and why.

### E2 — Game engine

A deterministic engine that runs base-game spins and the full feature.

Requirements:

- **Implements the `Slots.Contracts` contract** (`ISlotEngineFactory` and `ISlotEngine`). The kit's `Slots.Engine` project is already wired into the simulator — implement the contract there.
- Deterministic and reproducible: same seed → same sequence of results, always.
- **All monetary arithmetic in integers**, as in Section 2.8, with the bet entering as a spin parameter.
- RNG injected and seeded, as in Section 2.7.
- Forced stops are already part of the contract (`SpinWithStops`). Feature scripts (cases F01–F04) are an internal test point of yours: design the feature state machine to be testable without RNG.
- Configuration validation on load — reel lengths, unknown symbols, missing entries, weights that don't sum.

**What we expect in the display fields.** `play` mode prints what your engine returns — the contract's text fields are the spin's interface with whoever is watching. In each `WinResult`, `Detail` should say how the amount was formed (e.g.: `"3 reels · 2 ways · table 5"`). In the feature, the expectation is **one `FeatureStep` per respin** — saying what landed and the state after it — plus a final step with the account closing and the grid attached in `Window`. Text format is free; the criterion is that a spin is readable in the simulator. For case `F01`, a good output looks like:

```
respin 1: 3, 15 → lives 3, locked 2
respin 2: none → lives 2, locked 2
respin 3: ×2 → lives 3, locked 3
respin 4: none → lives 2, locked 3
respin 5: none → lives 1, locked 3
respin 6: none → lives 0, locked 3
cash 18 × mult 2 · units 10        ← final step, with the grid in Window
┌──────┬──────┬──────┬──────┬──────┐
│  ·   │  3   │  ·   │  ·   │  ·   │
│  ·   │  ·   │  ×2  │  ·   │  ·   │
│  ·   │  ·   │  ·   │  15  │  ·   │
└──────┴──────┴──────┴──────┴──────┘
feature win: 360 cents
```

The frame and the last line are from the simulator — your engine only returns the steps and the grid.

### E3 — Tests

The eleven acceptance cases are the base of your suite — G01–G07 the simulator also checks; F01–F04 **only exist in your tests**. What else to test is your decision.

---

## 8. Acceptance criteria

- [ ] The eleven cases from Section 6 pass exactly.
- [ ] The engine implements the contract and the simulator's `dotnet run cases` passes.
- [ ] The simulator's `sim` and `play` modes work with your engine, with readable wins and feature in `play`.
- [ ] The engine loads configuration from a file and validates what it loaded.
- [ ] Two runs with the same seed produce identical results.
- [ ] No monetary value travels as floating point in the engine.
- [ ] The project runs with clear execution instructions in the README.

---

## 9. Live session

After delivery, we will schedule a 60-minute conversation. You will:

- run your own code with us;
- explain architecture and schema decisions.

Have the environment ready to run the project in that session.

---

## 10. Decisions already made

- The bet is a spin parameter, in cents, multiple of 10 and at least 10. You may reject bets that violate that in validation — you don't need to support a 37-cent bet or define a rounding policy.
- RTP is the same for any valid bet. Wins that don't scale in proportion to the bet indicate a problem in how the bet flows through the chain.
- Ways, scatter, and feature from the same spin **add**. There is no win cap: the spin win is the sum, with no ceiling.
- Scatter pays once per spin, by count, without multiplying by ways.
- The scatters that triggered the feature do **not** become coins or lock positions. The grid starts empty.
- A `mult` coin occupies a cell and counts toward filling the grid, like any other.
- Multipliers multiply each other; they do not add. And they apply to all feature `cash`, regardless of when they landed.
- Grand is **not multiplied**: multipliers apply only to `cash`, and Grand is added afterward. It scales with the bet like everything else.
- The feature cannot be triggered from within itself.
- There are no side bets, feature buy, gamble, or progressive jackpot.
- There is no player balance or session.

Good luck! And don't get stuck alone: any question — about the brief, the kit, or anything else — just email **felipe.mendes@ino-games.com**. Asking does not cost you points.
