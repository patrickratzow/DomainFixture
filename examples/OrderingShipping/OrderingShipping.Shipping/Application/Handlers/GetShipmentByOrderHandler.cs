using OrderingShipping.SharedKernel;

namespace OrderingShipping.Shipping.Application;

public sealed class GetShipmentByOrderHandler : IQueryHandler<GetShipmentByOrder, ShipmentDetails?>
{
    private readonly IShipmentRepository _shipments;

    public GetShipmentByOrderHandler(IShipmentRepository shipments)
    {
        _shipments = shipments;
    }

    public ShipmentDetails? Handle(GetShipmentByOrder query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var shipment = _shipments.FindByOrder(query.OrderId);

        return shipment is null
            ? null
            : new ShipmentDetails(
                shipment.Id,
                shipment.OrderId,
                shipment.Status,
                shipment.TrackingNumber?.Value);
    }
}
