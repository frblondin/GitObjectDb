namespace GitObjectDb.Internal.Queries;

internal interface IQuery<in TArg, out TResult>
{
    TResult Execute(IQueryAccessor queryAccessor, TArg arg);
}
