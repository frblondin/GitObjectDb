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
    /// <summary>
    /// Analyzes the conditions of a predicate to collect property assignments to be made.
    /// </summary>
    internal partial class PredicateReflector
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

        /// <summary>
        /// Returns the value collected by the reflector, <paramref name="fallback"/> is none was found.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="argumentType">The argument type.</param>
        /// <param name="fallback">The fallback value.</param>
        /// <returns>The final value to be used for the parameter.</returns>
        internal object ProcessArgument(IMetadataObject instance, string name, Type argumentType, object fallback)
        {
            if (instance != Instance)
            {
                return fallback;
            }
            if (_visitor != null && _visitor.Values.TryGetValue(name, out var value))
            {
                return value;
            }
            if (argumentType == null)
            {
                throw new ArgumentNullException(nameof(argumentType));
            }

            return fallback;
        }

        /// <summary>
        /// Gets the child changes collected by the reflector.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="childProperty">The child property.</param>
        /// <returns>A list of changes.</returns>
        internal (IEnumerable<IMetadataObject> Additions, IEnumerable<IMetadataObject> Deletions) GetChildChanges(IMetadataObject instance, ChildPropertyInfo childProperty)
        {
            var childChanges = instance == Instance && _visitor != null && _visitor.ChildChanges.TryGetValue(childProperty.Name, out var changes) ? changes : null;
            return
            (
                childChanges?.Where(c => c.Type == ChildChangeType.Add).Select(c => c.Child),
                childChanges?.Where(c => c.Type == ChildChangeType.Delete).Select(c => c.Child)
            );
        }
    }
}
