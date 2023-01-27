using System.Collections.Generic;

namespace GitObjectDb.Model;

/// <summary>Resolves single node references.</summary>
/// <param name="propertyName">The property name of the node reference.</param>
/// <returns>Resolved node.</returns>
public delegate Node? SingleReferenceResolver(string propertyName);

/// <summary>Resolves multiple node references.</summary>
/// <param name="propertyName">The property name of the node reference.</param>
/// <returns>Resolved node.</returns>
public delegate IEnumerable<Node>? MultipleReferenceResolver(string propertyName);

/// <summary>Contains accessor to resolve node references.</summary>
public record ReferenceAccessors
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceAccessors"/> class.
    /// </summary>
    /// <param name="singleReferenceResolver">The single node resolver.</param>
    /// <param name="multiReferenceResolved">The multi node resolver.</param>
    public ReferenceAccessors(SingleReferenceResolver singleReferenceResolver,
                              MultipleReferenceResolver multiReferenceResolved)
    {
        SingleReferenceResolver = singleReferenceResolver;
        MultiReferenceResolved = multiReferenceResolved;
    }

    /// <summary>Gets the single node resolver.</summary>
    public SingleReferenceResolver SingleReferenceResolver { get; }

    /// <summary>Gets the multi node resolver.</summary>
    public MultipleReferenceResolver MultiReferenceResolved { get; }
}
