namespace OrderingShipping.Orders.Domain;

public sealed record ShippingAddress
{
    private ShippingAddress(string line1, string city, string postalCode, string countryCode)
    {
        Line1 = line1;
        City = city;
        PostalCode = postalCode;
        CountryCode = countryCode;
    }

    public string Line1 { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string CountryCode { get; }

    public static ShippingAddress Create(
        string line1,
        string city,
        string postalCode,
        string countryCode)
    {
        return new ShippingAddress(
            Required(line1, nameof(line1)),
            Required(city, nameof(city)),
            Required(postalCode, nameof(postalCode)),
            Required(countryCode, nameof(countryCode)).ToUpperInvariant());
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A value is required.", parameterName)
            : value.Trim();
}
