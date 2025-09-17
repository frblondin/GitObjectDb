namespace GitObjectDb;

/// <summary>Represents a series of node transformations to be written to <see cref="IIndex"/>.</summary>
public interface IIndexComposer : IChangeComposer
{
    /// <summary>Write the contents of this <see cref="IIndex"/> to disk.</summary>
    void Write();
}