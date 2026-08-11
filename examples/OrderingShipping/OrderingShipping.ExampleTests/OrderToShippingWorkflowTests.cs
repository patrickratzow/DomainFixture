using FluentAssertions;
using NUnit.Framework;
using OrderingShipping.Orders.Application;
using OrderingShipping.Orders.Domain;
using OrderingShipping.SharedKernel;
using OrderingShipping.Shipping.Application;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests;

[TestFixture]
public sealed class OrderToShippingWorkflowTests
{
    [Test]
    public void PlaceOrderCommand_ShouldCreateAQueryableShipmentThroughTheEventBoundary()
    {
        var events = new DomainEventDispatcher();
        var orders = new InMemoryOrderRepository();
        var shipments = new InMemoryShipmentRepository();
        var createShipment = new CreateShipmentHandler(shipments, events);
        var shipmentId = ShipmentId.From(new Guid("30000000-0000-0000-0000-000000000404"));

        events.Subscribe(new CreateShipmentWhenOrderPlaced(
            new FixedShipmentIdGenerator(shipmentId),
            createShipment));

        var placeOrder = new PlaceOrderHandler(orders, events);
        var getOrder = new GetOrderHandler(orders);
        var getShipment = new GetShipmentByOrderHandler(shipments);
        var orderId = OrderId.From(new Guid("10000000-0000-0000-0000-000000000404"));

        var result = placeOrder.Handle(new PlaceOrder(
            orderId,
            CustomerId.From(new Guid("20000000-0000-0000-0000-000000000404")),
            ShippingAddress.Create("12 Domain Lane", "Copenhagen", "2100", "DK"),
            new[] { OrderLine.Create(Sku.From("DDD-BOOK"), 2, Money.Usd(25m)) }));

        result.Should().Be(orderId);
        getOrder.Handle(new GetOrder(orderId)).Should().BeEquivalentTo(new
        {
            OrderId = orderId,
            Status = OrderStatus.Placed,
            Total = 50m,
            Currency = "USD",
            LineCount = 1
        });
        getShipment.Handle(new GetShipmentByOrder(orderId.Value)).Should().BeEquivalentTo(new
        {
            ShipmentId = shipmentId,
            OrderId = orderId.Value,
            Status = ShipmentStatus.Pending,
            TrackingNumber = (string?)null
        });
    }

    [Test]
    public void ShippingCommands_ShouldAdvanceTheWriteModelAndItsQueryProjection()
    {
        var events = new DomainEventDispatcher();
        var shipments = new InMemoryShipmentRepository();
        var shipmentId = ShipmentId.From(new Guid("30000000-0000-0000-0000-000000000405"));
        var orderId = new Guid("10000000-0000-0000-0000-000000000405");
        new CreateShipmentHandler(shipments, events).Handle(new CreateShipment(
            shipmentId,
            orderId,
            DeliveryAddress.Create("12 Domain Lane", "Copenhagen", "2100", "DK")));

        new DispatchShipmentHandler(shipments, events).Handle(new DispatchShipment(
            shipmentId,
            TrackingNumber.From("TRACK-405")));
        new DeliverShipmentHandler(shipments, events).Handle(new DeliverShipment(shipmentId));

        new GetShipmentByOrderHandler(shipments)
            .Handle(new GetShipmentByOrder(orderId))
            .Should().BeEquivalentTo(new
            {
                ShipmentId = shipmentId,
                OrderId = orderId,
                Status = ShipmentStatus.Delivered,
                TrackingNumber = "TRACK-405"
            });
    }

    private sealed class FixedShipmentIdGenerator : IShipmentIdGenerator
    {
        private readonly ShipmentId _shipmentId;

        public FixedShipmentIdGenerator(ShipmentId shipmentId)
        {
            _shipmentId = shipmentId;
        }

        public ShipmentId Next() => _shipmentId;
    }
}
