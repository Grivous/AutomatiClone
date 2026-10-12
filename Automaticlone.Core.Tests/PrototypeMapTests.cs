using System.Numerics;

namespace Automaticlone.Core.Tests;

public class PrototypeMapTests
{
    [Fact]
    public void ThreeDepositsAbout5SecondsWalkFromDelivery()
    {
        var world = new World(new Rules());
        PrototypeMap.Build(world);

        var delivery = Assert.Single(world.Stations.OfType<DeliveryPoint>());
        var deposits = world.Stations.OfType<ResourceDeposit>().ToList();
        Assert.Equal([Item.Fer, Item.Pierre, Item.Charbon], deposits.Select(d => d.Resource));
        foreach (var d in deposits)
            Assert.InRange(Vector3.Distance(d.Position, delivery.Position), 16f, 24f);
    }
}
