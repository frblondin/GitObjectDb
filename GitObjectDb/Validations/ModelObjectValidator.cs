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
    /// <see cref="IModelObject"/> validator.
    /// </summary>
    /// <typeparam name="TModelObject">The type of the model object being validated</typeparam>
    /// <seealso cref="AbstractValidator{IModelObject}" />
    public class ModelObjectValidator<TModelObject> : AbstractValidator<TModelObject>
        where TModelObject : IModelObject
    {
        readonly IValidatorFactory _validatorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelObjectValidator{TModelObject}"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ModelObjectValidator(IServiceProvider serviceProvider)
        {
            _validatorFactory = serviceProvider.GetRequiredService<IValidatorFactory>();

            RuleFor(
                p => typeof(ILazyLink).IsAssignableFrom(p.PropertyType),
                _ => new ChildValidatorAdaptor(LazyLinkValidator.Instance, LazyLinkValidator.Instance.GetType()));
            RuleForEach<IModelObject>(
                p => LazyChildrenHelper.TryGetLazyChildrenInterface(p.PropertyType) != null);
        }

        static (LambdaExpression Expression, Expression<Func<object, object>> NonGenericExpression) CreatePropertyAccessors(PropertyInfo p)
        {
            var instanceParam = Expression.Parameter(typeof(TModelObject));
            var nonGenInstanceParam = Expression.Parameter(typeof(object));
            return (
                Expression.Lambda(
                    Expression.GetFuncType(typeof(TModelObject), p.PropertyType),
                    Expression.Property(instanceParam, p),
                    instanceParam),
                Expression.Lambda<Func<object, object>>(
                    Expression.Convert(
                    Expression.Property(
                    Expression.Convert(nonGenInstanceParam, typeof(TModelObject)), p),
                    typeof(object)),
                    nonGenInstanceParam)
            );
        }

        void RuleFor(Predicate<PropertyInfo> predicate, Func<PropertyRule, IPropertyValidator> validator)
        {
            foreach (var property in typeof(TModelObject).GetTypeInfo().GetProperties())
            {
                if (!property.CanRead || !predicate(property))
                {
                    continue;
                }
                var (expression, nonGenericExpression) = CreatePropertyAccessors(property);
                var rule = new PropertyRule(property, nonGenericExpression.Compile(), expression, () => CascadeMode, property.PropertyType, typeof(TModelObject));
                AddRule(rule);
                rule.AddValidator(validator(rule));
            }
        }

        void RuleForEach<TProperty>(Predicate<PropertyInfo> predicate)
        {
            foreach (var property in typeof(TModelObject).GetTypeInfo().GetProperties())
            {
                if (!property.CanRead || !predicate(property))
                {
                    continue;
                }
                var (expression, nonGenericExpression) = CreatePropertyAccessors(property);
                var rule = new CollectionPropertyRule<TProperty>(property, nonGenericExpression.Compile(), expression, () => CascadeMode, property.PropertyType, typeof(TModelObject));
                AddRule(rule);
                var validator = new ChildValidatorAdaptor(GetPropertyValidator, typeof(IValidator));
                rule.AddValidator(validator);
            }
        }

        IValidator GetPropertyValidator(IValidationContext context) =>
            _validatorFactory.GetValidator(context.PropertyValue.GetType());
    }
}
