namespace OrderingShipping.SharedKernel;

public sealed class DomainEventDispatcher
{
    private readonly Dictionary<Type, List<Action<IDomainEvent>>> _subscriptions = new();

    public void Subscribe<TEvent>(IDomainEventHandler<TEvent> handler)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_subscriptions.TryGetValue(typeof(TEvent), out var subscriptions))
        {
            subscriptions = new List<Action<IDomainEvent>>();
            _subscriptions.Add(typeof(TEvent), subscriptions);
        }

        subscriptions.Add(domainEvent => handler.Handle((TEvent)domainEvent));
    }

    public void Dispatch(IEnumerable<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            if (!_subscriptions.TryGetValue(domainEvent.GetType(), out var subscriptions))
            {
                continue;
            }

            foreach (var subscription in subscriptions)
            {
                subscription(domainEvent);
            }
        }
    }
}
