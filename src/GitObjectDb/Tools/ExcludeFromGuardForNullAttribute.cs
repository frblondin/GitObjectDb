using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tools
{
    /// <summary>
    /// When applied on a type/member, indicates that the guard for null unit tests
    /// should not be verified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    [ExcludeFromGuardForNull]
    internal sealed class ExcludeFromGuardForNullAttribute : Attribute
    {
    }
}
