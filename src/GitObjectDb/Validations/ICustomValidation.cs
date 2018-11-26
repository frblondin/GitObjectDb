using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// Performs additional custom validations.
    /// </summary>
    public interface ICustomValidation
    {
        /// <summary>
        /// Validates this instance.
        /// </summary>
        /// <param name="context">The validation context.</param>
        /// <returns>The list of validation failures.</returns>
        IEnumerable<ValidationFailure> Validate(ValidationContext context);
    }
}
