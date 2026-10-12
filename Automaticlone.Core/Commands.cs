using System.Numerics;

namespace Automaticlone.Core;

// =====================================================================================
//  COMMANDES
// -------------------------------------------------------------------------------------
//  Tout ce qu'un personnage peut faire dans le monde. Le joueur les produit avec la
//  souris et le clavier ; un clone rejoue celles qui ont été enregistrées (§7).
//  Une commande vise une STATION (un lieu ou un conteneur), jamais un objet précis :
//  un clone ramasse « un minerai du tas », ce qui reste valable d'une boucle à l'autre.
// =====================================================================================

public abstract record Command;

/// <summary>Prendre un objet dans une station (tas d'un gisement, lingot du four…).</summary>
public sealed record PickUp(int StationId) : Command;

/// <summary>
/// Déposer l'objet porté dans une station (four, chantier, établi, livraison, ou son propre gisement).
/// <paramref name="IntoSite"/> : l'apport visait un CHANTIER au moment de l'enregistrement. Rejoué alors que
/// le chantier est déjà terminé, c'est un échec et l'objet reste en main (§3.4).
/// </summary>
public sealed record Deposit(int StationId, bool IntoSite = false) : Command;

/// <summary>Commencer à travailler sur une station (miner, aider au four, fabriquer).</summary>
public sealed record StartWork(int StationId) : Command;

/// <summary>Arrêter de travailler.</summary>
public sealed record StopWork : Command;

/// <summary>Poser un chantier de bâtiment à une position du sol.</summary>
public sealed record PlaceBuilding(BuildingKind Kind, Vector3 Position) : Command;

/// <summary>Une commande enregistrée, avec le pas de la vie où elle a eu lieu.</summary>
public sealed record TimedCommand(int Step, Command Command);
