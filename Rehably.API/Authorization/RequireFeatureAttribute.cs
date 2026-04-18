namespace Rehably.API.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute
{
    public string FeatureCode { get; }
    public int? Amount { get; }

    public RequireFeatureAttribute(string featureCode, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(featureCode))
            throw new ArgumentException("Feature code cannot be empty.", nameof(featureCode));

        FeatureCode = featureCode;
        Amount = amount;
    }
}
