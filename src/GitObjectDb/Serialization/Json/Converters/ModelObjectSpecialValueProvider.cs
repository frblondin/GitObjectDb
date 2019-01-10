using GitObjectDb.Models;
using GitObjectDb.Reflection;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Serialization.Json.Converters
{
    /// <summary>
    /// Provides value accessor for components being registered in the DI container, lazy children...
    /// </summary>
    internal class ModelObjectSpecialValueProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IModelDataAccessorProvider _dataAccessorProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelObjectSpecialValueProvider"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="modelDataAccessorProvider">The model data accessor provider.</param>
        public ModelObjectSpecialValueProvider(IServiceProvider serviceProvider, IModelDataAccessorProvider modelDataAccessorProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dataAccessorProvider = modelDataAccessorProvider ?? throw new ArgumentNullException(nameof(modelDataAccessorProvider));
        }

        /// <summary>
        /// Gets the injector.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="property">The property.</param>
        /// <returns>The value provider.</returns>
        internal Func<ModelObjectSerializationContext, object> TryGetInjector(Type objectType, JsonProperty property)
        {
            if (property.PropertyType == typeof(IServiceProvider))
            {
                return _ => _serviceProvider;
            }
            if (typeof(ILazyChildren).IsAssignableFrom(property.PropertyType))
            {
                var name = property.PropertyName;
                return context => context.ChildrenResolver?.Invoke(objectType, name) ?? ReturnEmptyChildren(objectType, name);
            }
            if (typeof(IObjectRepositoryContainer).IsAssignableFrom(property.PropertyType))
            {
                return context => context.Container;
            }
            if (_serviceProvider.GetService(property.PropertyType) != null)
            {
                return _ => _serviceProvider.GetService(property.PropertyType);
            }
            return null;
        }

        private ILazyChildren ReturnEmptyChildren(Type parentType, string propertyName)
        {
            var dataAccessor = _dataAccessorProvider.Get(parentType);
            var childProperty = dataAccessor.ChildProperties.TryGetWithValue(p => p.Name, propertyName);
            return LazyChildrenHelper.Create(childProperty, (o, r) => Enumerable.Empty<IModelObject>());
        }
    }
}
