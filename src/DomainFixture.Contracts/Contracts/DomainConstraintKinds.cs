namespace DomainFixture.Contracts;

public static class DomainConstraintKinds
{
    public const string TextLength = "domainfixture.text.length";
    public const string TextMinimumLength = "domainfixture.text.minimum-length";
    public const string TextMaximumLength = "domainfixture.text.maximum-length";
    public const string TextNotEmpty = "domainfixture.text.not-empty";
    public const string TextNotNull = "domainfixture.text.not-null";
    public const string Int32InclusiveRange = "domainfixture.int32.inclusive-range";
    public const string Int32ExclusiveRange = "domainfixture.int32.exclusive-range";
    public const string Int32GreaterThan = "domainfixture.int32.greater-than";
    public const string Int32LessThan = "domainfixture.int32.less-than";
}

public static class DomainConstraintParameters
{
    public const string Minimum = "minimum";
    public const string Maximum = "maximum";
}
