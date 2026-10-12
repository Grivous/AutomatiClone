namespace Automaticlone.Core;

/// <summary>
/// Apparence d'un clone (§3.2, §3.6). Rouge = le script du clone est cassé ;
/// orange = problème de rythme (il attend).
/// </summary>
public enum GhostLook
{
    Normal,      // cyan
    TintOrange,  // a attendu au moins une fois dans la boucle
    TintRed,     // a échoué au moins une fois dans la boucle
    Waiting,     // attend en ce moment (orange vif)
    FailFlash,   // vient d'échouer (rouge vif)
}

public static class GhostLooks
{
    /// <summary>L'apparence à afficher, par priorité : flash rouge, attente, teinte rouge, teinte orange.</summary>
    public static GhostLook For(Actor actor, int step, Rules rules)
    {
        int flashSteps = (int)(rules.FailFlashSeconds * rules.StepsPerSecond);
        if (actor.LastFailStep is { } failed && step - failed < flashSteps) return GhostLook.FailFlash;
        if (actor.IsWaiting) return GhostLook.Waiting;
        if (actor.HasFailed) return GhostLook.TintRed;
        if (actor.HasWaited) return GhostLook.TintOrange;
        return GhostLook.Normal;
    }
}
