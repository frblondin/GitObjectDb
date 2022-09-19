namespace GitObjectDb.Api.GraphQL;

/// <summary>
/// Indicates to Code Analysis that a method validates a particular parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class ValidatedNotNullAttribute : Attribute
{
}