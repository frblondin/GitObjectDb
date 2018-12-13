using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <inheritdoc />
    internal class ModelDataAccessorProvider : IModelDataAccessorProvider
    {
        private readonly ModelDataAccessorFactory _modelDataAccessorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelDataAccessorProvider"/> class.
        /// </summary>
        /// <param name="modelDataAccessorFactory">The <see cref="IModelDataAccessor"/> factory.</param>
        public ModelDataAccessorProvider(ModelDataAccessorFactory modelDataAccessorFactory)
        {
            _modelDataAccessorFactory = modelDataAccessorFactory ?? throw new ArgumentNullException(nameof(modelDataAccessorFactory));
        }

        /// <inheritdoc />
        public IModelDataAccessor Get(Type type) => _modelDataAccessorFactory(type);
    }
}
