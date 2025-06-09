using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Internal.Queries;

internal interface IAsyncQuery<in TArg, TResult>
{
    Task<TResult> ExecuteAsync(IConnection queryAccessor, TArg arg);
}

internal interface IAsyncEnumerableQuery<in TArg, out TResult>
{
    IAsyncEnumerable<TResult> ExecuteAsync(IConnection queryAccessor, TArg arg, CancellationToken token = default);
}
