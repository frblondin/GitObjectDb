using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Validations.PropertyValidators
{
    /// <summary>
    /// <see cref="ObjectPath"/> property validator.
    /// </summary>
    public class ObjectPathPropertyValidator : IPropertyValidator
    {
        /// <inheritdoc/>
        public bool CanValidate(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type == typeof(ObjectPath);
        }

        /// <inheritdoc/>
        public IEnumerable<ValidationFailure> Validate(string propertyName, object value, ValidationContext context) =>
            ValidatePath(propertyName, (ObjectPath)value, context);

        /// <summary>
        /// Validates the path.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="path">The path.</param>
        /// <param name="context">The context.</param>
        /// <returns>The list of failures, if any.</returns>
        /// <exception cref="GitObjectDbException">ObjectPath</exception>
        internal static IEnumerable<ValidationFailure> ValidatePath(string propertyName, ObjectPath path, ValidationContext context)
        {
            if (context.Instance.Repository != null)
            {
                if (!IsValidRepositoryDependency(path, context.Instance))
                {
                    yield return new ValidationFailure(propertyName, $"Referenced repository '{path.Repository}' is not added to the dependencies.", context);
                }
                else if (!IsReferencedObjectExisting(path, context.Instance))
                {
                    yield return new ValidationFailure(propertyName, $"Unexisting object {path} referenced.", context);
                }
            }
        }

        private static bool IsValidRepositoryDependency(ObjectPath path, IModelObject instance) =>
            path.Repository == instance.Repository.Id ||
            instance.Repository.Dependencies.Select(d => d.Id).Contains(path.Repository);

        private static bool IsReferencedObjectExisting(ObjectPath path, IModelObject instance) =>
            path.Repository == instance.Repository.Id ?
            instance.Repository.TryGetFromGitPath(path.Path) != null :
            instance.Container.TryGetFromGitPath(path) != null;
    }
}
