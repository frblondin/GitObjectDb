using LibGit2Sharp;

namespace GitObjectDb;

/// <summary>Represents a series of node transformations to be written to <see cref="Index"/>.</summary>
public interface IIndexComposer : ITransformationComposer
{
    /// <summary>Write the contents of this <see cref="Index"/> to disk.</summary>
    void Write();
}