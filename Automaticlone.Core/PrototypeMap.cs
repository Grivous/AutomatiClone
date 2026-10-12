using System.Numerics;

namespace Automaticlone.Core;

/// <summary>
/// La carte du prototype A (§9) : le joueur apparaît à l'origine, regarde vers -Z.
/// La livraison est juste devant ; les trois gisements sont à environ 19 m (5 s de marche),
/// avec de la place entre les deux pour construire fours et établis.
/// </summary>
public static class PrototypeMap
{
    public static readonly Vector3 Spawn = Vector3.Zero;

    public static void Build(World world)
    {
        world.AddDeposit(Item.Fer, new Vector3(-8, 0, -20));
        world.AddDeposit(Item.Pierre, new Vector3(0, 0, -22));
        world.AddDeposit(Item.Charbon, new Vector3(8, 0, -20));
        world.AddDeliveryPoint(new Vector3(0, 0, -3));
    }
}
