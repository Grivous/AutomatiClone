using System.Numerics;

namespace Automaticlone.Core;

// =====================================================================================
//  STATIONS
// -------------------------------------------------------------------------------------
//  Tout ce avec quoi un personnage interagit : gisements, four, établi, chantiers,
//  point de livraison. Chaque type de station décide lui-même ce qu'on peut y prendre,
//  y déposer, et comment le travail y avance. Le monde (World) ne fait qu'aiguiller.
// =====================================================================================

public abstract class Station
{
    protected Station(int id, Vector3 position)
    {
        Id = id;
        Position = position;
    }

    /// <summary>Identifiant stable d'une boucle à l'autre (les commandes enregistrées le visent).</summary>
    public int Id { get; }

    public Vector3 Position { get; }

    /// <summary>Prendre un objet ici. Les mains sont vides quand cette méthode est appelée.</summary>
    internal virtual ActionResult Take(Actor actor, Rules rules) => ActionResult.NothingToTake;

    /// <summary>Déposer ici l'objet porté. Les mains sont pleines quand cette méthode est appelée.</summary>
    internal virtual ActionResult Give(Actor actor, Rules rules) => ActionResult.WrongItem;

    /// <summary>Peut-on travailler sur cette station ?</summary>
    internal virtual bool CanWork => false;

    /// <summary>Un pas de travail. <paramref name="workers"/> : les personnages qui y travaillent.</summary>
    internal virtual void Advance(IReadOnlyList<Actor> workers, Rules rules) { }

    /// <summary>Début d'une nouvelle boucle : ce qui n'est pas persistant revient à zéro (§4).</summary>
    internal virtual void ResetForNewLoop() { }

    /// <summary>Met les travailleurs en attente (orange, §3.6).</summary>
    protected static void MarkWaiting(IReadOnlyList<Actor> workers)
    {
        foreach (var w in workers)
        {
            w.IsWaiting = true;
            w.HasWaited = true;
        }
    }
}

/// <summary>Un gisement inépuisable (fer, pierre ou charbon). Les ressources minées tombent à son pied (§3.6).</summary>
public sealed class ResourceDeposit : Station
{
    internal ResourceDeposit(int id, Item resource, Vector3 position) : base(id, position) => Resource = resource;

    public Item Resource { get; }

    /// <summary>Ressources au sol, au pied du gisement.</summary>
    public int Pile { get; set; }

    /// <summary>Pas de travail accumulés vers la prochaine extraction.</summary>
    public int ProgressSteps { get; private set; }

    internal override bool CanWork => true;

    internal override ActionResult Take(Actor actor, Rules rules)
    {
        if (Pile == 0) return ActionResult.NothingToTake;
        Pile--;
        actor.Carried = Resource;
        return ActionResult.Ok;
    }

    internal override void Advance(IReadOnlyList<Actor> workers, Rules rules)
    {
        if (workers.Count == 0) return;
        if (Pile >= rules.PileLimit)
        {
            MarkWaiting(workers);
            return;
        }

        ProgressSteps += workers.Count;
        int cost = rules.StepsFor(rules.MineTicks);
        if (ProgressSteps >= cost)
        {
            ProgressSteps -= cost;
            Pile = Math.Min(rules.PileLimit, Pile + rules.ResourcesPerExtraction);
        }
    }

    internal override void ResetForNewLoop()
    {
        Pile = 0;
        ProgressSteps = 0;
    }
}

/// <summary>
/// Le four (§9) : on y met du fer et du charbon. Allumé, il fond tout seul à 1 tick par seconde,
/// et chaque personnage qui y travaille ajoute 1 tick par seconde. Une fois allumé, il brûle son
/// charbon jusqu'au bout, même à vide.
/// </summary>
public sealed class Furnace : Station
{
    internal Furnace(int id, Vector3 position) : base(id, position) { }

    public int Iron { get; private set; }
    public int Coal { get; private set; }
    public int Ingots { get; private set; }
    public int BurnStepsLeft { get; private set; }
    public int ProgressSteps { get; private set; }
    public bool IsLit => BurnStepsLeft > 0;

    internal override bool CanWork => true;

    internal override ActionResult Take(Actor actor, Rules rules)
    {
        if (Ingots == 0) return ActionResult.NothingToTake;
        Ingots--;
        actor.Carried = Item.Lingot;
        return ActionResult.Ok;
    }

    internal override ActionResult Give(Actor actor, Rules rules)
    {
        switch (actor.Carried)
        {
            case Item.Fer when Iron >= rules.FurnaceInputCapacity:
            case Item.Charbon when Coal >= rules.FurnaceInputCapacity:
                return ActionResult.StationFull;
            case Item.Fer:
                Iron++;
                break;
            case Item.Charbon:
                Coal++;
                break;
            default:
                return ActionResult.WrongItem;
        }
        actor.Carried = null;
        return ActionResult.Ok;
    }

