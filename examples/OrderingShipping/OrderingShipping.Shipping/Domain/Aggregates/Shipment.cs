using OrderingShipping.SharedKernel;

namespace OrderingShipping.Shipping.Domain;

public sealed class Shipment : AggregateRoot<ShipmentId>
{
    private Shipment(
        ShipmentId id,
        Guid orderId,
        DeliveryAddress destination)
        : base(id)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("An originating order id is required.", nameof(orderId));
        }

        ArgumentNullException.ThrowIfNull(destination);
        OrderId = orderId;
        Destination = destination;
        Status = ShipmentStatus.Pending;
    }

    public Guid OrderId { get; }
    public DeliveryAddress Destination { get; }
    public ShipmentStatus Status { get; private set; }
    public TrackingNumber? TrackingNumber { get; private set; }

    public static Shipment Create(
        ShipmentId id,
        Guid orderId,
        DeliveryAddress destination)
    {
        var shipment = new Shipment(id, orderId, destination);
        shipment.Raise(new ShipmentCreated(id, orderId));
        return shipment;
    }

    public void Dispatch(TrackingNumber trackingNumber)
    {
        if (Status != ShipmentStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending shipment can be dispatched.");
        }

        TrackingNumber = trackingNumber;
        Status = ShipmentStatus.Dispatched;
        Raise(new ShipmentDispatched(Id, OrderId, trackingNumber));
    }

    public void Deliver()
    {
        if (Status != ShipmentStatus.Dispatched)
        {
            throw new InvalidOperationException("Only a dispatched shipment can be delivered.");
        }

        Status = ShipmentStatus.Delivered;
        Raise(new ShipmentDelivered(Id, OrderId));
    }
}
