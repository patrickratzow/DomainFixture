using DomainFixture.Generation;
using OrderingShipping.Orders.Domain;
using OrderingShipping.Shipping.Domain;

namespace OrderingShipping.ExampleTests.Generation;

public sealed class OrderingShippingProfile : IFixtureGenerationProfile
{
    public void Configure(IFixtureGenerationOptions options)
    {
        options.Conventions()
            .UseNullability()
            .UsePropertyNames()
            .UseImmutableObjects()
            .UseEntityIdentity();

        options.Activation()
            .UseFactories();

        options.Values()
            .For<int>(() => 2)
            .For<OrderId>(() => OrderId.From(new Guid("10000000-0000-0000-0000-000000000001")))
            .For<CustomerId>(() => CustomerId.From(new Guid("20000000-0000-0000-0000-000000000002")))
            .For<Sku>(() => Sku.From("DDD-BOOK"))
            .For<Money>(() => Money.Usd(25m))
            .For<ShipmentId>(() => ShipmentId.From(new Guid("30000000-0000-0000-0000-000000000003")))
            .For<TrackingNumber>(() => TrackingNumber.From("TRACK-123"));
    }
}
