using System;

[System.Runtime.Serialization.DataContract]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements must be documented
#pragma warning disable CA1050 // Declare types in namespaces
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class ModelTemplate : GitObjectDb.Models.IModelObject
#pragma warning restore CA1050 // Declare types in namespaces
#pragma warning restore SA1600 // Elements must be documented
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    readonly IServiceProvider _serviceProvider;
    readonly GitObjectDb.Validations.IValidator _validator;
    readonly GitObjectDb.Reflection.IModelDataAccessorProvider _dataAccessorProvider;
    readonly GitObjectDb.Reflection.IModelDataAccessor _dataAccessor;

    GitObjectDb.Models.IObjectRepositoryContainer _container;

    private ModelTemplate(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _validator = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<GitObjectDb.Validations.IValidator>(serviceProvider);
        _dataAccessorProvider = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<GitObjectDb.Reflection.IModelDataAccessorProvider>(serviceProvider);
        _dataAccessor = _dataAccessorProvider.Get(GetType());
    }

#pragma warning disable CA1033 // Interface methods should be callable by child types
    /// <inheritdoc />
    GitObjectDb.Reflection.IModelDataAccessor GitObjectDb.Models.IModelObject.DataAccessor => _dataAccessor;
#pragma warning restore CA1033 // Interface methods should be callable by child types

    /// <inheritdoc />
    [System.Runtime.Serialization.DataMember]
    public GitObjectDb.Models.UniqueId Id { get; }

    /// <inheritdoc />
    [System.Runtime.Serialization.DataMember]
    [GitObjectDb.Attributes.Modifiable]
    public string Name { get; }

    /// <inheritdoc />
    [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
    public System.Collections.Generic.IEnumerable<GitObjectDb.Models.IModelObject> Children => System.Linq.Enumerable.SelectMany(_dataAccessor.ChildProperties, p => p.Accessor(this));

    /// <inheritdoc />
    public GitObjectDb.Models.IModelObject Parent { get; private set; }

    /// <summary>
    /// Gets the parent instance.
    /// </summary>
    /// <exception cref="NotSupportedException">No parent repository has been set.</exception>
    public GitObjectDb.Models.IObjectRepository Repository =>
        GetRepository();

    /// <inheritdoc />
    GitObjectDb.Models.IObjectRepository GitObjectDb.Models.IModelObject.Repository => Repository;

    /// <inheritdoc />
    public GitObjectDb.Models.IObjectRepositoryContainer Container
    {
        get => _container ?? Repository.Container;
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
        private set => _container = value ?? throw new ArgumentNullException("container");
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
    }

    GitObjectDb.Models.IObjectRepository GetRepository() =>
        GitObjectDb.Models.IModelObjectExtensions.Root(this) as GitObjectDb.Models.IObjectRepository ??
        throw new GitObjectDb.GitObjectDbException("No parent repository has been set.");

    /// <inheritdoc />
    public void AttachToParent(GitObjectDb.Models.IModelObject parent)
    {
        if (parent == null)
        {
            throw new ArgumentNullException(nameof(parent));
        }
        if (Parent != null && Parent != parent)
        {
            throw new GitObjectDb.GitObjectDbException("A single model object cannot be attached to two different parents.");
        }

        Parent = parent;
    }

    /// <inheritdoc />
    public GitObjectDb.Validations.ValidationResult Validate(GitObjectDb.ValidationRules rules = GitObjectDb.ValidationRules.All)
    {
        var context = new GitObjectDb.Validations.ValidationContext(this, GitObjectDb.Validations.ValidationChain.Empty, rules, null);
        return _validator.Validate(context);
    }

    partial void Initialize();
}
