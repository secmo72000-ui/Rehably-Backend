namespace Rehably.API.Authorization;

[AttributeUsage(AttributeTargets.Method)]
public class AllowWhenSuspendedAttribute : Attribute { }
