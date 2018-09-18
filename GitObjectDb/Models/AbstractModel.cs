using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using GitObjectDb.Attributes;
using GitObjectDb.Reflection;
using GitObjectDb.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Abstract type that any <see cref="IMetadataObject"/> implementation should derive from.
    /// </summary>
    /// <seealso cref="IMetadataObject" />
    [DebuggerDisplay(DebuggerDisplay)]
    [DataContract]
    public abstract class AbstractModel : IMetadataObject
    {
        /// <summary>
        /// The debugger display used by models.
        /// </summary>
        internal const string DebuggerDisplay = "Name = {Name}, Id = {Id}";

        readonly IValidatorFactory _validatorFactory;
        readonly IObjectRepositorySearch _repositorySearch;
        readonly IModelDataAccessorProvider _dataAccessorProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException">
        /// serviceProvider
        /// or
        /// id
        /// or
        /// name
        /// </exception>
        protected AbstractModel(IServiceProvider serviceProvider, Guid id, string name)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _validatorFactory = serviceProvider.GetRequiredService<IValidatorFactory>();
            _repositorySearch = serviceProvider.GetRequiredService<IObjectRepositorySearch>();
            _dataAccessorProvider = serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
            DataAccessor = _dataAccessorProvider.Get(GetType());
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <inheritdoc />
        public IModelDataAccessor DataAccessor { get; }

        /// <inheritdoc />
        [DataMember]
        public Guid Id { get; }

        /// <inheritdoc />
        [DataMember]
        [Modifiable]
        public string Name { get; }

        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IEnumerable<IMetadataObject> Children => DataAccessor.ChildProperties.SelectMany(p => p.Accessor(this));

        /// <inheritdoc />
        public IMetadataObject Parent { get; private set; }

        /// <summary>
        /// Gets the parent instance.
        /// </summary>
        /// <exception cref="NotSupportedException">No parent repository has been set.</exception>
        public AbstractObjectRepository Repository =>
            GetRepository();

        /// <inheritdoc />
        IObjectRepository IMetadataObject.Repository => Repository;

        /// <inheritdoc />
        public virtual IObjectRepositoryContainer Container => Repository.Container;

        AbstractObjectRepository GetRepository() =>
            this.Root() as AbstractObjectRepository ??
            throw new GitObjectDbException("No parent repository has been set.");

        /// <inheritdoc />
        public void AttachToParent(IMetadataObject parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            if (Parent != null && Parent != parent)
            {
                throw new GitObjectDbException("A single metadata object cannot be attached to two different parents.");
            }

            Parent = parent;
        }

        /// <inheritdoc />
        public ValidationResult Validate(ValidationRules rules = ValidationRules.All)
        {
            var validator = _validatorFactory.GetValidator(GetType());
            var selector = rules == ValidationRules.All ?
                ValidatorOptions.ValidatorSelectors.RulesetValidatorSelectorFactory(new[] { "*" }) :
                ValidatorOptions.ValidatorSelectors.RulesetValidatorSelectorFactory(rules.ToString().Split(',', ';'));
            var context = new ValidationContext(this, new PropertyChain(), selector);
            return validator.Validate(context);
        }

        /// <inheritdoc />
        public IEnumerable<IMetadataObject> GetReferrers()
        {
            var isAsString = Id.ToString();
            var candidates = _repositorySearch.Grep(Container, isAsString);
            foreach (var node in candidates)
            {
                var accessor = _dataAccessorProvider.Get(node.GetType());
                var linkValues = from p in accessor.ModifiableProperties
                                 where p.IsLink
                                 let link = (ILazyLink)p.Accessor(node)
                                 where link.Path.Path.EndsWith(isAsString, StringComparison.OrdinalIgnoreCase)
                                 select p;
                if (linkValues.Any())
                {
                    yield return node;
                }
            }
        }
    }
}
