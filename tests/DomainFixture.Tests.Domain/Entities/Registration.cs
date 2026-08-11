namespace DomainFixture.Tests.Domain.Entities;

public enum RegistrationStatus
{
    Pending,
    Approved
}

public sealed class Registration
{
    public Guid Id { get; }
    public RegistrationStatus Status { get; private set; }

    public Registration(Guid id, RegistrationStatus status)
    {
        Id = id;
        Status = status;
    }

    public void Approve()
    {
        if (Status == RegistrationStatus.Approved)
            throw new InvalidOperationException("The registration is already approved.");

        Status = RegistrationStatus.Approved;
    }
}
