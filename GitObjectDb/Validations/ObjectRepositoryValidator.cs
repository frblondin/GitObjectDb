using FluentValidation;
using FluentValidation.Validators;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// See <see cref="IObjectRepositoryContainer{TRepository}"/> dependency validator.
    /// </summary>
    /// <typeparam name="TRepository">The type of the repository.</typeparam>
    public class ObjectRepositoryValidator<TRepository> : AbstractValidator<TRepository>
        where TRepository : AbstractObjectRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryValidator{TRepository}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ObjectRepositoryValidator(IServiceProvider serviceProvider)
        {
            Include(new MetadataObjectValidator<TRepository>(serviceProvider));
            RuleSet(nameof(ValidationRules.Dependency), () =>
                RuleForEach(r => r.Dependencies)
                    .Must(ValidateDependency)
                    .WithMessage("The {PropertyName} contains an an invalid dependency: {Message}"));
        }

        bool ValidateDependency(TRepository repository, RepositoryDependency dependency, PropertyValidatorContext context)
        {
            var container = (IObjectRepositoryContainer<TRepository>)repository.Container;
            var foundRepository = container.Repositories.FirstOrDefault(r => r.Id == dependency.Id);
            if (foundRepository == null)
            {
                context.MessageFormatter.AppendArgument("Message", $"the repository with id {dependency.Id} could not be found in container.");
                return false;
            }
            if (foundRepository.Version < dependency.Version)
            {
                context.MessageFormatter.AppendArgument("Message", $"the repository with id {dependency.Id} used in container should be of version >= {dependency.Version} ({foundRepository.Version} currently).");
                return false;
            }
            return true;
        }
    }
}
