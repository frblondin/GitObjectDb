using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Tools
{
    internal static class TypeHelper
    {
        private static readonly ConcurrentDictionary<Type, IEnumerable<Type>> _derivedTypesIncludingSelfCache = new();

        internal static int GetAssemblyDelimiterIndex(string fullTypeName)
        {
            var level = 0;
            for (var i = 0; i < fullTypeName.Length; i++)
            {
                switch (fullTypeName[i])
                {
                    // Manage nested generic type args, if any
                    case '[':
                        level++;
                        break;
                    case ']':
                        level--;
                        break;
                    case ',' when level == 0:
                        return i;
                }
            }
            throw new NotSupportedException("Assembly delimiter could not be found in full type name.");
        }

        internal static IEnumerable<Type> GetDerivedTypesIncludingSelf(Type root) =>
            _derivedTypesIncludingSelfCache.GetOrAdd(
                root,
                type =>
                (from a in AppDomain.CurrentDomain.GetAssemblies()
                 from t in a.GetTypes()
                 where type.IsAssignableFrom(t)
                 select t).ToList().AsReadOnly());
    }
}
