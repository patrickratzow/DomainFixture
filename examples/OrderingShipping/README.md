# Order & Shipping DDD/CQRS example

This is a runnable example of using DomainFixture against a domain that is larger than a validator or a single value object. It has two bounded contexts, synchronous CQRS handlers, aggregate-owned invariants, domain events, an integration-event boundary, generated specifications, and generated factories consumed by handwritten tests.

## Run it

From the repository root:

```powershell
dotnet test examples/OrderingShipping/OrderingShipping.ExampleTests/OrderingShipping.ExampleTests.csproj
```

The test project materializes source-generator output under `obj/Generated`. This makes generated
factories and test suites navigable in IDEs after a build; for example, search that directory for
`OrderFixture.Factory.g.cs`. The directory is refreshed before compilation so deleted recipes do
not leave stale generated files. The files remain compiler output and should not be committed.

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

The example profile contains no fixture values. DomainFixture recursively discovers conventional
value construction from the domain symbols:

- `OrderId.From(Guid)`, `CustomerId.From(Guid)`, and other GUID-backed values receive a fresh
  non-empty `Guid` on every factory invocation;
- `Sku.From(string)` and `TrackingNumber.From(string)` receive a non-empty string;
- `Money.Usd(decimal)` is selected because it is the single unambiguous static factory returning
  `Money`;
- unconstrained integral and decimal baselines use positive `1`, avoiding the common invalid zero
  quantity/count case;
- `ShippingAddress`, `DeliveryAddress`, `OrderLine`, and their collections are composed recursively.

Generated strings come from one thread-safe sequence shared by every generated factory in the test
assembly. Length constraints are respected, and a finite string space fails on exhaustion rather
than reusing a value. Explicit configured values still remain intentionally reusable overrides.

Explicit `options.Values().For<T>(...)` remains available when the domain has multiple meaningful
choices, but it is an override—not required setup.

The profile also enables `options.Conventions().AutoSynthesizeRecipes()`. Any recipe without an
explicit `Baseline`, `Synthesize`, or `FromTransition` source therefore receives a synthesized
valid baseline. Explicit sources always take precedence.

Leaf value objects do not need empty fixture declarations. `ShippingAddress`, `DeliveryAddress`,
and `OrderLine` are discovered and composed recursively while synthesizing their aggregate roots.
A dedicated fixture is only useful when a type needs named recipes, generated behavioral tests, or
a typed factory consumed directly by handwritten tests.

The recipes remain focused on behavior:

```csharp
fixture.Recipe("Pending")
    .State("Starts pending", shipment => shipment.Status, ShipmentStatus.Pending)
    .Transition(
        "Dispatch",
        shipment => shipment.Dispatch(FixtureValue.Auto<TrackingNumber>()),
        shipment => shipment.Status,
        ShipmentStatus.Dispatched);
```

Later valid states reuse those named transitions instead of rebuilding the aggregate by hand:

```csharp
fixture.Recipe("Dispatched")
    .FromTransition("Pending", "Dispatch")
    .Transition(
        "Deliver",
        shipment => shipment.Deliver(),
        shipment => shipment.Status,
        ShipmentStatus.Delivered);

fixture.Recipe("Delivered")
    .FromTransition("Dispatched", "Deliver");
```

The same applies to Orders: the Cancelled recipe is simply
`.FromTransition("Placed", "Cancel")`; it contains no duplicate IDs, address, line items, or manual
aggregate construction.

`FromTransition` asks the generator to create the source recipe, resolve any automatic command
arguments through the same valid-value pipeline, apply the transition, and return the resulting
fresh aggregate. Recipes can form an acyclic state graph; missing transitions and dependency
cycles are reported as generation diagnostics.

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
