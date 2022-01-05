using System.Linq.Expressions;

namespace DomainFixture.Setup.Conventions;

internal static class PropertyConventionHelper
{
    internal static Expression GetPropertyExpression(string propertyName, Type propertyType, Type entityType)
    {
        var entityLambdaExpression = Expression.Parameter(entityType, "x");
        var propertyExpression = Expression.Parameter(propertyType, $"x.{propertyName}");
        var func = typeof(Func<,>).MakeGenericType(entityType, propertyType);
        var lambda = Expression.Lambda(func, propertyExpression, entityLambdaExpression);

        return lambda;
    }
}