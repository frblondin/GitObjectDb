using FluentValidation;
using FluentValidation.Validators;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// See <see cref="IObjectRepositoryContainer{TRepository}"/> dependency validator.
    /// </summary>
    /// <typeparam name="TRepository">The type of the repository.</typeparam>
    public class ObjectRepositoryContainerValidator<TRepository> : AbstractValidator<IObjectRepositoryContainer<TRepository>>
        where TRepository : AbstractObjectRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryContainerValidator{TRepository}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ObjectRepositoryContainerValidator(IServiceProvider serviceProvider)
        {
            RuleSet(nameof(ValidationRules.Dependency), () =>
                RuleForEach(c => c.Repositories).SetValidator(new ObjectRepositoryValidator<TRepository>(serviceProvider)));
        }
    }
}
