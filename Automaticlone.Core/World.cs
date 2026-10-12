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
    private readonly List<Vector3> _reserved = new();
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

    public Workbench AddWorkbench(Vector3 position) =>
        Add(new Workbench(_nextId++, position));

    /// <summary>Interdit de construire autour de ce point (ex. le point d'apparition, sinon on renaîtrait dans un bâtiment).</summary>
    public void ReserveArea(Vector3 position) => _reserved.Add(position);

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

            case Deposit(var id, var intoSite):
            {
                if (Find(id) is not { } station) return ActionResult.UnknownStation;
                if (actor.Carried == null) return ActionResult.HandsEmpty;
                if (intoSite && station is not BuildSite) return ActionResult.WrongItem; // chantier déjà terminé (§3.4)
                var result = station.Give(actor, Rules);
                if (station is BuildSite { IsComplete: true } site) CompleteSite(site);
                return result;
            }

            case PlaceBuilding(var kind, var position):
            {
                // Trop près d'une station (y compris un bâtiment déjà là) : échec (§3.4).
                if (_stations.Any(s => HorizontalDistance(s.Position, position) < Rules.MinBuildSpacing)
                    || _reserved.Any(r => HorizontalDistance(r, position) < Rules.MinBuildSpacing))
                    return ActionResult.PlacementBlocked;
                Add(new BuildSite(_nextId++, kind, position, Rules));
                return ActionResult.Ok;
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

    /// <summary>Le chantier terminé est remplacé, à la même place dans la liste, par son bâtiment (même identifiant).</summary>
    private void CompleteSite(BuildSite site)
    {
        Station building = site.Kind switch
        {
            BuildingKind.Four => new Furnace(site.Id, site.Position),
            BuildingKind.Etabli => new Workbench(site.Id, site.Position),
            _ => throw new ArgumentOutOfRangeException(nameof(site)),
        };
        _stations[_stations.IndexOf(site)] = building;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b) =>
        new Vector2(a.X - b.X, a.Z - b.Z).Length();

    private T Add<T>(T station) where T : Station
    {
        _stations.Add(station);
        return station;
    }
}
