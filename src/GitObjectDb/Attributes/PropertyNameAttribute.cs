using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Attributes
{
    /// <summary>
    /// Instructs the GitToObjectDb engine that a specific property name should be used.
    /// </summary>
    /// <seealso cref="Attribute" />
    [AttributeUsage(AttributeTargets.Property)]
    [ExcludeFromGuardForNull]
    public sealed class PropertyNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        public PropertyNameAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            Name = name;
        }

        /// <summary>
        /// Gets the property name.
        /// </summary>
        public string Name { get; }
    }
}
