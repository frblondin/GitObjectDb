using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// Represents the result of a validation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        /// <param name="failures">The failures.</param>
        /// <exception cref="ArgumentNullException">failures</exception>
        public ValidationResult(IList<ValidationFailure> failures)
        {
            Errors = failures ?? throw new ArgumentNullException(nameof(failures));
        }

        /// <summary>
        /// Gets the failures.
        /// </summary>
        public IList<ValidationFailure> Errors { get; } = new List<ValidationFailure>();

        /// <summary>
        /// Gets a value indicating whether the validation result is valid.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid => !Errors.Any();

        /// <inheritdoc/>
        public override string ToString() => string.Join(Environment.NewLine, Errors);
    }
}