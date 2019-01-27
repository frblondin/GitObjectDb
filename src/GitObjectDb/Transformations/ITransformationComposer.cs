using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace GitObjectDb.Transformations
{
    /// <summary>
    /// Composes multiple <see cref="PropertyTransformation"/> to perform multiple changes at once.
    /// </summary>
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public interface ITransformationComposer : IEnumerable<ITransformation>
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        /// <summary>
        /// Adds a new <see cref="PropertyTransformation"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TProperty">The type of the property or field.</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="propertyPicker">An expression that identifies the property or field that will have <paramref name="value" /> assigned.</param>
        /// <param name="value">The value to assign to the property or field identified by <paramref name="propertyPicker" />.</param>
        /// <returns>An <see cref="ITransformationComposer"/> which can be used to further customize the transformations.</returns>
        ITransformationComposer Update<TModel, TProperty>(TModel node, Expression<Func<TModel, TProperty>> propertyPicker, TProperty value)
            where TModel : IModelObject;

        /// <summary>
        /// Adds a new child.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TChildProperty">The type of the child property or field.</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="propertyPicker">An expression that identifies the property or field that will have the <paramref name="child" /> added.</param>
        /// <param name="child">The child to add to the property or field identified by <paramref name="propertyPicker" />.</param>
        /// <returns>An <see cref="ITransformationComposer"/> which can be used to further customize the transformations.</returns>
        ITransformationComposer Add<TModel, TChildProperty>(TModel node, Expression<Func<TModel, TChildProperty>> propertyPicker, IModelObject child)
            where TModel : IModelObject
            where TChildProperty : ILazyChildren;

        /// <summary>
        /// Removes a new child.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TChildProperty">The type of the child property or field.</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="propertyPicker">An expression that identifies the property or field that will have the <paramref name="child" /> removed.</param>
        /// <param name="child">The child to remove to the property or field identified by <paramref name="propertyPicker" />.</param>
        /// <returns>An <see cref="ITransformationComposer"/> which can be used to further customize the transformations.</returns>
        ITransformationComposer Remove<TModel, TChildProperty>(TModel node, Expression<Func<TModel, TChildProperty>> propertyPicker, IModelObject child)
            where TModel : IModelObject
            where TChildProperty : ILazyChildren;
    }
}