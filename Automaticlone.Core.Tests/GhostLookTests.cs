namespace Automaticlone.Core.Tests;

public class GhostLookTests
{
    private static readonly Rules R = new();

    [Fact]
    public void NormalWhenNothingHappened() =>
        Assert.Equal(GhostLook.Normal, GhostLooks.For(new Actor(1), 100, R));

    [Fact]
    public void OrangeTintAfterWaiting() =>
        Assert.Equal(GhostLook.TintOrange, GhostLooks.For(new Actor(1) { HasWaited = true }, 100, R));

    [Fact]
    public void RedTintBeatsOrangeTint() =>
        Assert.Equal(GhostLook.TintRed,
            GhostLooks.For(new Actor(1) { HasWaited = true, HasFailed = true, LastFailStep = 0 }, 1000, R));

    [Fact]
    public void WaitingBeatsTints() =>
        Assert.Equal(GhostLook.Waiting,
            GhostLooks.For(new Actor(1) { IsWaiting = true, HasWaited = true, HasFailed = true, LastFailStep = 0 }, 1000, R));

    [Fact]
    public void FailFlashLasts90StepsThenRedTint()
    {
        var a = new Actor(1) { IsWaiting = true, HasFailed = true, LastFailStep = 100 };
        Assert.Equal(GhostLook.FailFlash, GhostLooks.For(a, 100, R));
        Assert.Equal(GhostLook.FailFlash, GhostLooks.For(a, 189, R));
        a.IsWaiting = false;
        Assert.Equal(GhostLook.TintRed, GhostLooks.For(a, 190, R));
    }
}
