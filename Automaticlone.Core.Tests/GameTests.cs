using System.Numerics;
using static Automaticlone.Core.Tests.TestHelpers;

namespace Automaticlone.Core.Tests;

public class GameTests
{
    // Vie courte (10 s = 600 pas) pour des tests rapides.
    private static readonly Rules ShortLife = new(LifeSeconds: 10);

    private sealed class TestGame
    {
        public TestGame(Rules? rules = null)
        {
            Game = new Game(rules ?? ShortLife, world =>
            {
                Iron = world.AddDeposit(Item.Fer, new Vector3(0, 0, -20));
                Furnace = world.AddFurnace(new Vector3(8, 0, -10));
                Delivery = world.AddDeliveryPoint(Vector3.Zero);
            });
        }

        public Game Game { get; }
        public ResourceDeposit Iron { get; private set; } = null!;
        public Furnace Furnace { get; private set; } = null!;
        public DeliveryPoint Delivery { get; private set; } = null!;
    }

    /// <summary>
    /// Joue la vie en cours jusqu'à sa fin (ou jusqu'au pas <paramref name="until"/>), en injectant les commandes
    /// du joueur aux pas indiqués. La pose du joueur vaut (pas, 0, 0). Renvoie les résultats par pas.
    /// </summary>
    private static Dictionary<int, ActionResult> PlayLife(Game g, int? until, params (int step, Command cmd)[] script)
    {
        var results = new Dictionary<int, ActionResult>();
        while (!g.IsLifeOver && (until == null || g.Step < until))
        {
            int step = g.Step;
            var cmds = script.Where(s => s.step == step).Select(s => s.cmd).ToList();
            g.SetPlayerPose(new Pose(new Vector3(step, 0, 0), 0, 0));
            var res = g.Tick(cmds);
            if (res.Count > 0) results[step] = res[^1];
        }
        return results;
    }

    [Fact]
    public void KeptLifeReplaysAsCloneWithSameCommandsAndPoses()
    {
        var t = new TestGame();
        var g = t.Game;
        t.Iron.Pile = 1;
        PlayLife(g, null, (100, new PickUp(t.Iron.Id)));

        var rec = g.FinishLife(keep: true);
        Assert.Equal(1, rec.LifeNumber);
        Assert.Equal(600, rec.Poses.Count);
        Assert.Equal([new TimedCommand(100, new PickUp(t.Iron.Id))], rec.Commands);

        var clone = Assert.Single(g.Clones);
        Assert.Equal(50f, clone.PoseAt(50).Position.X);
        Assert.Equal(599f, clone.PoseAt(5000).Position.X);
        Assert.Equal(2, g.LifeNumber);
        Assert.Equal(0, g.Step);

        t.Iron.Pile = 1;
        PlayLife(g, 101);
        Assert.Equal(Item.Fer, clone.Actor.Carried);
        Assert.Equal(0, t.Iron.Pile);
        Assert.False(clone.Actor.HasFailed);
    }

    [Fact]
    public void RejectedLifeAddsNoClone()
    {
        var t = new TestGame();
        PlayLife(t.Game, null);
        t.Game.FinishLife(keep: false);
        Assert.Empty(t.Game.Clones);
        Assert.Equal(2, t.Game.LifeNumber);
    }

    [Fact]
    public void PlayerFailedCommandsAreNotRecorded()
    {
        var t = new TestGame();
        var g = t.Game;
        t.Iron.Pile = 1;
        var results = PlayLife(g, null, (10, new Deposit(t.Iron.Id)), (20, new PickUp(t.Iron.Id)));
        Assert.Equal(ActionResult.HandsEmpty, results[10]);
        Assert.Equal(ActionResult.Ok, results[20]);

        var rec = g.FinishLife(keep: true);
        Assert.Single(rec.Commands);

        t.Iron.Pile = 1;
        PlayLife(g, null);
        Assert.False(g.Clones[0].Actor.HasFailed);
    }

    [Fact]
    public void CloneFailsWhenPlayerTookTheOreFirst()
    {
        var t = new TestGame();
        var g = t.Game;
        t.Iron.Pile = 1;
        PlayLife(g, null, (100, new PickUp(t.Iron.Id)));
        g.FinishLife(keep: true);

        t.Iron.Pile = 1;
        PlayLife(g, 101, (50, new PickUp(t.Iron.Id)));
        var clone = g.Clones[0].Actor;
        Assert.True(clone.HasFailed);
        Assert.Equal(100, clone.LastFailStep);
        Assert.Null(clone.Carried);
        Assert.Equal(Item.Fer, g.Player.Carried);
    }

    [Fact]
    public void FailedPickupCascadesIntoFailedDeposit()
    {
        var t = new TestGame();
        var g = t.Game;
        t.Iron.Pile = 1;
        PlayLife(g, null, (100, new PickUp(t.Iron.Id)), (200, new Deposit(t.Furnace.Id)));
        g.FinishLife(keep: true);

        t.Iron.Pile = 1;
        PlayLife(g, null, (50, new PickUp(t.Iron.Id)));
        var clone = g.Clones[0].Actor;
        Assert.True(clone.HasFailed);
        Assert.Equal(200, clone.LastFailStep);
        Assert.Equal(0, t.Furnace.Iron);
    }

