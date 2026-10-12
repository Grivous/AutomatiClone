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
