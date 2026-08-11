using OrderingShipping.Shipping.Application;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests.TestSupport;

internal sealed class FixedShipmentIdGenerator : IShipmentIdGenerator
{
    private readonly ShipmentId _shipmentId;

    public FixedShipmentIdGenerator(ShipmentId shipmentId)
    {
        _shipmentId = shipmentId;
    }

    public ShipmentId Next() => _shipmentId;
}
