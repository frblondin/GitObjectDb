using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.JsonConverters
{
    /// <summary>
    /// Resolves creator parameter values while deserializing instances.
    /// </summary>
    internal interface ICreatorParameterResolver
    {
        /// <summary>
        /// Determines whether this instance can resolve the specified json property.
        /// </summary>
        /// <param name="property">The json property.</param>
        /// <param name="instanceType">Type of the instance.</param>
        /// <returns>
        ///   <c>true</c> if this instance can resolve the specified json property; otherwise, <c>false</c>.
        /// </returns>
        bool CanResolve(JsonProperty property, Type instanceType);

        /// <summary>
        /// Resolves the specified json property.
        /// </summary>
        /// <param name="converter">The model object converter.</param>
        /// <param name="property">The json property.</param>
        /// <param name="instanceType">Type of the instance.</param>
        /// <returns>The resolved value.</returns>
        object Resolve(ModelObjectConverter converter, JsonProperty property, Type instanceType);
    }
}
