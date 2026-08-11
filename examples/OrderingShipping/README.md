# Order & Shipping DDD/CQRS example

This is a runnable example of using DomainFixture against a domain that is larger than a validator or a single value object. It has two bounded contexts, synchronous CQRS handlers, aggregate-owned invariants, domain events, an integration-event boundary, generated specifications, and generated factories consumed by handwritten tests.

## Run it

From the repository root:

```powershell
dotnet test examples/OrderingShipping/OrderingShipping.ExampleTests/OrderingShipping.ExampleTests.csproj
```

The project currently runs 44 tests. DomainFixture generates most of the construction, equality, state, transition, rejection, and identity-preservation cases; the remaining tests describe event payloads and the end-to-end application workflow.

## Architecture

```text
OrderingShipping.SharedKernel
  AggregateRoot<TId>, domain-event dispatcher, CQRS contracts

OrderingShipping.Orders
  Order aggregate, value objects, OrderPlaced/OrderCancelled
  PlaceOrder + CancelOrder commands, GetOrder query

OrderingShipping.Shipping
  Shipment aggregate, value objects, Shipping-owned events
  Create/Dispatch/Deliver commands, GetShipmentByOrder query
  OrderPlaced -> CreateShipment translation

OrderingShipping.ExampleTests
  DomainFixture profile and recipes
  generated specifications and typed fixture factories
  handwritten aggregate and cross-context workflow tests
```

The dependency from Shipping to Orders exists only in the application-level `CreateShipmentWhenOrderPlaced` event handler. That handler translates the Orders event snapshot into Shipping's own `DeliveryAddress`; the Shipment aggregate does not use the Orders aggregate or its address value object.

## Domain behavior

`Order.Create` is the aggregate entry point. It rejects an empty cart, calculates its own total, starts in `Placed`, and emits `OrderPlaced`. `Order.Cancel` is the only cancellation path, requires a reason, rejects a second cancellation, and emits `OrderCancelled`.

`Shipment.Create` starts in `Pending` and emits `ShipmentCreated`. A Shipment can move only through:

```text
Pending -> Dispatched -> Delivered
```

Each transition emits a Shipping-owned event. Invalid transitions throw before mutating state.

Commands modify the write model through repositories. Queries return purpose-built DTOs instead of exposing aggregates. The end-to-end test wires the event boundary as follows:

```text
PlaceOrder
  -> Order.Create
  -> persist Order
  -> OrderPlaced
  -> CreateShipmentWhenOrderPlaced
  -> CreateShipment
  -> persist Shipment
  -> ShipmentCreated
```

## DomainFixture configuration

The central profile supplies valid business primitives once:

```csharp
options.Values()
    .For<int>(() => 2)
    .For<Sku>(() => Sku.From("DDD-BOOK"))
    .For<Money>(() => Money.Usd(25m))
    .For<TrackingNumber>(() => TrackingNumber.From("TRACK-123"));
```

The recipes remain focused on behavior:

```csharp
fixture.Recipe("Pending")
    .Synthesize()
    .State("Starts pending", shipment => shipment.Status, ShipmentStatus.Pending)
    .Transition(
        "Dispatch",
        shipment => shipment.Dispatch(FixtureValue.Auto<TrackingNumber>()),
        shipment => shipment.Status,
        ShipmentStatus.Dispatched);
```

DomainFixture discovers `Create` factories and recursively builds the graph. For an Order this means a configured `OrderId`, `CustomerId`, `Sku`, and `Money`, a generated `ShippingAddress`, a generated `OrderLine`, and a generated one-element `IReadOnlyList<OrderLine>`.

The same recipe emits a typed factory for tests written by hand:

```csharp
var order = OrderFixtureFactory.Placed.Create();
var pendingShipment = ShipmentFixtureFactory.Pending.Create();
```

Every call produces a fresh aggregate. The example tests prove that mutating one generated instance does not affect the next.

## Production substitutions

The example deliberately keeps plumbing small enough to read in one sitting. In production:

- replace the in-memory repositories with transactional persistence;
- record outgoing integration events in an outbox in the same transaction as the aggregate;
- publish the outbox asynchronously with idempotency and retry policies;
- treat the public `OrderPlaced` payload as a versioned integration contract, potentially in a dedicated contracts package;
- build read models from durable event/command processing instead of querying the in-memory write repository;
- add optimistic concurrency/version checks to aggregate persistence.

Those substitutions do not change the aggregate specifications or the DomainFixture recipes.
