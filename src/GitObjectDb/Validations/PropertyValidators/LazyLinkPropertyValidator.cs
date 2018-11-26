using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Validations.PropertyValidators
{
    /// <summary>
    /// <see cref="ILazyLink"/> validator.
    /// </summary>
    public sealed class LazyLinkPropertyValidator : IPropertyValidator
    {
        /// <inheritdoc/>
        public bool CanValidate(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return typeof(ILazyLink).IsAssignableFrom(type);
        }

        /// <inheritdoc/>
        public IEnumerable<ValidationFailure> Validate(string propertyName, object value, ValidationContext context)
        {
            var link = value as ILazyLink ?? throw new GitObjectDbException($"Property of type {nameof(ILazyLink)} expected.");
            return ObjectPathPropertyValidator.ValidatePath($"propertyName.{nameof(ILazyLink.Path)}", link.Path, context);
        }
    }
}
