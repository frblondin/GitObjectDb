using GitObjectDb.Transformations;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// A set of methods for instances of <see cref="IObjectRepositoryContainer"/>.
    /// </summary>
    internal static class IObjectRepositoryExtensions
    {
        internal const string NodeNotInRepositoryMessage = "Modified node is not defined in target repository.";

        /// <summary>
        /// Returns the result of the provided function processing.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="processor">The function.</param>
        /// <returns>The result of the function call.</returns>
        internal static TResult Execute<TResult>(this IObjectRepository repository, Func<IRepository, TResult> processor)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }
            if (repository.RepositoryDescription == null)
            {
                throw new GitObjectDbException($"No {nameof(repository.RepositoryDescription)} has been set on this instance.");
            }
            return repository.RepositoryProvider.Execute(repository.RepositoryDescription, processor);
        }

        /// <summary>
        /// Calls the provided function processing.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="processor">The function.</param>
        internal static void Execute(this IObjectRepository repository, Action<IRepository> processor)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }
            if (repository.RepositoryDescription == null)
            {
                throw new GitObjectDbException($"No {nameof(repository.RepositoryDescription)} has been set on this instance.");
            }
            repository.RepositoryProvider.Execute(repository.RepositoryDescription, processor);
        }

        /// <summary>
        /// Creates a copy of the repository and apply changes according to the new test values provided in the transformation.
        /// </summary>
        /// <typeparam name="TRepository">The type of the repository.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="transformation">The transformation.</param>
        /// <returns>The newly created copy. Both parents and children nodes have been cloned as well.</returns>
        public static IObjectRepository With<TRepository>(this TRepository repository, Func<ITransformationComposer, ITransformationComposer> transformation)
            where TRepository : IObjectRepository
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (transformation == null)
            {
                throw new ArgumentNullException(nameof(transformation));
            }

            var composer = transformation(new TransformationComposer(repository, ImmutableList.Create<ITransformation>()));
            return repository.DataAccessor.With(repository, composer);
        }

        /// <summary>
        /// Creates a copy of the repository and apply changes according to the new test values provided in the transformation.
        /// </summary>
        /// <typeparam name="TRepository">The type of the repository.</typeparam>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TProperty">The type of the property or field.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="node">The node to which the change should be applied.</param>
        /// <param name="propertyPicker">An expression that identifies the property or field that will have <paramref name="value" /> assigned.</param>
        /// <param name="value">The value to assign to the property or field identified by <paramref name="propertyPicker" />.</param>
        /// <returns>The newly created copy. Both parents and children nodes have been cloned as well.</returns>
        public static TModel With<TRepository, TModel, TProperty>(this TRepository repository, TModel node, Expression<Func<TModel, TProperty>> propertyPicker, TProperty value = default)
            where TRepository : IObjectRepository
            where TModel : IModelObject
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (propertyPicker == null)
            {
                throw new ArgumentNullException(nameof(propertyPicker));
            }
            if (!object.ReferenceEquals(node.Repository, repository))
            {
                throw new GitObjectDbException(NodeNotInRepositoryMessage);
            }

            var propertyTransformation = new PropertyTransformation(node, propertyPicker, value);
            var composer = new TransformationComposer(repository, ImmutableList.Create<ITransformation>(propertyTransformation));
            var result = (TRepository)repository.DataAccessor.With(repository, composer);
            return (TModel)result.GetFromGitPath(node.GetDataPath());
        }
    }
}
