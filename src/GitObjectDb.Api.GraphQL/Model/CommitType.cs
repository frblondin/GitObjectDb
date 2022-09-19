using GraphQL.Types;
using LibGit2Sharp;

namespace GitObjectDb.Api.GraphQL.Model;

internal class CommitType : ObjectGraphType<Commit>
{
    public CommitType()
    {
        Field(c => c.Sha);
        Field(c => c.Message);
    }
}
