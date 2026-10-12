namespace Automaticlone.Core;

// =====================================================================================
//  LA BOUCLE DE VIE
// -------------------------------------------------------------------------------------
//  Une vie dure Rules.LifeSteps pas. À chaque pas (Tick) :
//    1. les clones rejouent leurs commandes, des vies les plus anciennes aux plus
//       récentes (§3.7) ; un échec les marque en rouge (§3.2) ;
//    2. le joueur agit en dernier ; seules ses commandes RÉUSSIES sont enregistrées
//       (un clic raté n'est pas une action à rejouer) ;
//    3. le travail avance (minage, four, établi).
//  À la fin de la vie, le joueur garde ou rejette sa vie (§3.3). Une vie rejetée ne
//  devient pas un clone, mais ses effets sur le monde restent.
// =====================================================================================

public sealed class Game
{
    private readonly List<Clone> _clones = new();
    private readonly List<Pose> _poses = new();
    private readonly List<TimedCommand> _commands = new();
    private Pose _playerPose;

    public Game(Rules rules, Action<World> buildMap)
    {
        Rules = rules;
        World = new World(rules);
        buildMap(World);
        Player = new Actor(LifeNumber);
    }

    public Rules Rules { get; }

    public World World { get; }

    /// <summary>Numéro de la vie en cours (commence à 1).</summary>
    public int LifeNumber { get; private set; } = 1;

    /// <summary>Pas courant dans la vie en cours.</summary>
    public int Step { get; private set; }

    public bool IsLifeOver => Step >= Rules.LifeSteps;

    /// <summary>Le personnage du joueur pour la vie en cours.</summary>
    public Actor Player { get; private set; }

    /// <summary>Les clones, triés par numéro de vie croissant (ordre de priorité).</summary>
    public IReadOnlyList<Clone> Clones => _clones;

    public int GearsDeliveredThisLoop => World.Stations.OfType<DeliveryPoint>().Sum(d => d.GearsDelivered);

    public bool GoalReached => GearsDeliveredThisLoop >= Rules.GoalGears;

    /// <summary>Pose du joueur pour le prochain pas (fournie par Godot).</summary>
    public void SetPlayerPose(Pose pose) => _playerPose = pose;

    /// <summary>Avance d'un pas. Renvoie le résultat de chaque commande du joueur, dans l'ordre.</summary>
    public IReadOnlyList<ActionResult> Tick(IReadOnlyList<Command> playerCommands)
    {
        if (IsLifeOver) return Array.Empty<ActionResult>();

        foreach (var clone in _clones) ReplayStep(clone);

        var results = new List<ActionResult>(playerCommands.Count);
        foreach (var command in playerCommands)
        {
            // Un apport à un chantier est enregistré comme tel : rejoué sur le bâtiment terminé, il échouera (§3.4).
            var recorded = command is Deposit d && World.Find(d.StationId) is BuildSite ? d with { IntoSite = true } : command;
            var result = World.Apply(Player, command);
            results.Add(result);
            if (result == ActionResult.Ok) _commands.Add(new TimedCommand(Step, recorded));
        }
        _poses.Add(_playerPose);

        var actors = _clones.Select(c => c.Actor).Append(Player).ToList();
        World.Advance(actors);

        Step++;
        return results;
    }

    /// <summary>Termine la vie en cours : la garde comme clone ou la rejette, puis commence la suivante.</summary>
    public LifeRecording FinishLife(bool keep)
    {
        var recording = new LifeRecording(LifeNumber, _poses.ToList(), _commands.ToList());
        if (keep) _clones.Add(new Clone(recording));

        _poses.Clear();
        _commands.Clear();
        World.ResetForNewLoop();
        foreach (var clone in _clones)
        {
            clone.Actor.ResetForNewLoop();
            clone.NextCommand = 0;
        }

        Step = 0;
        LifeNumber++;
        Player = new Actor(LifeNumber);
        return recording;
    }

    /// <summary>Supprime un clone tout de suite. Ses effets sur le monde restent (§3.3).</summary>
    public bool DeleteClone(int lifeNumber) => _clones.RemoveAll(c => c.Recording.LifeNumber == lifeNumber) > 0;

    private void ReplayStep(Clone clone)
    {
        var commands = clone.Recording.Commands;
        while (clone.NextCommand < commands.Count && commands[clone.NextCommand].Step == Step)
        {
            var result = World.Apply(clone.Actor, commands[clone.NextCommand].Command);
            if (result != ActionResult.Ok)
            {
                clone.Actor.HasFailed = true;
                clone.Actor.LastFailStep = Step;
            }
            clone.NextCommand++;
        }
    }
}
