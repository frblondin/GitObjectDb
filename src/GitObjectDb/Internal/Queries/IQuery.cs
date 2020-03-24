using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Internal.Queries
{
    internal interface IQuery<TArg, TResult>
    {
        TResult Execute(Repository repository, TArg arg);
    }

    internal interface IQuery<TArg1, TArg2, TResult>
    {
        TResult Execute(Repository repository, TArg1 arg1, TArg2 arg2);
    }
}
