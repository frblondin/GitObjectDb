namespace GitObjectDb.Models
{
    /// <summary>
    /// Stores the content of a value in a separate file.
    /// </summary>
    public interface IBlob
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        object Value { get; }
    }

    /// <summary>
    /// Stores the content of a value in a separate file.
    /// </summary>
    /// <typeparam name="TValue">Type of the value.</typeparam>
    public interface IBlob<out TValue> : IBlob
    {
        /// <summary>
        /// Gets the value.
        /// </summary>
        new TValue Value { get; }
    }
}