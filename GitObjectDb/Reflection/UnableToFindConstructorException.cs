using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Reflection
{
    public class UnableToFindConstructorException : Exception
    {
        public UnableToFindConstructorException(Type type) :
            base($"Unable to find a working constructor for type '{type}'. This often indicate that some parameters are not managed by the service provider and no property matching parameter name/types could be found.")
        {

        }
    }
}
