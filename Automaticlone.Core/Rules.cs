namespace Automaticlone.Core;

// =====================================================================================
//  RÉGLAGES D'ÉQUILIBRAGE
// -------------------------------------------------------------------------------------
//  Toutes les valeurs du prototype A (cahier des charges §9) sont ici, et nulle part
//  ailleurs : on les ajuste en un seul endroit après les premiers tests.
//
//  La simulation avance à pas fixe : 1 pas = 1 frame physique de Godot (60 par seconde).
//  Un « tick » du cahier des charges (le travail d'un personnage pendant 1 s) vaut donc
//  StepsPerSecond pas de travail.
// =====================================================================================

public sealed record Rules(
    int StepsPerSecond = 60,
    int LifeSeconds = 60,             // réserve d'oxygène
    int MineTicks = 20,               // extraire une ressource
    int ResourcesPerExtraction = 1,
    int PileLimit = 10,               // ressources au pied d'un gisement avant blocage (§3.6)
    int SmeltTicks = 5,               // fondre un minerai de fer en lingot
    int CoalBurnSeconds = 10,         // 1 charbon = 10 s de fonctionnement du four
    int GearTicks = 10,               // fabriquer un engrenage à l'établi
    int GearIngots = 2,               // lingots par engrenage
    int FurnaceStoneCost = 3,
    int WorkbenchIngotCost = 2,
    int FurnaceInputCapacity = 5,     // fers et charbons, chacun
    int FurnaceOutputCapacity = 5,    // lingots
    int WorkbenchInputCapacity = 4,   // lingots
    int WorkbenchOutputCapacity = 2,  // engrenages
    int GoalGears = 5,                // engrenages à livrer en une boucle
    float MinBuildSpacing = 2.5f,     // mètres entre deux stations
    float WalkSpeed = 4f,             // mètres par seconde
    float InteractRange = 3f,         // mètres
    float FailFlashSeconds = 1.5f)    // durée du flash rouge d'un échec
{
    /// <summary>Durée d'une vie, en pas de simulation.</summary>
    public int LifeSteps => LifeSeconds * StepsPerSecond;

    /// <summary>Nombre de pas de travail correspondant à un nombre de ticks.</summary>
    public int StepsFor(int ticks) => ticks * StepsPerSecond;
}
