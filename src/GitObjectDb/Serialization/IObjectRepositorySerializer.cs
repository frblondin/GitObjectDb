using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitObjectDb.Models;

namespace GitObjectDb.Serialization
{
    /// <summary>
    /// Creates a new instance of <see cref="IObjectRepositorySerializer"/>.
    /// </summary>
    /// <param name="context">The serialization context.</param>
    /// <returns>The newly created instance.</returns>
    public delegate IObjectRepositorySerializer ObjectRepositorySerializerFactory(ModelObjectSerializationContext context = null);

    /// <summary>
	/// Serializes and deserializes <see cref="IModelObject"/> objects into and from a Git repository.
    /// </summary>
    public interface IObjectRepositorySerializer
    {
        /// <summary>
		/// Deserializes the structure contained in the specified <see cref="Stream"/>.
        /// </summary>
		/// <param name="stream">The <see cref="Stream" /> that contains the structure to deserialize.</param>
		/// <returns>The <see cref="IModelObject"/> being deserialized.</returns>
        IModelObject Deserialize(Stream stream);

        /// <summary>
		/// Serializes the specified <see cref="IModelObject"/> and writes the structure
		/// using the specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="node">The <see cref="IModelObject"/> to serialize.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> used to write the structure.</param>
        void Serialize(IModelObject node, StringBuilder builder);

        /// <summary>
        /// Validates that the given type can be serialized successfully.
        /// </summary>
        /// <param name="type"></param>
        void ValidateSerializable(Type type);
    }
}
