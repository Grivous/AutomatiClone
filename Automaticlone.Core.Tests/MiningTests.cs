using static Automaticlone.Core.Tests.TestHelpers;

namespace Automaticlone.Core.Tests;

public class MiningTests
{
    [Fact]
    public void OneMinerProducesOneOreAfter20Seconds()
    {
        var (world, iron, _) = IronWorld();
        var miner = new Actor(1);
        Ok(world.Apply(miner, new StartWork(iron.Id)));

        world.Run([miner], 1199);
        Assert.Equal(0, iron.Pile);
        world.Run([miner], 1);
        Assert.Equal(1, iron.Pile);
    }

    [Fact]
    public void TwoMinersStackTicks()
    {
        var (world, iron, _) = IronWorld();
        var a = new Actor(1);
        var b = new Actor(2);
        Ok(world.Apply(a, new StartWork(iron.Id)));
        Ok(world.Apply(b, new StartWork(iron.Id)));

        world.Run([a, b], 599);
        Assert.Equal(0, iron.Pile);
        world.Run([a, b], 1);
        Assert.Equal(1, iron.Pile);
    }

    [Fact]
    public void ProgressIsKeptWhenMinerLeaves()
    {
        var (world, iron, _) = IronWorld();
        var a = new Actor(1);
        var b = new Actor(2);
        Ok(world.Apply(a, new StartWork(iron.Id)));
        world.Run([a, b], 720);                     // 12 ticks sur 20
        Ok(world.Apply(a, new StopWork()));
        Assert.Null(a.WorkStationId);

        Ok(world.Apply(b, new StartWork(iron.Id)));
        world.Run([a, b], 479);
        Assert.Equal(0, iron.Pile);
        world.Run([a, b], 1);                       // les 8 ticks restants
        Assert.Equal(1, iron.Pile);
    }

    [Fact]
    public void PickUpTakesFromPileAndFailsWhenEmptyOrHandsFull()
    {
        var (world, iron, _) = IronWorld();
        iron.Pile = 1;
        var a = new Actor(1);
        var b = new Actor(2);

        Ok(world.Apply(a, new PickUp(iron.Id)));
        Assert.Equal(Item.Fer, a.Carried);
        Assert.Equal(0, iron.Pile);

        Assert.Equal(ActionResult.HandsFull, world.Apply(a, new PickUp(iron.Id)));
        Assert.Equal(ActionResult.NothingToTake, world.Apply(b, new PickUp(iron.Id)));
        Assert.Null(b.Carried);
    }

    [Fact]
    public void DepositOnDepositOrWithEmptyHandsOrUnknownStationFails()
    {
        var (world, iron, delivery) = IronWorld();
        var a = new Actor(1);
        Assert.Equal(ActionResult.HandsEmpty, world.Apply(a, new Deposit(iron.Id)));
        Assert.Equal(ActionResult.UnknownStation, world.Apply(a, new PickUp(999)));

        a.Carried = Item.Pierre;                     // pas la ressource de ce gisement
        Assert.Equal(ActionResult.WrongItem, world.Apply(a, new Deposit(iron.Id)));
        Assert.Equal(Item.Pierre, a.Carried);
        Assert.Equal(ActionResult.CannotWorkHere, world.Apply(a, new StartWork(delivery.Id)));
        Assert.Null(a.WorkStationId);
    }

    [Fact]
    public void PuttingBackOnFullPileFails()
    {
        var (world, iron, _) = IronWorld(SmallPile);
        iron.Pile = 3;
        var a = new Actor(1) { Carried = Item.Fer };
        Assert.Equal(ActionResult.StationFull, world.Apply(a, new Deposit(iron.Id)));
        Assert.Equal(Item.Fer, a.Carried);
    }

    [Fact]
    public void MinerBlocksAtPileLimit()
    {
        var (world, iron, _) = IronWorld(SmallPile);
        var miner = new Actor(1);
        Ok(world.Apply(miner, new StartWork(iron.Id)));

        world.Run([miner], 3 * 1200);
        Assert.Equal(3, iron.Pile);
        world.Run([miner], 1);
        Assert.True(miner.IsWaiting);
        Assert.True(miner.HasWaited);

        world.Run([miner], 1200);
        Assert.Equal(3, iron.Pile);
    }

    [Fact]
    public void BlockedMinerResumesWhenPileFreed()
    {
        var (world, iron, _) = IronWorld(SmallPile);
        var miner = new Actor(1);
        var carrier = new Actor(2);
        Ok(world.Apply(miner, new StartWork(iron.Id)));
        world.Run([miner, carrier], 3 * 1200 + 10);
        Assert.True(miner.IsWaiting);

        Ok(world.Apply(carrier, new PickUp(iron.Id)));
        world.Run([miner, carrier], 1);
        Assert.False(miner.IsWaiting);
        Assert.True(miner.HasWaited);

        world.Run([miner, carrier], 1199);
        Assert.Equal(3, iron.Pile);
    }

    [Fact]
    public void ResetForNewLoopEmptiesPilesAndProgress()
    {
        var (world, iron, _) = IronWorld();
        var miner = new Actor(1);
        Ok(world.Apply(miner, new StartWork(iron.Id)));
        world.Run([miner], 1800);
        Assert.Equal(1, iron.Pile);

        world.ResetForNewLoop();
        Assert.Equal(0, iron.Pile);
        Assert.Equal(0, iron.ProgressSteps);
    }
}
