namespace Automaticlone.Core;

/// <summary>
/// Un personnage : le joueur de la vie en cours, ou un clone qui rejoue une vie passée.
/// Les deux passent par les mêmes règles du monde.
/// </summary>
public sealed class Actor
{
    public Actor(int lifeNumber) => LifeNumber = lifeNumber;

    /// <summary>Numéro de la vie : sert aussi d'ordre de priorité (les plus anciennes d'abord, §3.7).</summary>
    public int LifeNumber { get; }

    /// <summary>L'objet porté (un seul à la fois), ou <c>null</c>.</summary>
    public Item? Carried { get; set; }

    /// <summary>La station sur laquelle le personnage travaille, ou <c>null</c>.</summary>
    public int? WorkStationId { get; set; }

    /// <summary>En attente en ce moment (orange vif, §3.6) : son travail n'avance pas.</summary>
    public bool IsWaiting { get; set; }

    /// <summary>A été en attente au moins une fois dans cette boucle (teinte orange permanente).</summary>
    public bool HasWaited { get; set; }

    /// <summary>A échoué au moins une action dans cette boucle (teinte rouge permanente, §3.2).</summary>
    public bool HasFailed { get; set; }

    /// <summary>Pas du dernier échec (pour le flash rouge), ou <c>null</c>.</summary>
    public int? LastFailStep { get; set; }

    /// <summary>Remet le personnage à neuf pour une nouvelle boucle.</summary>
    public void ResetForNewLoop()
    {
        Carried = null;
        WorkStationId = null;
        IsWaiting = false;
        HasWaited = false;
        HasFailed = false;
        LastFailStep = null;
    }
}
