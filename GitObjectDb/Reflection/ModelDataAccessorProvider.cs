using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <inheritdoc />
    internal class ModelDataAccessorProvider : IModelDataAccessorProvider
    {
        readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelDataAccessorProvider"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ModelDataAccessorProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public IModelDataAccessor Get(Type type) => new ModelDataAccessor(_serviceProvider, type);
    }
}
