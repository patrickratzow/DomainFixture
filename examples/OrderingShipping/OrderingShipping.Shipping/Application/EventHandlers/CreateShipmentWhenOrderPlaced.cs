using OrderingShipping.Orders.Domain;
using OrderingShipping.SharedKernel;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.Shipping.Application;

public sealed class CreateShipmentWhenOrderPlaced : IDomainEventHandler<OrderPlaced>
{
    private readonly IShipmentIdGenerator _ids;
    private readonly CreateShipmentHandler _handler;

    public CreateShipmentWhenOrderPlaced(
        IShipmentIdGenerator ids,
        CreateShipmentHandler handler)
    {
        _ids = ids;
        _handler = handler;
    }

    public void Handle(OrderPlaced domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var destination = DeliveryAddress.Create(
            domainEvent.ShipTo.Line1,
            domainEvent.ShipTo.City,
            domainEvent.ShipTo.PostalCode,
            domainEvent.ShipTo.CountryCode);

        _handler.Handle(new CreateShipment(
            _ids.Next(),
            domainEvent.OrderId.Value,
            destination));
    }
}
