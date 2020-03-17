using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb
{
    /// <summary>
    /// Holds a value that will be serialized in a separate git blob without any
    /// json serialization.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public class Blob<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Blob{TValue}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public Blob(TValue value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public TValue Value { get; }
    }
}
