namespace Automaticlone.Core;

/// <summary>Les objets transportables du prototype A.</summary>
public enum Item { Fer, Pierre, Charbon, Lingot, Engrenage }

/// <summary>Les bâtiments que le joueur peut construire.</summary>
public enum BuildingKind { Four, Etabli }

/// <summary>Résultat d'une action. Tout ce qui n'est pas <c>Ok</c> est un échec (un clone passe en rouge, §3.2).</summary>
public enum ActionResult
{
    Ok,
    UnknownStation,    // la station visée n'existe pas
    HandsFull,         // on porte déjà un objet (capacité : 1)
    HandsEmpty,        // rien à déposer
    NothingToTake,     // rien à ramasser ici
    WrongItem,         // cette station n'accepte pas l'objet porté
    StationFull,       // la station a atteint sa capacité
    CannotWorkHere,    // on ne peut pas travailler sur cette station
    PlacementBlocked,  // trop près d'une autre station
}
