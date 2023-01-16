using System.Collections.Generic;

namespace DomainFixture.Factories;

public interface IObjectState
{
    public IEnumerable<IObjectProperty> Properties { get; }
}