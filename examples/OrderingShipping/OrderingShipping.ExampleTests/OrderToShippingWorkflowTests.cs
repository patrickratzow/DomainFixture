using FluentAssertions;
using NUnit.Framework;
using OrderingShipping.ExampleTests.Generation;
using OrderingShipping.ExampleTests.TestSupport;
using OrderingShipping.Orders.Application;
using OrderingShipping.Orders.Domain;
using OrderingShipping.Shipping.Application;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests;

[TestFixture]
public sealed class OrderToShippingWorkflowTests
{
    [Test]
    public void PlaceOrderCommand_ShouldCreateAQueryableShipmentThroughTheEventBoundary()
    {
        var command = PlaceOrderFixtureFactory.Valid.Create();
        var nextShipmentId = ShipmentIdFixtureFactory.Valid.Create();
        var application = new OrderingShippingTestHost(nextShipmentId);

        var result = application.PlaceOrder.Handle(command);
        var orderQuery = GetOrderFixtureFactory.Valid.Create(query =>
            query with { OrderId = command.OrderId });
        var shipmentQuery = GetShipmentByOrderFixtureFactory.Valid.Create(query =>
            query with { OrderId = command.OrderId.Value });

        result.Should().Be(command.OrderId);
        application.GetOrder.Handle(orderQuery).Should().BeEquivalentTo(new
        {
            command.OrderId,
            Status = OrderStatus.Placed,
            Total = command.Lines.Sum(line => line.UnitPrice.Amount * line.Quantity),
            Currency = command.Lines[0].UnitPrice.Currency,
            LineCount = command.Lines.Count
        });
        application.GetShipmentByOrder.Handle(shipmentQuery)
            .Should().BeEquivalentTo(new
            {
                ShipmentId = nextShipmentId,
                OrderId = command.OrderId.Value,
                Status = ShipmentStatus.Pending,
                TrackingNumber = (string?)null
            });
    }

    [Test]
    public void ShippingCommands_ShouldAdvanceTheWriteModelAndItsQueryProjection()
    {
        var create = CreateShipmentFixtureFactory.Valid.Create();
        var dispatch = DispatchShipmentFixtureFactory.Valid.Create(command =>
            command with { ShipmentId = create.ShipmentId });
        var deliver = DeliverShipmentFixtureFactory.Valid.Create(command =>
            command with { ShipmentId = create.ShipmentId });
        var query = GetShipmentByOrderFixtureFactory.Valid.Create(request =>
            request with { OrderId = create.OrderId });
        var application = new OrderingShippingTestHost(create.ShipmentId);

        application.CreateShipment.Handle(create);
        application.DispatchShipment.Handle(dispatch);
        application.DeliverShipment.Handle(deliver);

        application.GetShipmentByOrder.Handle(query)
            .Should().BeEquivalentTo(new
            {
                create.ShipmentId,
                create.OrderId,
                Status = ShipmentStatus.Delivered,
                TrackingNumber = dispatch.TrackingNumber.Value
            });
    }
}
