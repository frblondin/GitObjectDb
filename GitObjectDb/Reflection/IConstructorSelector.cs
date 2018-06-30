using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Reflection
{
    internal interface IConstructorSelector
    {
        ConstructorParameterBinding SelectConstructorBinding(Type type, ConstructorParameterBinding[] constructorBindings);
    }
}
