namespace OrderingShipping.Orders.Domain;

public sealed record OrderLine
{
    private OrderLine(Sku sku, int quantity, Money unitPrice)
    {
        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Sku Sku { get; }
    public int Quantity { get; }
    public Money UnitPrice { get; }
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    public static OrderLine Create(Sku sku, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "An order line needs at least one item.");
        }

        return new OrderLine(sku, quantity, unitPrice);
    }
}
