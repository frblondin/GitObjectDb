namespace System
{
    /// <summary>
    /// The exception that is thrown when the requested service could not be found.
    /// </summary>
    /// <seealso cref="Exception" />
    public sealed class MissingDependencyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MissingDependencyException"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public MissingDependencyException(Type type)
            : base($"The service '{type}' could not be found in current service provider.")
        {
        }
    }
}
