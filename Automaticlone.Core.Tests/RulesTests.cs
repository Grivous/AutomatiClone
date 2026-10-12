namespace Automaticlone.Core.Tests;

public class RulesTests
{
    [Fact]
    public void DefaultsMatchSpecPrototypeA()
    {
        var r = new Rules();
        Assert.Equal(3600, r.LifeSteps);             // 60 s à 60 pas/s
        Assert.Equal(1200, r.StepsFor(r.MineTicks)); // 20 ticks
        Assert.Equal(10, r.PileLimit);
        Assert.Equal(5, r.SmeltTicks);
        Assert.Equal(10, r.CoalBurnSeconds);
        Assert.Equal(3, r.FurnaceStoneCost);
        Assert.Equal(2, r.WorkbenchIngotCost);
        Assert.Equal((2, 10), (r.GearIngots, r.GearTicks));
        Assert.Equal(5, r.GoalGears);
    }
}
