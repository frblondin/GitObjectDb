using GitObjectDb.Models;
using GitObjectDb.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Transformations
{
    /// <inheritdoc />
    internal partial class TransformationComposer : ITransformationComposer
    {
        private readonly IObjectRepository _repository;
        private readonly IImmutableList<ITransformation> _transformations;

        internal TransformationComposer(IObjectRepository repository, IImmutableList<ITransformation> transformations)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _transformations = transformations ?? throw new ArgumentNullException(nameof(transformations));
        }

        /// <inheritdoc />
        public ITransformationComposer Update<TModel, TProperty>(TModel node, Expression<Func<TModel, TProperty>> propertyPicker, TProperty value)
            where TModel : IModelObject
        {
            EnsureInRepository(node);
            return new TransformationComposer(_repository, _transformations.Add(new PropertyTransformation(node, propertyPicker, value)));
        }

        /// <inheritdoc />
        public ITransformationComposer Add<TModel, TChildProperty>(TModel node, Expression<Func<TModel, TChildProperty>> propertyPicker, IModelObject child)
            where TModel : IModelObject
            where TChildProperty : ILazyChildren
        {
            EnsureInRepository(node);
            return new TransformationComposer(_repository, _transformations.Add(new ChildAddTransformation(node, propertyPicker, child)));
        }

        /// <inheritdoc />
        public ITransformationComposer Remove<TModel, TChildProperty>(TModel node, Expression<Func<TModel, TChildProperty>> propertyPicker, IModelObject child)
            where TModel : IModelObject
            where TChildProperty : ILazyChildren
        {
            EnsureInRepository(node);
            EnsureInRepository(child);
            return new TransformationComposer(_repository, _transformations.Add(new ChildDeleteTransformation(child)));
        }

        private void EnsureInRepository<TModel>(TModel node)
            where TModel : IModelObject
        {
            if (!object.ReferenceEquals(node.Repository, _repository))
            {
                throw new GitObjectDbException(IObjectRepositoryExtensions.NodeNotInRepositoryMessage);
            }
        }

        public IEnumerator<ITransformation> GetEnumerator() => _transformations.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _transformations.GetEnumerator();
    }
}
