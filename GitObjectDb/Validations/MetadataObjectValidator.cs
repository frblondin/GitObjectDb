using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using GitObjectDb.Models;
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
                p => LazyChildrenHelper.TryGetLazyChildrenInterface(p.PropertyType) != null,
                r =>
                {
                    var validator = _validatorFactory.GetValidator(r.TypeToValidate);
                    return new ChildValidatorAdaptor(validator, validator.GetType());
                });
        }

        static (LambdaExpression expression, Expression<Func<object, object>> nonGenericExpression) CreatePropertyAccessors(PropertyInfo p)
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
            foreach (var p in typeof(TMetadataObject).GetProperties())
            {
                if (!p.CanRead || !predicate(p))
                {
                    continue;
                }
                var (expression, nonGenericExpression) = CreatePropertyAccessors(p);
                var rule = new PropertyRule(p, nonGenericExpression.Compile(), expression, () => CascadeMode, p.PropertyType, typeof(TMetadataObject));
                AddRule(rule);
                rule.AddValidator(validator(rule));
            }
        }

        void RuleForEach<TProperty>(Predicate<PropertyInfo> predicate, Func<PropertyRule, IPropertyValidator> validator)
        {
            foreach (var p in typeof(TMetadataObject).GetProperties())
            {
                if (!p.CanRead || !predicate(p))
                {
                    continue;
                }
                var (expression, nonGenericExpression) = CreatePropertyAccessors(p);
                var rule = new CollectionPropertyRule<TProperty>(p, nonGenericExpression.Compile(), expression, () => CascadeMode, p.PropertyType, typeof(TMetadataObject));
                AddRule(rule);
                rule.AddValidator(validator(rule));
            }
        }
    }
}
