using OrderingShipping.SharedKernel;

namespace OrderingShipping.Shipping.Application;

public sealed class DeliverShipmentHandler : ICommandHandler<DeliverShipment, bool>
{
    private readonly IShipmentRepository _shipments;
    private readonly DomainEventDispatcher _events;

    public DeliverShipmentHandler(IShipmentRepository shipments, DomainEventDispatcher events)
    {
        _shipments = shipments;
        _events = events;
    }

    public bool Handle(DeliverShipment command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var shipment = _shipments.Find(command.ShipmentId)
            ?? throw new KeyNotFoundException($"Shipment {command.ShipmentId} was not found.");

        shipment.Deliver();
        _shipments.Save(shipment);
        _events.Dispatch(shipment.PullDomainEvents());
        return true;
    }
}
