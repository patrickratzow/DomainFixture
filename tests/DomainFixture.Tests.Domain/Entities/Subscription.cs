namespace DomainFixture.Tests.Domain.Entities;

public enum SubscriptionStatus
{
    Pending,
    Active
}

public sealed class SubscriptionDetails
{
    public SubscriptionStatus Status { get; }

    public SubscriptionDetails(SubscriptionStatus status)
    {
        Status = status;
    }
}

public sealed class Subscription
{
    public Guid Id { get; }
    public string PlanName { get; }
    public int Seats { get; }
    public SubscriptionStatus Status { get; }
    public SubscriptionDetails Details { get; }

    private Subscription(
        Guid id,
        string planName,
        int seats,
        SubscriptionStatus status)
    {
        Id = id;
        PlanName = planName;
        Seats = seats;
        Status = status;
        Details = new SubscriptionDetails(status);
    }

    public static Subscription Create(string planName, int seats, Guid id) =>
        new(id, planName, seats, SubscriptionStatus.Pending);

    public Subscription Activate(int seats) =>
        new(Id, PlanName, seats, SubscriptionStatus.Active);

    public bool CanReserve(int seats) => seats >= 0;
}
