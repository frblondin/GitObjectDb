using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GitObjectDb.Validations.PropertyValidators
{
    /// <summary>
    /// Proprety validator.
    /// </summary>
    public interface IPropertyValidator
    {
        /// <summary>
        /// Determines whether this instance can validate the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///   <c>true</c> if this instance can validate the specified type; otherwise, <c>false</c>.
        /// </returns>
        bool CanValidate(Type type);

        /// <summary>
        /// Validates the property value.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="value">The value.</param>
        /// <param name="context">The context.</param>
        /// <returns>The list of falires, if any.</returns>
        IEnumerable<ValidationFailure> Validate(string propertyName, object value, ValidationContext context);
    }
}
