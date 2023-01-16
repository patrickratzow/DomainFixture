using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DomainFixture.TestGenerator.Framework;

public record FrameworkAttributes(IReadOnlyCollection<Attribute> TestAttributes, Attribute? ClassAttribute = null)
{
    /// <summary>
    /// All namespaces used by the attributes
    /// </summary>½
    /// <return>A distinct list of full path namespaces</return>
    private IReadOnlyCollection<string> Namespaces
    {
        get
        {
            var query = TestAttributes
                .Select(x => x.Namespace);

            if (ClassAttribute is not null)
                query = query.Append(ClassAttribute.Namespace);
            
            return new ReadOnlyCollection<string>
            (
                query.Distinct().ToList()
            );
        }
    }
}