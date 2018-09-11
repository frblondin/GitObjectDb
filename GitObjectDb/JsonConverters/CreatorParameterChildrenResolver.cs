using GitObjectDb.Attributes;
using GitObjectDb.Models;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.JsonConverters
{
    /// <summary>
    /// Resolves the parameter value using sub-folder.
    /// </summary>
    /// <seealso cref="ICreatorParameterResolver" />
    [ExcludeFromGuardForNull]
    internal class CreatorParameterChildrenResolver : ICreatorParameterResolver
    {
        /// <inheritdoc/>
        public bool CanResolve(JsonProperty property, Type instanceType) =>
            typeof(ILazyChildren).IsAssignableFrom(property.PropertyType);

        /// <inheritdoc/>
        public object Resolve(MetadataObjectConverter converter, JsonProperty property, Type instanceType) =>
            converter.ChildResolver(instanceType, property.PropertyName);
    }
}
