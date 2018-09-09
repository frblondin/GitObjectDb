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
    internal class CreatorParameterFromServiceProviderResolver : ICreatorParameterResolver
    {
        readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatorParameterFromServiceProviderResolver"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <exception cref="ArgumentNullException">serviceProvider</exception>
        public CreatorParameterFromServiceProviderResolver(IServiceProvider serviceProvider) =>
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        /// <inheritdoc/>
        public bool CanResolve(JsonProperty property, Type instanceType) =>
            _serviceProvider.GetService(property.PropertyType) != null;

        /// <inheritdoc/>
        public object Resolve(MetadataObjectConverter converter, JsonProperty property, Type instanceType) =>
            _serviceProvider.GetService(property.PropertyType);
    }
}
