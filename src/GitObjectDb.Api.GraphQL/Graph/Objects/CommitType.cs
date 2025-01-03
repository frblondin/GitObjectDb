using GraphQL.Types;
using LibGit2Sharp;

namespace GitObjectDb.Api.GraphQL.Graph.Objects;

internal class CommitType : ObjectGraphType<Commit>
{
    public CommitType()
    {
        Field(c => c.Sha).Description("Gets the 40 character sha1 of this object.");
        Field(c => c.Message).Description("Gets the commit message.");
    }
}