    internal override void Advance(IReadOnlyList<Actor> workers, Rules rules)
    {
        // Allumage automatique dès qu'il y a du charbon.
        if (BurnStepsLeft == 0 && Coal > 0)
        {
            Coal--;
            BurnStepsLeft = rules.CoalBurnSeconds * rules.StepsPerSecond;
        }

        bool litThisStep = IsLit;
        if (litThisStep) BurnStepsLeft--; // brûle même à vide

        bool canSmelt = litThisStep && Iron > 0 && Ingots < rules.FurnaceOutputCapacity;
        if (!canSmelt)
        {
            MarkWaiting(workers);
            return;
        }

        ProgressSteps += 1 + workers.Count;
        int cost = rules.StepsFor(rules.SmeltTicks);
        if (ProgressSteps >= cost)
        {
            ProgressSteps -= cost;
            Iron--;
            Ingots++;
        }
    }

    internal override void ResetForNewLoop()
    {
        Iron = Coal = Ingots = BurnStepsLeft = ProgressSteps = 0;
    }
}

/// <summary>
/// Un chantier : il reçoit ses matériaux un par un, puis devient le bâtiment (même identifiant).
/// Ses matériaux persistent d'une boucle à l'autre (§4).
/// </summary>
public sealed class BuildSite : Station
{
    internal BuildSite(int id, BuildingKind kind, Vector3 position, Rules rules) : base(id, position)
    {
        Kind = kind;
        (Material, Needed) = kind switch
        {
            BuildingKind.Four => (Item.Pierre, rules.FurnaceStoneCost),
            BuildingKind.Etabli => (Item.Lingot, rules.WorkbenchIngotCost),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    public BuildingKind Kind { get; }
    public Item Material { get; }
    public int Needed { get; }
    public int Delivered { get; private set; }
    public bool IsComplete => Delivered >= Needed;

    internal override ActionResult Give(Actor actor, Rules rules)
    {
        if (actor.Carried != Material) return ActionResult.WrongItem;
        actor.Carried = null;
        Delivered++;
        return ActionResult.Ok;
    }

    // ResetForNewLoop : rien à faire, les matériaux apportés restent acquis.
}

/// <summary>L'établi : 2 lingots donnent 1 engrenage. Il ne travaille pas seul : seuls les personnages apportent des ticks.</summary>
public sealed class Workbench : Station
{
    internal Workbench(int id, Vector3 position) : base(id, position) { }

    public int Ingots { get; private set; }
    public int Gears { get; private set; }
    public int ProgressSteps { get; private set; }

    internal override bool CanWork => true;

    internal override ActionResult Take(Actor actor, Rules rules)
    {
        if (Gears == 0) return ActionResult.NothingToTake;
        Gears--;
        actor.Carried = Item.Engrenage;
        return ActionResult.Ok;
    }

    internal override ActionResult Give(Actor actor, Rules rules)
    {
        if (actor.Carried != Item.Lingot) return ActionResult.WrongItem;
        if (Ingots >= rules.WorkbenchInputCapacity) return ActionResult.StationFull;
        actor.Carried = null;
        Ingots++;
        return ActionResult.Ok;
    }

    internal override void Advance(IReadOnlyList<Actor> workers, Rules rules)
    {
        if (workers.Count == 0) return;
        if (Ingots < rules.GearIngots || Gears >= rules.WorkbenchOutputCapacity)
        {
            MarkWaiting(workers);
            return;
        }

        ProgressSteps += workers.Count;
        int cost = rules.StepsFor(rules.GearTicks);
        if (ProgressSteps >= cost)
        {
            ProgressSteps -= cost;
            Ingots -= rules.GearIngots;
            Gears++;
        }
    }

    internal override void ResetForNewLoop()
    {
        Ingots = Gears = ProgressSteps = 0;
    }
}

/// <summary>Le point de livraison : compte les engrenages livrés pendant la boucle.</summary>
public sealed class DeliveryPoint : Station
{
    internal DeliveryPoint(int id, Vector3 position) : base(id, position) { }

    public int GearsDelivered { get; private set; }

    internal override ActionResult Give(Actor actor, Rules rules)
    {
        if (actor.Carried != Item.Engrenage) return ActionResult.WrongItem;
        actor.Carried = null;
        GearsDelivered++;
        return ActionResult.Ok;
    }

    internal override void ResetForNewLoop() => GearsDelivered = 0;
}
