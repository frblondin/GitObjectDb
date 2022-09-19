using System;

namespace GitObjectDb.Tools
{
    internal static class TypeHelper
    {
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
    }
}
