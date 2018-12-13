using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Validations.PropertyValidators;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// Object validator.
    /// </summary>
    public class Validator : IValidator
    {
        private readonly IModelDataAccessorProvider _modelDataAccessorProvider;
        private readonly IEnumerable<IPropertyValidator> _propertyValidators;

        /// <summary>
        /// Initializes a new instance of the <see cref="Validator"/> class.
        /// </summary>
        /// <param name="modelDataAccessor">The model data accessor.</param>
        /// <param name="propertyValidators">The property validators.</param>
        /// <exception cref="ArgumentNullException">serviceProvider</exception>
        public Validator(IModelDataAccessorProvider modelDataAccessor, IEnumerable<IPropertyValidator> propertyValidators)
        {
            _modelDataAccessorProvider = modelDataAccessor ?? throw new ArgumentNullException(nameof(modelDataAccessor));
            _propertyValidators = propertyValidators ?? throw new ArgumentNullException(nameof(propertyValidators));
        }

        /// <inheritdoc/>
        public ValidationResult Validate(ValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var result = new List<ValidationFailure>();
            ValidateImpl(context, result);

            return new ValidationResult(result);
        }

        private void ValidateImpl(ValidationContext context, List<ValidationFailure> result)
        {
            var dataProvider = _modelDataAccessorProvider.Get(context.Instance.GetType());

            ValidatePropertyValues(context, dataProvider, result);
            GetCustomValidationResult(context, result);

            ValidateChildren(context, dataProvider, result);
        }

        private void ValidatePropertyValues(ValidationContext context, IModelDataAccessor dataProvider, List<ValidationFailure> result)
        {
            foreach (var property in dataProvider.ModifiableProperties)
            {
                var propertyValue = property.Accessor(context.Instance);
                if (propertyValue == null)
                {
                    continue;
                }

                if (property.IsDiscriminatedUnion)
                {
                    ValidateDiscriminatedUnion(context, property.Name, propertyValue, result);
                }
                else
                {
                    ValidatePropertyValue(context, property.Name, propertyValue, result);
                }
            }
        }

        private bool ValidatePropertyValue(ValidationContext context, string propertyName, object value, List<ValidationFailure> result)
        {
            var type = value.GetType();
            var anyMatchingValidator = false;
            foreach (var validator in _propertyValidators)
            {
                if (validator.CanValidate(type))
                {
                    anyMatchingValidator = true;
                    result.AddRange(validator.Validate(propertyName, value, context));
                }
            }
            return anyMatchingValidator;
        }

        private void ValidateDiscriminatedUnion(ValidationContext context, string propertyName, object value, List<ValidationFailure> result)
        {
            foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var fieldName = $"{propertyName}.{field.Name}";
                var fieldValue = field.GetValue(value);
                if (fieldValue != null)
                {
                    var anyMatchingValidator = ValidatePropertyValue(context, fieldName, fieldValue, result);
                    if (!anyMatchingValidator)
                    {
                        ValidateDiscriminatedUnion(context, fieldName, fieldValue, result);
                    }
                }
            }
        }

        private static void GetCustomValidationResult(ValidationContext context, List<ValidationFailure> result)
        {
            result.AddRange(context.Instance is ICustomValidation customValidation ?
                            customValidation.Validate(context) :
                            Enumerable.Empty<ValidationFailure>());
        }

        private void ValidateChildren(ValidationContext context, IModelDataAccessor dataProvider, List<ValidationFailure> result)
        {
            foreach (var childProperty in dataProvider.ChildProperties)
            {
                var children = childProperty.Accessor(context.Instance);
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        var nestedContext = context.NewNested(childProperty, child);
                        ValidateImpl(nestedContext, result);
                    }
                }
            }

            ValidateContainer(context, result);
        }

        private void ValidateContainer(ValidationContext context, List<ValidationFailure> result)
        {
            if (context.Instance is IObjectRepositoryContainer container)
            {
                foreach (var repository in container.Repositories)
                {
                    var repositoryContext = new ValidationContext(repository, ValidationChain.Empty, context.Rules, null);
                    ValidateImpl(repositoryContext, result);
                }
            }
        }
    }
}
