namespace Slots.Engine.Tests;

public class HoldAndSpinStateTests {
    [Fact]
    public void NewCoin_ShouldResetLivesToThree()
    {
        // Arrange
        var state = new HoldAndSpinState(3);

        state.ApplyRespin(Array.Empty<int>());
        state.ApplyRespin(Array.Empty<int>());

        Assert.Equal(1, state.Lives);

        state.ApplyRespin(new[] { 5 });

        // Assert
        Assert.Equal(3, state.Lives);
        Assert.True(state.LockedCells[5]);
        Assert.Equal(1, state.LockedCount);
    }

    [Fact]
    public void ThreeEmptyRespins_ShouldFinishFeature()
    {
        var state = new HoldAndSpinState(3);

        state.ApplyRespin(Array.Empty<int>());
        state.ApplyRespin(Array.Empty<int>());
        state.ApplyRespin(Array.Empty<int>());

        Assert.Equal(0, state.Lives);
        Assert.True(state.IsFinished);
        Assert.Equal(3, state.RespinCount);
    }

    [Fact]
    public void LockedCell_ShouldNotBeAcceptedAgain()
    {
        var state = new HoldAndSpinState(3);

        state.ApplyRespin(new[] { 2 });

        Assert.Throws<ArgumentException>(() =>
            state.ApplyRespin(new[] { 2 }));
    }
}