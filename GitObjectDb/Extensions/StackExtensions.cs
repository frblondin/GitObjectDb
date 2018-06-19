using System;
using System.Linq;
using System.Text;

namespace System.Collections.Generic
{
    public static class StackExtensions
    {
        public static string ToPath(this Stack<string> stack) => string.Join("/", stack.Reverse());
    }
}
