## **This is just an initial dirty prototype to test the concept! It will be rewritten later to make greater use of C# Source Generators, and to incorporate a lot more features**

# DomainFixture
C# fixture library for creating domain entities & value objects in valid/invalid states.

# The problem
Say you have an entity that looks like this
```cs
public class User
{
  public int Id { get; set; }
  public Subscription Subscription { get; set; } = Subscription.Free;
  [StringLength(4096, MinimumLength = 4)]
  public string Description { get; set; }
  public List<ContactDetail> ContactDetails { get; set; } = new();
  public List<Project> Projects { get; set; } = new();
}
```

When making tests you would need to make sure that the User instance is valid/invalid according to your use case, and that is a major pain the instant any dependency to another object exists.

## Test Scenarios
If you were to test just the description property you would have 4 different things to test
- Length = 3 (Invalid)
- Length = 4 (Valid)
- Length = 4096 (Valid)
- Length = 4097 (Invalid)

But as mentioned earlier, your Arrange part of your test would have to setup a valid object. Some validation libraries such as FluentValidation does allow you to specify what property you want to test, so it won't run validation on any other property. However that does not change testing object validation is a major pain, and most developers find it boring..

# The solution
Instead of having to create the objects yourself, you can use this library to setup valid & invalid scenarios, and then create fixtures for your tests.

A hypothethical way that could look in code is
```cs
public UserTestConfiguration : TestConfiguration<User>
{
  protected override void Configure()
  {
    // default (0) is invalid, but 1 or -1 would be valid
    Property(x => x.Id)
      .Empty().IsInvalid();
    
    // Length 4-4096 is valid, but a string with the length of 4 consisting of just whitespaces wouldn't be valid
    Property(x => x.Description)
      Length(4, 4096).IsValid()
      Empty().IsInvalid();

    // Having 0, 1, or 2 contact details are valid.
    Property(x => x.ContactDetails)
      .Count(0, 2).IsValid();
      
    // Enums would not need to have scenarios setup, they are automatically inferred.

    // Conditional logic
    Property(x => x.Projects)
      .When(x => x.Subscription, Subscription.Free).Count(0, 3).IsValid()
      .When(x => x.Subscription, Subscription.Premium).Count(0, 25).IsValid();
      .When(x => x.Subscription, Subscription.Enterprise).Count(0, 100).IsValid();
  }
}
```
DomainFixture would then generate all the tests according to your specificiations, making it a lot easier & less grunt work to make tests for validation logic.
