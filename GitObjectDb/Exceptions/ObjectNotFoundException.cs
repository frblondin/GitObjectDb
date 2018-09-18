using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb
{
    /// <summary>
    /// The exception that is thrown attempting to reference a resource that does not exist.
    /// </summary>
    /// <seealso cref="Exception" />
    public class ObjectNotFoundException : GitObjectDbException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ObjectNotFoundException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
