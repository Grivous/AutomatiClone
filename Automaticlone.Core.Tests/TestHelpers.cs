using System.Numerics;

namespace Automaticlone.Core.Tests;

/// <summary>Petits mondes et raccourcis pour les tests.</summary>
internal static class TestHelpers
{
    /// <summary>Règles de test : limite au pied des gisements réduite à 3 pour tester le blocage rapidement.</summary>
    public static readonly Rules SmallPile = new(PileLimit: 3);

    /// <summary>Monde avec un gisement de fer en (0, 0, -20) et un point de livraison en (0, 0, 0).</summary>
    public static (World world, ResourceDeposit iron, DeliveryPoint delivery) IronWorld(Rules? rules = null)
    {
        var world = new World(rules ?? new Rules());
        var iron = world.AddDeposit(Item.Fer, new Vector3(0, 0, -20));
        var delivery = world.AddDeliveryPoint(Vector3.Zero);
        return (world, iron, delivery);
    }

    /// <summary>Fait avancer le travail du monde de <paramref name="steps"/> pas.</summary>
    public static void Run(this World world, IReadOnlyList<Actor> actors, int steps)
    {
        for (int i = 0; i < steps; i++) world.Advance(actors);
    }

    public static void Ok(ActionResult result) => Assert.Equal(ActionResult.Ok, result);
}
