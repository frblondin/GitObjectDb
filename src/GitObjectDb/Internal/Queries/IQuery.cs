using LibGit2Sharp;

namespace GitObjectDb.Internal.Queries
{
    internal interface IQuery<in TArg, out TResult>
    {
        TResult Execute(Repository repository, TArg arg);
    }

    internal interface IQuery<in TArg1, in TArg2, out TResult>
    {
        TResult Execute(Repository repository, TArg1 arg1, TArg2 arg2);
    }
}
