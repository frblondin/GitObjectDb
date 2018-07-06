using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Selects the constructor with the most parameters.
    /// </summary>
    internal class MostParametersConstructorSelector : IConstructorSelector
    {
        /// <inheritdoc />
        public ConstructorParameterBinding SelectConstructorBinding(Type type, IEnumerable<ConstructorParameterBinding> constructorBindings)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            if (constructorBindings == null)
            {
                throw new ArgumentNullException(nameof(constructorBindings));
            }

            var result = (from c in constructorBindings
                          orderby c.Parameters.Count descending
                          select c).FirstOrDefault();
            return result ?? throw new UnableToFindConstructorException(type);
        }
    }
}
