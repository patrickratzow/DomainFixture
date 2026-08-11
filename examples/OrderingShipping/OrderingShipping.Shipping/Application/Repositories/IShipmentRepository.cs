using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public interface IShipmentRepository
{
    void Add(Shipment shipment);
    Shipment? Find(ShipmentId shipmentId);
    Shipment? FindByOrder(Guid orderId);
    void Save(Shipment shipment);
}
