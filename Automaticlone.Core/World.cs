using System.Numerics;

namespace Automaticlone.Core;

// =====================================================================================
//  LE MONDE SIMULÉ
// -------------------------------------------------------------------------------------
//  La liste des stations et les règles d'action. Le joueur et les clones passent par
//  les MÊMES méthodes : Apply (une commande) et Advance (un pas de travail).
//  Ce code ne dépend pas de Godot : il se teste avec `dotnet test`.
// =====================================================================================

public sealed class World
{
    private readonly List<Station> _stations = new();
    private int _nextId = 1; // ne revient jamais en arrière : les identifiants restent stables

    public World(Rules rules) => Rules = rules;

    public Rules Rules { get; }

    public IReadOnlyList<Station> Stations => _stations;

    public ResourceDeposit AddDeposit(Item resource, Vector3 position) =>
        Add(new ResourceDeposit(_nextId++, resource, position));

    public DeliveryPoint AddDeliveryPoint(Vector3 position) =>
        Add(new DeliveryPoint(_nextId++, position));

    public Furnace AddFurnace(Vector3 position) =>
        Add(new Furnace(_nextId++, position));

    public Station? Find(int id) => _stations.Find(s => s.Id == id);

    /// <summary>Applique une commande d'un personnage. Un échec ne change jamais l'objet porté.</summary>
    public ActionResult Apply(Actor actor, Command command)
    {
        switch (command)
        {
            case StopWork:
                actor.WorkStationId = null;
                return ActionResult.Ok;

            case PickUp(var id):
            {
                if (Find(id) is not { } station) return ActionResult.UnknownStation;
                if (actor.Carried != null) return ActionResult.HandsFull;
                return station.Take(actor, Rules);
            }

            case Deposit(var id):
            {
                if (Find(id) is not { } station) return ActionResult.UnknownStation;
                if (actor.Carried == null) return ActionResult.HandsEmpty;
                return station.Give(actor, Rules);
            }

            case StartWork(var id):
            {
                if (Find(id) is not { } station) return ActionResult.UnknownStation;
                if (!station.CanWork) return ActionResult.CannotWorkHere;
                actor.WorkStationId = id;
                return ActionResult.Ok;
            }

            default:
                throw new ArgumentException($"Commande inconnue : {command}", nameof(command));
        }
    }

    /// <summary>Un pas de travail pour toutes les stations.</summary>
    public void Advance(IReadOnlyList<Actor> actors)
    {
        foreach (var a in actors) a.IsWaiting = false;

        foreach (var station in _stations)
        {
            var workers = actors.Where(a => a.WorkStationId == station.Id).ToList();
            station.Advance(workers, Rules);
        }
    }

    /// <summary>Début d'une nouvelle boucle : ressources et contenus remis à zéro, constructions conservées (§4).</summary>
    public void ResetForNewLoop()
    {
        foreach (var s in _stations) s.ResetForNewLoop();
    }

    private T Add<T>(T station) where T : Station
    {
        _stations.Add(station);
        return station;
    }
}
