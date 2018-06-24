using System;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    internal static class StackExtensions
    {
        internal static string ToPath(this Stack<string> stack) =>
            string.Join("/", stack.Reverse());
    }
}
