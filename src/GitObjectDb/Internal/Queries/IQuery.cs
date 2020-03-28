using LibGit2Sharp;

namespace GitObjectDb.Internal.Queries
{
    internal interface IQuery<in TArg, out TResult>
    {
        TResult Execute(IConnectionInternal connection, TArg arg);
    }
}
