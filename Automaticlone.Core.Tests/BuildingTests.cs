using System.Numerics;
using static Automaticlone.Core.Tests.TestHelpers;

namespace Automaticlone.Core.Tests;

public class BuildingTests
{
    private static readonly Vector3 Spot = new(5, 0, -5);

    private static BuildSite PlaceSite(World world, BuildingKind kind, Vector3 position)
    {
        Ok(world.Apply(new Actor(1), new PlaceBuilding(kind, position)));
        return world.Stations.OfType<BuildSite>().Single(s => s.Position == position);
    }

    private static void Bring(World world, int stationId, Item item, int count)
    {
        var carrier = new Actor(99);
        for (int i = 0; i < count; i++)
        {
            carrier.Carried = item;
            Ok(world.Apply(carrier, new Deposit(stationId)));
        }
    }

    [Fact]
    public void PlacingCreatesSiteAndThreeStonesMakeAFurnaceWithSameId()
    {
        var (world, _, _) = IronWorld();
        var site = PlaceSite(world, BuildingKind.Four, Spot);
        Assert.Equal((Item.Pierre, 3, 0), (site.Material, site.Needed, site.Delivered));

        Bring(world, site.Id, Item.Pierre, 2);
        Assert.IsType<BuildSite>(world.Find(site.Id));
        Bring(world, site.Id, Item.Pierre, 1);

        var furnace = Assert.IsType<Furnace>(world.Find(site.Id));
        Assert.Equal(Spot, furnace.Position);
        Assert.Equal(3, world.Stations.Count);
    }

    [Fact]
    public void WorkbenchSiteNeedsTwoIngots()
    {
        var (world, _, _) = IronWorld();
        var site = PlaceSite(world, BuildingKind.Etabli, Spot);
        Assert.Equal((Item.Lingot, 2), (site.Material, site.Needed));

        Bring(world, site.Id, Item.Lingot, 2);
        Assert.IsType<Workbench>(world.Find(site.Id));
    }

    [Fact]
    public void SiteRejectsWrongMaterialPickUpAndWork()
    {
        var (world, _, _) = IronWorld();
        var site = PlaceSite(world, BuildingKind.Four, Spot);
        var a = new Actor(1) { Carried = Item.Fer };
        Assert.Equal(ActionResult.WrongItem, world.Apply(a, new Deposit(site.Id)));
        Assert.Equal(Item.Fer, a.Carried);

        var b = new Actor(2);
        Assert.Equal(ActionResult.NothingToTake, world.Apply(b, new PickUp(site.Id)));
        Assert.Equal(ActionResult.CannotWorkHere, world.Apply(b, new StartWork(site.Id)));
    }

    [Fact]
    public void PlacementTooCloseIsBlocked()
    {
        var (world, iron, _) = IronWorld();   // fer en (0, 0, -20)
        var a = new Actor(1);
        Assert.Equal(ActionResult.PlacementBlocked,
            world.Apply(a, new PlaceBuilding(BuildingKind.Four, iron.Position + new Vector3(0, 0, 2.4f))));
        Ok(world.Apply(a, new PlaceBuilding(BuildingKind.Four, iron.Position + new Vector3(0, 0, 2.6f))));
    }

    [Fact]
    public void PlacingWhereABuildingAlreadyStandsFails()
    {
        var (world, _, _) = IronWorld();
        var site = PlaceSite(world, BuildingKind.Four, Spot);
        Bring(world, site.Id, Item.Pierre, 3);

        Assert.Equal(ActionResult.PlacementBlocked,
            world.Apply(new Actor(2), new PlaceBuilding(BuildingKind.Four, Spot)));
    }

    [Fact]
    public void SiteMaterialsSurviveLoopReset()
    {
        var (world, _, _) = IronWorld();
        var site = PlaceSite(world, BuildingKind.Four, Spot);
        Bring(world, site.Id, Item.Pierre, 2);

        world.ResetForNewLoop();
        Assert.Equal(2, Assert.IsType<BuildSite>(world.Find(site.Id)).Delivered);
    }

    [Fact]
    public void StoneToFinishedFurnaceFailsAndStaysInHand()
    {
        var (world, _, _) = IronWorld();
        var site = PlaceSite(world, BuildingKind.Four, Spot);
        Bring(world, site.Id, Item.Pierre, 3);

        var late = new Actor(2) { Carried = Item.Pierre };
        Assert.Equal(ActionResult.WrongItem, world.Apply(late, new Deposit(site.Id)));
        Assert.Equal(Item.Pierre, late.Carried);
    }

    [Fact]
    public void WorkbenchMakesGearFromTwoIngotsIn10Ticks()
    {
        var world = new World(new Rules());
        var bench = world.AddWorkbench(Spot);
        Bring(world, bench.Id, Item.Lingot, 2);
        var worker = new Actor(1);
        Ok(world.Apply(worker, new StartWork(bench.Id)));

        world.Run([worker], 599);
        Assert.Equal(0, bench.Gears);
        world.Run([worker], 1);
        Assert.Equal(1, bench.Gears);
        Assert.Equal(0, bench.Ingots);

        Ok(world.Apply(worker, new PickUp(bench.Id)));
        Assert.Equal(Item.Engrenage, worker.Carried);
    }

    [Fact]
    public void WorkbenchDoesNotWorkAlone()
    {
        var world = new World(new Rules());
        var bench = world.AddWorkbench(Spot);
        Bring(world, bench.Id, Item.Lingot, 2);

        world.Run([], 1200);
        Assert.Equal(0, bench.Gears);
    }

    [Fact]
    public void WorkbenchWorkerWaitsWithoutIngotsAndInputIsCapped()
    {
        var world = new World(new Rules());
        var bench = world.AddWorkbench(Spot);
        var worker = new Actor(1);
        Ok(world.Apply(worker, new StartWork(bench.Id)));
        world.Run([worker], 1);
        Assert.True(worker.IsWaiting);

        Bring(world, bench.Id, Item.Lingot, 4);
        var extra = new Actor(2) { Carried = Item.Lingot };
        Assert.Equal(ActionResult.StationFull, world.Apply(extra, new Deposit(bench.Id)));
        extra.Carried = Item.Fer;
        Assert.Equal(ActionResult.WrongItem, world.Apply(extra, new Deposit(bench.Id)));
    }

    [Fact]
    public void DeliveryCountsGearsOnly()
    {
        var (world, _, delivery) = IronWorld();
        var a = new Actor(1) { Carried = Item.Lingot };
        Assert.Equal(ActionResult.WrongItem, world.Apply(a, new Deposit(delivery.Id)));

        a.Carried = Item.Engrenage;
        Ok(world.Apply(a, new Deposit(delivery.Id)));
        Assert.Equal(1, delivery.GearsDelivered);
        Assert.Null(a.Carried);

        world.ResetForNewLoop();
        Assert.Equal(0, delivery.GearsDelivered);
    }
}
