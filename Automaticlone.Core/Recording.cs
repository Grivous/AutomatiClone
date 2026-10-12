using System.Numerics;

namespace Automaticlone.Core;

/// <summary>Position et orientation d'un personnage à un pas donné.</summary>
public readonly record struct Pose(Vector3 Position, float Yaw, float Pitch);

/// <summary>
/// L'enregistrement d'une vie (§7) : une pose par pas, et les commandes réussies avec leur pas.
/// </summary>
public sealed record LifeRecording(int LifeNumber, IReadOnlyList<Pose> Poses, IReadOnlyList<TimedCommand> Commands);

/// <summary>Un clone : une vie passée qui se rejoue, avec son propre personnage dans le monde.</summary>
public sealed class Clone
{
    internal Clone(LifeRecording recording)
    {
        Recording = recording;
        Actor = new Actor(recording.LifeNumber);
    }

    public LifeRecording Recording { get; }

    public Actor Actor { get; }

    /// <summary>Indice de la prochaine commande à rejouer.</summary>
    internal int NextCommand { get; set; }

    /// <summary>La pose au pas donné (la dernière si la vie enregistrée est plus courte).</summary>
    public Pose PoseAt(int step)
    {
        var poses = Recording.Poses;
        if (poses.Count == 0) return default;
        return poses[Math.Clamp(step, 0, poses.Count - 1)];
    }
}
