using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// The exception that is thrown when the constructor cannot be found.
    /// </summary>
    /// <seealso cref="Exception" />
    public class UnableToFindConstructorException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnableToFindConstructorException"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public UnableToFindConstructorException(Type type)
            : base($"Unable to find a working constructor for type '{type}'. This often indicate that some parameters are not managed by the service provider and no property matching parameter name/types could be found.")
        {
        }
    }
}
