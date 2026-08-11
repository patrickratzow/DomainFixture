using OrderingShipping.SharedKernel;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public sealed class CreateShipmentHandler : ICommandHandler<CreateShipment, ShipmentId>
{
    private readonly IShipmentRepository _shipments;
    private readonly DomainEventDispatcher _events;

    public CreateShipmentHandler(IShipmentRepository shipments, DomainEventDispatcher events)
    {
        _shipments = shipments;
        _events = events;
    }

    public ShipmentId Handle(CreateShipment command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_shipments.FindByOrder(command.OrderId) is not null)
        {
            throw new InvalidOperationException($"Order {command.OrderId} already has a shipment.");
        }

        var shipment = Shipment.Create(
            command.ShipmentId,
            command.OrderId,
            command.Destination);

        _shipments.Add(shipment);
        _events.Dispatch(shipment.PullDomainEvents());
        return shipment.Id;
    }
}
