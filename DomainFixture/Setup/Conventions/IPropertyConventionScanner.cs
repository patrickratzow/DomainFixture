namespace DomainFixture.Setup.Conventions;

internal interface IPropertyConventionScanner
{
    internal IDictionary<Type, IList<IPropertyConvention>> GetConventions(IEnumerable<Type> types);
    internal void ApplyConvention(ApplyPropertyConvention applyPropertyConvention);
}