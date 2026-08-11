using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public sealed class InMemoryShipmentRepository : IShipmentRepository
{
    private readonly Dictionary<ShipmentId, Shipment> _shipments = new();

    public void Add(Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        _shipments.Add(shipment.Id, shipment);
    }

    public Shipment? Find(ShipmentId shipmentId) =>
        _shipments.TryGetValue(shipmentId, out var shipment) ? shipment : null;

    public Shipment? FindByOrder(Guid orderId) =>
        _shipments.Values.SingleOrDefault(shipment => shipment.OrderId == orderId);

    public void Save(Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        _shipments[shipment.Id] = shipment;
    }
}
