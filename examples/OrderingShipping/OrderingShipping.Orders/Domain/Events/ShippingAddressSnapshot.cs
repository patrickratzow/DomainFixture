namespace OrderingShipping.Orders.Domain;

public sealed record ShippingAddressSnapshot(
    string Line1,
    string City,
    string PostalCode,
    string CountryCode);
