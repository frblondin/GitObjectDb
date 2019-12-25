using GitObjectDb.Reflection;
using GitObjectDb.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Model tree node.
    /// </summary>
    public interface IModelObject
    {
        /// <summary>
        /// Gets the data accessor.
        /// </summary>
        IModelDataAccessor DataAccessor { get; }

        /// <summary>
        /// Gets the parent repository.
        /// </summary>
        IObjectRepository Repository { get; }

        /// <summary>
        /// Gets the container.
        /// </summary>
        IObjectRepositoryContainer Container { get; }

        /// <summary>
        /// Gets the parent.
        /// </summary>
        IModelObject Parent { get; }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        UniqueId Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the <see cref="ObjectPath"/>.
        /// </summary>
        ObjectPath Path { get; }

        /// <summary>
        /// Attaches to instance to a given parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void AttachToParent(IModelObject parent);

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>The children.</returns>
        IAsyncEnumerable<IModelObject> GetChildrenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates the specified rules.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <returns>A <see cref="ValidationResult"/> object containing any validation failures.</returns>
        Task<ValidationResult> ValidateAsync(ValidationRules rules = ValidationRules.All);
    }
}
