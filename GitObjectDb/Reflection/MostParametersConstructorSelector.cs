using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Reflection
{
    internal class MostParametersConstructorSelector : IConstructorSelector
    {
        public ConstructorParameterBinding SelectConstructorBinding(Type type, ConstructorParameterBinding[] constructorBindings)
        {
            if (constructorBindings == null) throw new ArgumentNullException(nameof(constructorBindings));
            var result = (from c in constructorBindings
                          orderby c.Parameters.Length descending
                          select c).FirstOrDefault();
            return result ?? throw new UnableToFindConstructorException(type);
        }
    }
}
