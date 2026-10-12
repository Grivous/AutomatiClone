using Godot;
using Automaticlone.Core;

namespace Automaticlone.Game;

/// <summary>Conversions entre les types du Core (System.Numerics) et ceux de Godot, et textes des objets.</summary>
public static class Conv
{
    public static Vector3 ToGodot(this System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);

    public static System.Numerics.Vector3 ToCore(this Vector3 v) => new(v.X, v.Y, v.Z);

    public static string Name(Item item) => item switch
    {
        Item.Fer => "fer",
        Item.Pierre => "pierre",
        Item.Charbon => "charbon",
        Item.Lingot => "lingot",
        Item.Engrenage => "engrenage",
        _ => item.ToString(),
    };

    public static string Name(BuildingKind kind) => kind == BuildingKind.Four ? "four" : "établi";

    /// <summary>Message affiché au joueur quand une action échoue.</summary>
    public static string Message(ActionResult result) => result switch
    {
        ActionResult.UnknownStation => "Rien ici",
        ActionResult.HandsFull => "Mains pleines",
        ActionResult.HandsEmpty => "Mains vides",
        ActionResult.NothingToTake => "Rien à ramasser",
        ActionResult.WrongItem => "Cet objet ne va pas ici",
        ActionResult.StationFull => "C'est plein",
        ActionResult.CannotWorkHere => "On ne peut pas travailler ici",
        ActionResult.PlacementBlocked => "Trop près d'un autre bâtiment",
        _ => "",
    };
}
