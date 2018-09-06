using FluentValidation.Results;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Metadata tree node.
    /// </summary>
    public interface IMetadataObject
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
        IMetadataObject Parent { get; }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the children.
        /// </summary>
        IEnumerable<IMetadataObject> Children { get; }

        /// <summary>
        /// Attaches to instance to a given parent.
        /// </summary>
        /// <param name="parent">The parent.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void AttachToParent(IMetadataObject parent);

        /// <summary>
        /// Validates the specified rules.
        /// </summary>
        /// <param name="rules">The rules.</param>
        /// <returns>A <see cref="ValidationResult"/> object containing any validation failures.</returns>
        ValidationResult Validate(ValidationRules rules = ValidationRules.None);
    }
}
