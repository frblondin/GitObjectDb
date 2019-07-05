using GitObjectDb.Models;
using GitObjectDb.Reflection;
using System;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// Represents the context of validation.
    /// </summary>
    public class ValidationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationContext"/> class.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="rules">The rules.</param>
        /// <param name="parent">The parent.</param>
        public ValidationContext(IModelObject instance, ValidationChain chain, ValidationRules rules, ValidationContext parent = null)
        {
            Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            Chain = chain ?? throw new ArgumentNullException(nameof(chain));
            Rules = rules;
            Parent = parent;
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public IModelObject Instance { get; }

        /// <summary>
        /// Gets the chain.
        /// </summary>
        public ValidationChain Chain { get; }

        /// <summary>
        /// Gets the rules.
        /// </summary>
        public ValidationRules Rules { get; }

        /// <summary>
        /// Gets the parent validation context.
        /// </summary>
        public ValidationContext Parent { get; }

        /// <summary>
        /// Creates a nested context.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>The new context.</returns>
        public ValidationContext NewNested(ChildPropertyInfo property, IModelObject instance)
        {
            return new ValidationContext(instance, Chain.Add(Instance, property), Rules, this);
        }
   }
}