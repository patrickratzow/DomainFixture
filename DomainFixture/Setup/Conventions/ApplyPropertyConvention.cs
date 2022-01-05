using System.Reflection;

namespace DomainFixture.Setup.Conventions;

internal record ApplyPropertyConvention(
    IClassConfiguration ClassConfiguration,
    EntityConfigurationBuilder EntityBuilder,
    Type EntityType,
    Dictionary<Type, List<IPropertyConvention>> Conventions,
    PropertyInfo PropertyInfo)
{
    public IEnumerable<IPropertyConvention> FilteredConventions
    {
        get
        {
            if (!Conventions.TryGetValue(PropertyInfo.PropertyType, out var conventions))
                conventions = new();
            
            return conventions!
                .Where(x => ClassConfiguration.Conventions.RemovedConventions.All(c => x.GetType() != c.GetType()));
        }
    }
}