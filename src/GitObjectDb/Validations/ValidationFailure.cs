using System;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// Holds the failure information.
    /// </summary>
    public class ValidationFailure
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationFailure"/> class.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="context">The context.</param>
        /// <exception cref="ArgumentNullException">
        /// propertyName
        /// or
        /// errorMessage
        /// or
        /// context
        /// </exception>
        public ValidationFailure(string propertyName, string errorMessage, ValidationContext context)
        {
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the error message
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the context.
        /// </summary>
        public ValidationContext Context { get; }

        /// <inheritdoc/>
        public override string ToString() => ErrorMessage;
    }
}