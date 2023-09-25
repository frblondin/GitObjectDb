using LibGit2Sharp;
using ObjectsComparer;
using System.Collections.Generic;

namespace GitObjectDb.Comparison;

/// <summary>Provides a set of methods to compare repository items.</summary>
public interface IComparer
{
    /// <summary>Compares two objects of the same type to each other.</summary>
    /// <param name="expectedObject">The expected object value to compare.</param>
    /// <param name="actualObject">The actual object value to compare.</param>
    /// <param name="policy">The merge policy to use.</param>
    /// <param name="result">An enumerable containing all the differences.</param>
    /// <returns>Details about the comparison.</returns>
    bool Compare(object? expectedObject, object? actualObject, ComparisonPolicy policy, out IEnumerable<Difference> result);
}

internal interface IComparerInternal
{
    /// <summary>Compares two trees recursively.</summary>
    /// <param name="connection">The current connection.</param>
    /// <param name="old">The starting point of comparison.</param>
    /// <param name="new">The end start of comparison.</param>
    /// <param name="policy">The merge policy to use.</param>
    /// <returns>Details about the comparison.</returns>
    ChangeCollection Compare(IConnectionInternal connection,
                             Commit old,
                             Commit @new,
                             ComparisonPolicy? policy = null);
}
