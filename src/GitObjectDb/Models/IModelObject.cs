using GitObjectDb.Reflection;
using GitObjectDb.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

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
        /// Gets the children.
        /// </summary>
        IEnumerable<IModelObject> Children { get; }

        /// <summary>
        /// Attaches to instance to a given parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void AttachToParent(IModelObject parent);

        /// <summary>
        /// Validates the specified rules.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <returns>A <see cref="ValidationResult"/> object containing any validation failures.</returns>
        ValidationResult Validate(ValidationRules rules = ValidationRules.All);
    }
}
