namespace Slots.Engine;

public class HoldAndSpinState {
    private readonly int _initialLives;

    public int Lives { get; private set; }

    public HoldAndSpinState(int initialLives)
    {
        if (initialLives <= 0)
            throw new ArgumentOutOfRangeException(nameof(initialLives));

        _initialLives = initialLives;
        Lives = initialLives;
    }

    public int RespinCount { get; private set; }

    public bool[] LockedCells { get; } = new bool[15];

    public int LockedCount => LockedCells.Count(locked => locked);

    public bool IsFinished => Lives == 0 || LockedCount == LockedCells.Length;

    public void ApplyRespin(IReadOnlyList<int> newCoinPositions) {
        if (IsFinished) {
            throw new InvalidOperationException("Feature already finished.");
        }

        foreach (var position in newCoinPositions) {
            if (position < 0 || position >= LockedCells.Length)
                throw new ArgumentException("Invalid cell position.");

            if (LockedCells[position]) {
                throw new ArgumentException("Cell already locked.");
            }
        }

        if (newCoinPositions.Distinct().Count() != newCoinPositions.Count) {
            throw new ArgumentException("Duplicate cell position.");
        }

        foreach (var position in newCoinPositions)
            LockedCells[position] = true;

        RespinCount++;

        if (newCoinPositions.Count > 0)
            Lives = _initialLives;
        else
            Lives--;
    }
    
}