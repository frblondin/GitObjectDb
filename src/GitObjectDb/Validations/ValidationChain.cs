using GitObjectDb.Models;
using GitObjectDb.Reflection;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// Gets the list of parents.
    /// </summary>
    public class ValidationChain
    {
        private ValidationChain()
            : this(ImmutableArray.Create<(IModelObject, ChildPropertyInfo)>())
        {
        }

        private ValidationChain(ImmutableArray<(IModelObject Instance, ChildPropertyInfo Property)> parents)
        {
            Parents = parents;
        }

        /// <summary>
        /// Gets the empty chain singleton.
        /// </summary>
        public static ValidationChain Empty { get; } = new ValidationChain();

        /// <summary>
        /// Gets the parent list.
        /// </summary>
        public ImmutableArray<(IModelObject Instance, ChildPropertyInfo Property)> Parents { get; }

        /// <summary>
        /// Adds the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="property">The property.</param>
        /// <returns>The result of the validation chain update.</returns>
        public ValidationChain Add(IModelObject instance, ChildPropertyInfo property)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return new ValidationChain(Parents.Add((instance, property)));
        }

        /// <inheritdoc/>
        public override string ToString() =>
            string.Join("/", Parents.Select(p => $"{p.Instance.Id}/{p.Property.Name}"));
    }
}