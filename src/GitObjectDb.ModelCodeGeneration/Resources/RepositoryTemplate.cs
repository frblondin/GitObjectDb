using System;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements must be documented
#pragma warning disable CA1050 // Declare types in namespaces
public partial class ModelTemplate : GitObjectDb.Models.IObjectRepository
#pragma warning restore CA1050 // Declare types in namespaces
#pragma warning restore SA1600 // Elements must be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    GitObjectDb.Services.IObjectRepositorySearch _repositorySearch;

    /// <inheritdoc />
    [System.Runtime.Serialization.DataMember]
    public System.Version Version { get; }

    /// <inheritdoc />
    [System.Runtime.Serialization.DataMember]
    [GitObjectDb.Attributes.Modifiable]
    public System.Collections.Immutable.IImmutableList<GitObjectDb.Models.RepositoryDependency> Dependencies { get; }

    /// <inheritdoc />
    [GitObjectDb.Attributes.PropertyName(GitObjectDb.FileSystemStorage.MigrationFolder)]
    public GitObjectDb.Models.ILazyChildren<GitObjectDb.Models.Migration.IMigration> Migrations { get; }

    /// <inheritdoc />
    public LibGit2Sharp.ObjectId CommitId { get; private set; }

    /// <inheritdoc />
    public GitObjectDb.Git.IRepositoryProvider RepositoryProvider { get; private set; }

    /// <inheritdoc />
    public GitObjectDb.Git.RepositoryDescription RepositoryDescription { get; private set; }

    partial void Initialize()
    {
        RepositoryProvider = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<GitObjectDb.Git.IRepositoryProvider>(_serviceProvider);
        _repositorySearch = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<GitObjectDb.Services.IObjectRepositorySearch>(_serviceProvider);
    }

    /// <inheritdoc />
    public GitObjectDb.Models.IModelObject TryGetFromGitPath(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (path.Equals(GitObjectDb.FileSystemStorage.DataFile, StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        var chunks = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (chunks.Length < 2)
        {
            return null;
        }

        GitObjectDb.Models.IModelObject result = this;
        for (int i = 0; result != null && i < chunks.Length - 1; i += 2)
        {
            var propertyInfo = System.Linq.IEnumerableExtensions.TryGetWithValue(
                result.DataAccessor.ChildProperties,
                p => p.FolderName,
                chunks[i]);
            if (propertyInfo == null)
            {
                return null;
            }

            var children = propertyInfo.Accessor(result);
            result = GitObjectDb.Models.UniqueId.TryParse(chunks[i + 1], out var id) ?
                System.Linq.Enumerable.FirstOrDefault(children, c => c.Id == id) :
                null;
        }
        return result;
    }

    /// <inheritdoc />
    public GitObjectDb.Models.IModelObject GetFromGitPath(string path) =>
        TryGetFromGitPath(path) ?? throw new LibGit2Sharp.NotFoundException($"The element with path '{path}' could not be found.");

    /// <inheritdoc/>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public void SetRepositoryData(GitObjectDb.Git.RepositoryDescription repositoryDescription, LibGit2Sharp.ObjectId commitId)
    {
        RepositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));
        CommitId = commitId ?? throw new ArgumentNullException(nameof(commitId));
    }

    /// <inheritdoc />
    public System.Collections.Generic.IEnumerable<GitObjectDb.Models.IModelObject> GetReferrers<TModel>(TModel node)
        where TModel : class, GitObjectDb.Models.IModelObject =>
        _repositorySearch.GetReferrers(node);
}
