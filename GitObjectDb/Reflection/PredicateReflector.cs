using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <inheritdoc/>
    internal partial class PredicateReflector : IPredicateReflector
    {
        static readonly MethodInfo _childrenAddMethod = ExpressionReflector.GetMethod<ILazyChildren>(c => c.Add(default));
        static readonly MethodInfo _childrenDeleteMethod = ExpressionReflector.GetMethod<ILazyChildren>(c => c.Delete(default));

        readonly PredicateVisitor _visitor;
        readonly Expression _predicate;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredicateReflector"/> class.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="predicate">The predicate.</param>
        public PredicateReflector(IMetadataObject instance, Expression predicate)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _visitor = predicate != null ? CreateVisitor(predicate) : null;
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public IMetadataObject Instance { get; }

        static PredicateVisitor CreateVisitor(Expression predicate)
        {
            var visitor = new PredicateVisitor();
            visitor.Visit(predicate);
            return visitor;
        }

        /// <inheritdoc/>
        public object ProcessArgument(IMetadataObject instance, string name, Type argumentType, object fallback = null)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (argumentType == null)
            {
                throw new ArgumentNullException(nameof(argumentType));
            }

            if (instance != Instance)
            {
                return fallback is ICloneable cloneable ? cloneable.Clone() : fallback;
            }

            if (_visitor != null && _visitor.Values.TryGetValue(name, out var value))
            {
                return value;
            }

            return fallback;
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

            var childChanges = instance == Instance && _visitor != null && _visitor.ChildChanges.TryGetValue(childProperty.Name, out var changes) ? changes : null;
            return
            (
                childChanges?.Where(c => c.Type == ChildChangeType.Add).Select(c => c.Child),
                childChanges?.Where(c => c.Type == ChildChangeType.Delete).Select(c => c.Child)
            );
        }
    }
}
