using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Validations.PropertyValidators
{
    /// <summary>
    /// <see cref="IEnumerable{RepositoryDependency}"/> property validator.
    /// </summary>
    public class DependencyPropertyValidator : IPropertyValidator
    {
        /// <inheritdoc/>
        public bool CanValidate(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return typeof(IEnumerable<RepositoryDependency>).IsAssignableFrom(type);
        }

        /// <inheritdoc/>
        public IEnumerable<ValidationFailure> Validate(string propertyName, object value, ValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return ValidateIterator(propertyName, value, context);
        }

        private IEnumerable<ValidationFailure> ValidateIterator(string propertyName, object value, ValidationContext context)
        {
            var dependencies = value as IEnumerable<RepositoryDependency> ?? throw new GitObjectDbException($"Property of type {nameof(IEnumerable<RepositoryDependency>)} expected.");
            foreach (var dependency in dependencies)
            {
                var foundRepository = context.Instance.Container.Repositories.FirstOrDefault(r => r.Id == dependency.Id);
                if (foundRepository == null)
                {
                    yield return new ValidationFailure(propertyName, $"Repository with id {dependency.Id} could not be found in container.", context);
                }
                else if (foundRepository.Version < dependency.Version)
                {
                    yield return new ValidationFailure(propertyName, $"Repository with id {dependency.Id} used in container should be of version >= {dependency.Version} ({foundRepository.Version} currently).", context);
                }
            }
        }
    }
}
