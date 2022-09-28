using GitObjectDb.Api.GraphQL.Commands;

namespace GitObjectDb.Api.GraphQL.Converters;

internal static class PathToNodeConverter
{
    internal static Node Convert(object path)
    {
        var current = NodeMutation.Context.Current.Value ??
            throw new GitObjectDbException("Data context has not been set for path to node converter.");

        var dataPath = DataPath.Parse((string)path);
        return current.Convert(dataPath);
    }
}
