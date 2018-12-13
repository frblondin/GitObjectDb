using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.JsonConverters
{
    /// <summary>
    /// Creates a new instance of <see cref="ModelObjectContractResolver"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The newly created instance.</returns>
    internal delegate ModelObjectContractResolver ModelObjectContractResolverFactory(ModelObjectSerializationContext context);

    /// <summary>
    /// Resolves member mappings for a type, camel casing property names, and processing special repository
    /// object types such as children defined in nested files.
    /// </summary>
    /// <seealso cref="CamelCasePropertyNamesContractResolver"/>
    internal class ModelObjectContractResolver : CamelCasePropertyNamesContractResolver
    {
        private readonly ModelObjectContractCache _modelObjectContractCache;
        private readonly ModelObjectSerializationContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelObjectContractResolver"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="modelObjectContractCache">The model object constract cache.</param>
        [ActivatorUtilitiesConstructor]
        public ModelObjectContractResolver(ModelObjectSerializationContext context,
            ModelObjectContractCache modelObjectContractCache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _modelObjectContractCache = modelObjectContractCache ?? throw new ArgumentNullException(nameof(modelObjectContractCache));
            Serializer = JsonSerializerProvider.Create(this);
        }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public JsonSerializer Serializer { get; }

        /// <inheritdoc/>
        public override JsonContract ResolveContract(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _modelObjectContractCache.GetContract(type, _context);
        }
    }
}
