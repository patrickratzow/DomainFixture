using OrderingShipping.Orders.Application;
using OrderingShipping.SharedKernel;
using OrderingShipping.Shipping.Application;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests.TestSupport;

internal sealed class OrderingShippingTestHost
{
    private readonly InMemoryOrderRepository _orders = new();
    private readonly InMemoryShipmentRepository _shipments = new();

    public PlaceOrderHandler PlaceOrder { get; }
    public GetOrderHandler GetOrder { get; }
    public CreateShipmentHandler CreateShipment { get; }
    public DispatchShipmentHandler DispatchShipment { get; }
    public DeliverShipmentHandler DeliverShipment { get; }
    public GetShipmentByOrderHandler GetShipmentByOrder { get; }

    public OrderingShippingTestHost(ShipmentId nextShipmentId)
    {
        var events = new DomainEventDispatcher();
        CreateShipment = new CreateShipmentHandler(_shipments, events);
        events.Subscribe(new CreateShipmentWhenOrderPlaced(
            new FixedShipmentIdGenerator(nextShipmentId),
            CreateShipment));

        PlaceOrder = new PlaceOrderHandler(_orders, events);
        GetOrder = new GetOrderHandler(_orders);
        DispatchShipment = new DispatchShipmentHandler(_shipments, events);
        DeliverShipment = new DeliverShipmentHandler(_shipments, events);
        GetShipmentByOrder = new GetShipmentByOrderHandler(_shipments);
    }
}
