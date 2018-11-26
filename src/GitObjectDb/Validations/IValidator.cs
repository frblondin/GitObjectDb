namespace GitObjectDb.Validations
{
    /// <summary>
    /// Object validator.
    /// </summary>
    public interface IValidator
    {
        /// <summary>
        /// Validates the instance from the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The validation result.</returns>
        ValidationResult Validate(ValidationContext context);
    }
}