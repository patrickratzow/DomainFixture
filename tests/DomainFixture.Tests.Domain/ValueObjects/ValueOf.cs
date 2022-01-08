using System.Linq.Expressions;
using System.Reflection;

namespace DomainFixture.Tests.Domain.ValueObjects;

public class ValueOf<TValue, TThis> : Validatable<TThis> where TThis : ValueOf<TValue, TThis>, new()
{
    private static readonly Func<TThis> Factory;

    static ValueOf()
    {
        var ctor = typeof(TThis)
            .GetTypeInfo()
            .DeclaredConstructors
            .First();

        var argsExp = Array.Empty<Expression>();
        var newExp = Expression.New(ctor, argsExp);
        var lambda = Expression.Lambda(typeof(Func<TThis>), newExp);

        Factory = (Func<TThis>)lambda.Compile();
    }

    public TValue Value { get; protected set; } = default!;

    public static TThis From(TValue item)
    {
        var x = Factory();
        x.Value = item;
        x.Validate();

        return x;
    }

    protected virtual bool Equals(ValueOf<TValue, TThis> other)
    {
        return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((ValueOf<TValue, TThis>)obj);
    }

    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return EqualityComparer<TValue>.Default.GetHashCode(Value!);
    }

    public static bool operator ==(ValueOf<TValue, TThis>? a, ValueOf<TValue, TThis>? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(ValueOf<TValue, TThis>? a, ValueOf<TValue, TThis>? b)
    {
        return !(a == b);
    }

    public override string ToString()
    {
        return Value!.ToString() ?? throw new InvalidOperationException();
    }
}