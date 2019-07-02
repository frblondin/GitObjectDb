using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Serialization
{
    /// <summary>
    /// Provides information about additional information that should be serialized.
    /// </summary>
    public class ModelNestedObjectInfo
    {
        /// <summary>Initializes a new instance of the <see cref="ModelNestedObjectInfo"/> class.</summary>
        /// <param name="fileName">The path.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="ArgumentNullException">path
        /// or
        /// data</exception>
        public ModelNestedObjectInfo(string fileName, StringBuilder data)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>Gets the path.</summary>
        public string FileName { get; }

        /// <summary>Gets the data.</summary>
        public StringBuilder Data { get; }
    }
}
