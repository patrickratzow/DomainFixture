namespace OrderingShipping.SharedKernel;

public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    void Handle(TEvent domainEvent);
}
