using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Injection
{
    [AttributeUsage(AttributeTargets.Constructor)]
    internal class FactoryDelegateConstructorAttribute : Attribute
    {
        public FactoryDelegateConstructorAttribute(Type delegateType)
        {
            DelegateType = delegateType;
        }

        public Type DelegateType { get; }
    }
}
