namespace DomainFixture;

internal static class TypeExtensions
{
    internal static bool IsAssignableTo(this Type from, Type to)
        => to.IsAssignableFrom(from);

    internal static bool IsDerivedOfGenericType(this Type type, Type genericType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType) return true;

        return type.BaseType is not null && IsDerivedOfGenericType(type.BaseType, genericType);
    }
}