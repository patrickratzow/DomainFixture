namespace DomainFixture.Setup.Conventions;

internal class EnumPropertyConventionScanner : IPropertyConventionScanner
{
    IDictionary<Type, IList<IPropertyConvention>> IPropertyConventionScanner.GetConventions(IEnumerable<Type> types)
    {
        var conventions = types
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableTo(typeof(IEnumPropertyConvention))))
            .ToList();
        var dict = new Dictionary<Type, IList<IPropertyConvention>>();
        
        foreach (var convention in conventions)
        {
            var propertyType = typeof(Enum);
            if (!dict.TryGetValue(propertyType, out var conventionList))
            {
                conventionList = new List<IPropertyConvention>();
            }

            if (Activator.CreateInstance(convention) is not IEnumPropertyConvention conventionInstance)
                throw new InvalidCastException($"Unable to create {convention.Name} as {nameof(IEnumPropertyConvention)}");

            conventionList!.Add(conventionInstance);

            dict[propertyType] = conventionList;
        }

        return dict;
    }

    void IPropertyConventionScanner.ApplyConvention(ApplyPropertyConvention applyPropertyConvention)
    {
        var propertyType = applyPropertyConvention.PropertyInfo.PropertyType;
        if (!propertyType.IsEnum) return;
        var filteredConventions = applyPropertyConvention.Conventions
            .SelectMany(c => c.Value)
            .Where(c => c.GetType().IsAssignableTo(typeof(IEnumPropertyConvention)));

        foreach (var convention in filteredConventions)
        {
            var runMethod = convention.GetType().GetMethods()
                .Single(m => m.Name is "Run");
            var generic = runMethod!.MakeGenericMethod(applyPropertyConvention.EntityType);
            
            var constructedType = typeof(ConventionsPropertyBuilder<,>).MakeGenericType(typeof(object), 
                applyPropertyConvention.EntityType);
            var propertyExpression = PropertyConventionHelper.GetPropertyExpression(
                applyPropertyConvention.PropertyInfo.Name, typeof(object), applyPropertyConvention.EntityType);
            var propertyBuilder = Activator.CreateInstance(constructedType, propertyExpression) as dynamic;
            
            generic.Invoke(convention, new object?[]
            {
                applyPropertyConvention.PropertyInfo, propertyBuilder
            });

            applyPropertyConvention.EntityBuilder.PropertyBuilders.Add(propertyBuilder);
        }
        
        //throw new NotImplementedException();
    }
}