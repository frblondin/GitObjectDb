using GitObjectDb.Attributes;
using GitObjectDb.Models;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.JsonConverters
{
    /// <summary>
    /// Resolves the parameter value from the JToken representing the instance being deserialized.
    /// </summary>
    /// <seealso cref="ICreatorParameterResolver" />
    [ExcludeFromGuardForNull]
    internal class CreatorRepositoryContainerParameterResolver : ICreatorParameterResolver
    {
        /// <inheritdoc/>
        public bool CanResolve(JsonProperty property, Type instanceType) =>
            typeof(IObjectRepositoryContainer).IsAssignableFrom(property.PropertyType);

        /// <inheritdoc/>
        public object Resolve(MetadataObjectConverter converter, JsonProperty property, Type instanceType) =>
            converter.RepositoryContainer;
    }
}
