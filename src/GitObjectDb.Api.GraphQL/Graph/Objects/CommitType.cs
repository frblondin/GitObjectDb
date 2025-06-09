using GitDotNet;
using GraphQL.Types;

namespace GitObjectDb.Api.GraphQL.Graph.Objects;

internal class CommitType : ObjectGraphType<CommitEntry>
{
    public CommitType()
    {
        Field(c => c.Id).Description("Gets the 40 character sha1 of this object.");
        Field(c => c.Message).Description("Gets the commit message.");
    }
}
