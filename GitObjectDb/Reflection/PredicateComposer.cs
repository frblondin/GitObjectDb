using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Composes multiple <see cref="PredicateReflector"/> to perform multiple changes at once.
    /// </summary>
    public class PredicateComposer : IPredicateReflector
    {
        readonly IList<PredicateReflector> _reflectors = new List<PredicateReflector>();

        /// <summary>
        /// Adds a new <see cref="PredicateReflector"/>.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="node">The node.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The instance itself to allow chained calls.</returns>
        public PredicateComposer And<TModel>(TModel node, Expression<Func<TModel, bool>> predicate)
            where TModel : IMetadataObject
        {
            _reflectors.Add(new PredicateReflector(node, predicate));
            return this;
        }

        /// <inheritdoc/>
        public object ProcessArgument(IMetadataObject instance, string name, Type argumentType, object fallback = null)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (argumentType == null)
            {
                throw new ArgumentNullException(nameof(argumentType));
            }

            var matchingPredicate = _reflectors.FirstOrDefault(r => r.Instance == instance);
            if (matchingPredicate != null)
            {
                return matchingPredicate.ProcessArgument(instance, name, argumentType, fallback);
            }
            return fallback is ICloneable cloneable ? cloneable.Clone() : fallback;
        }

        /// <inheritdoc/>
        public (IEnumerable<IMetadataObject> Additions, IEnumerable<IMetadataObject> Deletions) GetChildChanges(IMetadataObject instance, ChildPropertyInfo childProperty)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (childProperty == null)
            {
                throw new ArgumentNullException(nameof(childProperty));
            }

            var matchingPredicate = _reflectors.FirstOrDefault(r => r.Instance == instance);
            if (matchingPredicate != null)
            {
                return matchingPredicate.GetChildChanges(instance, childProperty);
            }
            return (Enumerable.Empty<IMetadataObject>(), Enumerable.Empty<IMetadataObject>());
        }
    }
}
