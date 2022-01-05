namespace DomainFixture.Setup.Conventions;

internal class GenericPropertyConventionScanner : IPropertyConventionScanner
{
    IDictionary<Type, IList<IPropertyConvention>> IPropertyConventionScanner.GetConventions(IEnumerable<Type> types)
    {
        var conventions = types
            .Where(t => t.GetInterfaces().Any(i => i.IsDerivedOfGenericType(typeof(IPropertyConvention<>))))
            .ToList();

        var dict = new Dictionary<Type, IList<IPropertyConvention>>();
        
        foreach (var convention in conventions)
        {
            var interfaces = convention.GetInterfaces()
                .Where(i => i.IsDerivedOfGenericType(typeof(IPropertyConvention<>)));

            foreach (var @interface in interfaces)
            {
                var propertyType = @interface.GenericTypeArguments.First();
                if (!dict.TryGetValue(propertyType, out var conventionList))
                {
                    conventionList = new List<IPropertyConvention>();
                }

                if (Activator.CreateInstance(convention) is not IPropertyConvention conventionInstance)
                    throw new InvalidCastException($"Unable to create {convention.Name} as {nameof(IPropertyConvention)}");

                conventionList!.Add(conventionInstance);

                dict[propertyType] = conventionList;
            }
        }

        return dict;
    }


    void IPropertyConventionScanner.ApplyConvention(ApplyPropertyConvention applyPropertyConvention)
    {
        var propertyType = applyPropertyConvention.PropertyInfo.PropertyType;
        var filteredConventions = applyPropertyConvention.FilteredConventions;

        foreach (var convention in filteredConventions)
        {
            // <Guid, TEntity>
            var runMethod = convention.GetType().GetMethods()
                .Where(m => m.Name is "Run")
                .Single(m => m.ReturnType.GenericTypeArguments.First() == propertyType);
            var generic = runMethod!.MakeGenericMethod(applyPropertyConvention.EntityType);

            var constructedType = typeof(ConventionsPropertyBuilder<,>).MakeGenericType(propertyType, 
                applyPropertyConvention.EntityType);
            var propertyExpression = PropertyConventionHelper.GetPropertyExpression(
                applyPropertyConvention.PropertyInfo.Name, propertyType, applyPropertyConvention.EntityType);
            var propertyBuilder = Activator.CreateInstance(constructedType, propertyExpression) as dynamic;

            generic.Invoke(convention, new object?[]
            {
                applyPropertyConvention.PropertyInfo, propertyBuilder
            });

            applyPropertyConvention.EntityBuilder.PropertyBuilders.Add(propertyBuilder);
        }
    }
}