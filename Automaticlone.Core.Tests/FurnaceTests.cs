using System.Numerics;
using static Automaticlone.Core.Tests.TestHelpers;

namespace Automaticlone.Core.Tests;

public class FurnaceTests
{
    private static (World world, Furnace furnace) FurnaceWorld(Rules? rules = null)
    {
        var world = new World(rules ?? new Rules());
        var furnace = world.AddFurnace(new Vector3(0, 0, -5));
        return (world, furnace);
    }

    /// <summary>Dépose <paramref name="count"/> objets dans le four, comme le ferait un porteur.</summary>
    private static void Feed(World world, Furnace furnace, Item item, int count)
    {
        var carrier = new Actor(99);
        for (int i = 0; i < count; i++)
        {
            carrier.Carried = item;
            Ok(world.Apply(carrier, new Deposit(furnace.Id)));
        }
    }

    [Fact]
    public void LitFurnaceSmeltsAloneOneIngotEvery5Seconds()
    {
        var (world, furnace) = FurnaceWorld();
        Feed(world, furnace, Item.Charbon, 1);
        Feed(world, furnace, Item.Fer, 3);

        world.Run([], 299);
        Assert.Equal(0, furnace.Ingots);
        world.Run([], 301);
        Assert.Equal(2, furnace.Ingots);
        Assert.Equal(1, furnace.Iron);
    }

    [Fact]
    public void HelperDoublesSmeltingSpeed()
    {
        var (world, furnace) = FurnaceWorld();
        Feed(world, furnace, Item.Charbon, 1);
        Feed(world, furnace, Item.Fer, 3);
        var helper = new Actor(1);
        Ok(world.Apply(helper, new StartWork(furnace.Id)));

        world.Run([helper], 300);
        Assert.Equal(2, furnace.Ingots);
        Assert.False(helper.IsWaiting);
    }

    [Fact]
    public void CoalBurnsEvenWithoutIron()
    {
        var (world, furnace) = FurnaceWorld();
        Feed(world, furnace, Item.Charbon, 1);

        world.Run([], 1);
        Assert.True(furnace.IsLit);
        Assert.Equal(0, furnace.Coal);
        world.Run([], 599);
        Assert.False(furnace.IsLit);
    }

    [Fact]
    public void NextCoalIsLitAutomatically()
    {
        var (world, furnace) = FurnaceWorld();
        Feed(world, furnace, Item.Charbon, 2);

        world.Run([], 601);
        Assert.Equal(0, furnace.Coal);
        Assert.True(furnace.IsLit);
    }

    [Fact]
    public void WorkerWaitsWhenFurnaceUnlitOrEmpty()
    {
        var (world, furnace) = FurnaceWorld();
        Feed(world, furnace, Item.Fer, 1);
        var helper = new Actor(1);
        Ok(world.Apply(helper, new StartWork(furnace.Id)));

        world.Run([helper], 1);                     // pas de charbon
        Assert.True(helper.IsWaiting);
        Assert.True(helper.HasWaited);
        Assert.Equal(0, furnace.ProgressSteps);
    }

    [Fact]
    public void DepositRejectsWrongItemAndFullInput()
    {
        var (world, furnace) = FurnaceWorld();
        var a = new Actor(1) { Carried = Item.Pierre };
        Assert.Equal(ActionResult.WrongItem, world.Apply(a, new Deposit(furnace.Id)));
        Assert.Equal(Item.Pierre, a.Carried);

        Feed(world, furnace, Item.Fer, 5);
        a.Carried = Item.Fer;
        Assert.Equal(ActionResult.StationFull, world.Apply(a, new Deposit(furnace.Id)));
        Assert.Equal(Item.Fer, a.Carried);
        Assert.Equal(5, furnace.Iron);
    }

    [Fact]
    public void PickUpTakesAnIngot()
    {
        var (world, furnace) = FurnaceWorld();
        var a = new Actor(1);
        Assert.Equal(ActionResult.NothingToTake, world.Apply(a, new PickUp(furnace.Id)));

        Feed(world, furnace, Item.Charbon, 1);
        Feed(world, furnace, Item.Fer, 1);
        world.Run([], 300);
        Ok(world.Apply(a, new PickUp(furnace.Id)));
        Assert.Equal(Item.Lingot, a.Carried);
        Assert.Equal(0, furnace.Ingots);
    }

    [Fact]
    public void FullOutputStopsSmeltingButCoalStillBurns()
    {
        var (world, furnace) = FurnaceWorld(new Rules(FurnaceOutputCapacity: 1));
        Feed(world, furnace, Item.Charbon, 1);
        Feed(world, furnace, Item.Fer, 3);

        world.Run([], 600);
        Assert.Equal(1, furnace.Ingots);
        Assert.Equal(2, furnace.Iron);
        Assert.False(furnace.IsLit);
    }

    [Fact]
    public void ResetForNewLoopEmptiesFurnace()
    {
        var (world, furnace) = FurnaceWorld();
        Feed(world, furnace, Item.Charbon, 2);
        Feed(world, furnace, Item.Fer, 2);
        world.Run([], 400);

        world.ResetForNewLoop();
        Assert.Equal((0, 0, 0, 0, 0), (furnace.Iron, furnace.Coal, furnace.Ingots, furnace.BurnStepsLeft, furnace.ProgressSteps));
    }
}
