using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public interface IShipmentIdGenerator
{
    ShipmentId Next();
}
