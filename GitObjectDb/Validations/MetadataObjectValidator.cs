using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using GitObjectDb.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// <see cref="IMetadataObject"/> validator.
    /// </summary>
    /// <typeparam name="TMetadataObject">The type of the metadata object being validated</typeparam>
    /// <seealso cref="AbstractValidator{IMetadataObject}" />
    public class MetadataObjectValidator<TMetadataObject> : AbstractValidator<TMetadataObject>
        where TMetadataObject : IMetadataObject
    {
        readonly IValidatorFactory _validatorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataObjectValidator{TMetadataObject}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public MetadataObjectValidator(IServiceProvider serviceProvider)
        {
            _validatorFactory = serviceProvider.GetRequiredService<IValidatorFactory>();

            RuleFor(
                p => typeof(ILazyLink).IsAssignableFrom(p.PropertyType),
                _ => new ChildValidatorAdaptor(LazyLinkValidator.Instance, LazyLinkValidator.Instance.GetType()));
            RuleForEach<IMetadataObject>(
                p => LazyChildrenHelper.TryGetLazyChildrenInterface(p.PropertyType) != null);
        }

        static (LambdaExpression Expression, Expression<Func<object, object>> NonGenericExpression) CreatePropertyAccessors(PropertyInfo p)
        {
            var instanceParam = Expression.Parameter(typeof(TMetadataObject));
            var nonGenInstanceParam = Expression.Parameter(typeof(object));
            return (
                Expression.Lambda(
                    Expression.GetFuncType(typeof(TMetadataObject), p.PropertyType),
                    Expression.Property(instanceParam, p),
                    instanceParam),
                Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                    Expression.Property(
                    Expression.Convert(nonGenInstanceParam, typeof(TMetadataObject)), p),
                    typeof(object)),
                    nonGenInstanceParam)
            );
        }

        void RuleFor(Predicate<PropertyInfo> predicate, Func<PropertyRule, IPropertyValidator> validator)
        {
            foreach (var property in typeof(TMetadataObject).GetTypeInfo().GetProperties())
            {
                if (!property.CanRead || !predicate(property))
                {
                    continue;
                }
                var (expression, nonGenericExpression) = CreatePropertyAccessors(property);
                var rule = new PropertyRule(property, nonGenericExpression.Compile(), expression, () => CascadeMode, property.PropertyType, typeof(TMetadataObject));
                AddRule(rule);
                rule.AddValidator(validator(rule));
            }
        }

        void RuleForEach<TProperty>(Predicate<PropertyInfo> predicate)
        {
            foreach (var property in typeof(TMetadataObject).GetTypeInfo().GetProperties())
            {
                if (!property.CanRead || !predicate(property))
                {
                    continue;
                }
                var (expression, nonGenericExpression) = CreatePropertyAccessors(property);
                var rule = new CollectionPropertyRule<TProperty>(property, nonGenericExpression.Compile(), expression, () => CascadeMode, property.PropertyType, typeof(TMetadataObject));
                AddRule(rule);
                var validator = new ChildValidatorAdaptor(GetPropertyValidator, typeof(IValidator));
                rule.AddValidator(validator);
            }
        }

        IValidator GetPropertyValidator(IValidationContext context) =>
            _validatorFactory.GetValidator(context.PropertyValue.GetType());
    }
}
