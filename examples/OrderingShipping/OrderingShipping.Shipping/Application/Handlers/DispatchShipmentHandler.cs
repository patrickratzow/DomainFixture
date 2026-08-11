using OrderingShipping.SharedKernel;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public sealed class DispatchShipmentHandler : ICommandHandler<DispatchShipment, bool>
{
    private readonly IShipmentRepository _shipments;
    private readonly DomainEventDispatcher _events;

    public DispatchShipmentHandler(IShipmentRepository shipments, DomainEventDispatcher events)
    {
        _shipments = shipments;
        _events = events;
    }

    public bool Handle(DispatchShipment command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var shipment = _shipments.Find(command.ShipmentId)
            ?? throw new KeyNotFoundException($"Shipment {command.ShipmentId} was not found.");

        shipment.Dispatch(command.TrackingNumber);
        _shipments.Save(shipment);
        _events.Dispatch(shipment.PullDomainEvents());
        return true;
    }
}