    [Fact]
    public void OlderLifeWinsTheLastOreOnSameStep()
    {
        var t = new TestGame();
        var g = t.Game;
        t.Iron.Pile = 1;
        PlayLife(g, null, (100, new PickUp(t.Iron.Id)));
        g.FinishLife(keep: true);

        t.Iron.Pile = 2;                             // assez pour le clone 1 et le joueur
        PlayLife(g, null, (100, new PickUp(t.Iron.Id)));
        g.FinishLife(keep: true);

        t.Iron.Pile = 1;                             // un seul minerai pour les deux clones
        PlayLife(g, 101);
        Assert.Equal(Item.Fer, g.Clones[0].Actor.Carried);
        Assert.False(g.Clones[0].Actor.HasFailed);
        Assert.Null(g.Clones[1].Actor.Carried);
        Assert.True(g.Clones[1].Actor.HasFailed);
    }

    [Fact]
    public void DeletingWorkingCloneKeepsStationProgress()
    {
        var t = new TestGame();
        var g = t.Game;
        PlayLife(g, null, (0, new StartWork(t.Iron.Id)));
        g.FinishLife(keep: true);

        PlayLife(g, 300);
        Assert.Equal(300, t.Iron.ProgressSteps);
        Assert.True(g.DeleteClone(1));
        Assert.Empty(g.Clones);

        PlayLife(g, 400);
        Assert.Equal(300, t.Iron.ProgressSteps);
        Assert.False(g.DeleteClone(1));
    }

    [Fact]
    public void RejectedLifeKeepsBuildingsAndSiteMaterials()
    {
        var t = new TestGame();
        var g = t.Game;
        var spot = new Vector3(-8, 0, -10);
        g.Player.Carried = Item.Pierre;
        int siteId = g.World.Stations.Max(s => s.Id) + 1;
        PlayLife(g, null, (10, new PlaceBuilding(BuildingKind.Four, spot)), (20, new Deposit(siteId)));
        g.FinishLife(keep: false);

        var site = Assert.IsType<BuildSite>(g.World.Find(siteId));
        Assert.Equal(1, site.Delivered);
        Assert.Equal(spot, site.Position);
    }

    [Fact]
    public void ReplayedDeliveryToFinishedWorkbenchSiteFailsAndKeepsIngot()
    {
        var t = new TestGame();
        var g = t.Game;
        int siteId = g.World.Stations.Max(s => s.Id) + 1;
        Ok(g.World.Apply(new Actor(99), new PlaceBuilding(BuildingKind.Etabli, new Vector3(-8, 0, -10))));
        var helper = new Actor(98) { Carried = Item.Lingot };
        Ok(g.World.Apply(helper, new Deposit(siteId)));       // 1/2 lingots

        g.Player.Carried = Item.Lingot;
        PlayLife(g, null, (10, new Deposit(siteId)));          // le joueur termine l'établi
        Assert.IsType<Workbench>(g.World.Find(siteId));
        g.FinishLife(keep: true);

        var clone = g.Clones[0].Actor;
        clone.Carried = Item.Lingot;                           // comme s'il avait encore un lingot en main
        PlayLife(g, 11);
        Assert.True(clone.HasFailed);
        Assert.Equal(Item.Lingot, clone.Carried);
        Assert.Equal(0, ((Workbench)g.World.Find(siteId)!).Ingots);
    }

    [Fact]
    public void ResourceCanBePutBackOnItsDepositAndReplays()
    {
        var t = new TestGame();
        var g = t.Game;
        t.Iron.Pile = 1;
        var results = PlayLife(g, null, (10, new PickUp(t.Iron.Id)), (20, new Deposit(t.Iron.Id)));
        Assert.Equal(ActionResult.Ok, results[20]);
        Assert.Equal(1, t.Iron.Pile);
        Assert.Null(g.Player.Carried);
        g.FinishLife(keep: true);

        t.Iron.Pile = 1;
        PlayLife(g, null);
        Assert.False(g.Clones[0].Actor.HasFailed);
        Assert.Equal(1, t.Iron.Pile);
    }

    [Fact]
    public void NewLoopEmptiesFurnaceAndResetsActors()
    {
        var t = new TestGame();
        var g = t.Game;
        g.Player.Carried = Item.Fer;
        PlayLife(g, null, (10, new Deposit(t.Furnace.Id)));
        Assert.Equal(1, t.Furnace.Iron);
        g.FinishLife(keep: true);

        Assert.Equal(0, t.Furnace.Iron);
        Assert.Null(g.Player.Carried);
        Assert.Equal(2, g.Player.LifeNumber);
    }

    [Fact]
    public void GoalReachedWhenEnoughGearsDeliveredInOneLoop()
    {
        var t = new TestGame(ShortLife with { GoalGears = 1 });
        var g = t.Game;
        Assert.False(g.GoalReached);
        g.Player.Carried = Item.Engrenage;
        PlayLife(g, null, (10, new Deposit(t.Delivery.Id)));
        Assert.Equal(1, g.GearsDeliveredThisLoop);
        Assert.True(g.GoalReached);

        g.FinishLife(keep: false);
        Assert.Equal(0, g.GearsDeliveredThisLoop);
        Assert.False(g.GoalReached);
    }

    [Fact]
    public void TickDoesNothingAfterLifeIsOver()
    {
        var t = new TestGame();
        var g = t.Game;
        PlayLife(g, null);
        Assert.True(g.IsLifeOver);
        Assert.Equal(600, g.Step);

        g.Player.Carried = Item.Engrenage;
        Assert.Empty(g.Tick([new Deposit(t.Delivery.Id)]));
        Assert.Equal(600, g.Step);
        Assert.Equal(0, g.GearsDeliveredThisLoop);
    }
}
