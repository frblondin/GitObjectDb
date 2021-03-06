using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        /// <param name="relativeFileDataResolver">The relative file data resolver.</param>
        /// <returns>The <see cref="IModelObject"/> being deserialized.</returns>
        IModelObject Deserialize(Stream stream, Func<string, string> relativeFileDataResolver);

        /// <summary>
        /// Serializes the specified <see cref="IModelObject"/> and writes the structure
        /// using the specified <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="node">The <see cref="IModelObject"/> to serialize.</param>
        /// <param name="builder">The <see cref="StringBuilder"/> used to write the structure.</param>
        /// <returns>The list of additional nested files to be serialized, if any.</returns>
        IList<ModelNestedObjectInfo> Serialize(IModelObject node, StringBuilder builder);

        /// <summary>
        /// Validates that the given type can be serialized successfully.
        /// </summary>
        /// <param name="type">The type to be validated.</param>
        void ValidateSerializable(Type type);
    }
}
