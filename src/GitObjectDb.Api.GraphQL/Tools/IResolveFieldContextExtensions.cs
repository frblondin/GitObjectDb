using GraphQL;

namespace GitObjectDb.Api.GraphQL.Tools;
internal static class IResolveFieldContextExtensions
{
    internal static TType? GetArgumentFromParentContexts<TType>(this IResolveFieldContext context, string name, TType defaultValue = default!)
    {
        var parentContext = context;
        while (parentContext is not null)
        {
            var value = context.GetArgument<TType>(name);
            if (value is not null)
            {
                return value;
            }
            parentContext = parentContext.Parent;
        }
        return defaultValue;
    }
}
