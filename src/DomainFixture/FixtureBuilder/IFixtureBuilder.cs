using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DomainFixture.FixtureBuilder;

public interface IFixtureBuilder<TFixture>
{
    public IFixtureBuilder<TFixture> With<TProperty>(
        Expression<Func<TFixture, TProperty>> propertyExpression,
        TProperty value);
    public IFixtureBuilder<TFixture> With<TProperty>(
        Expression<Func<TFixture, TProperty>> propertyExpression,
        Func<TFixture, TProperty> valueFunc);
    public TFixture Create();
    public IEnumerable<TFixture> CreateMany(int? amount = null);
}