using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// Hack for accessing LibGit2Sharp internal methods.
    /// </summary>
    internal static class LibGit2SharpHackExtensions
    {
        static readonly MethodInfo _referenceCollectionMoveHeadTarget =
            (from m in typeof(ReferenceCollection).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
             where m.Name == "MoveHeadTarget" && m.IsGenericMethodDefinition
             select m).Single();

        /// <summary>
        /// Moves the head target.
        /// </summary>
        /// <typeparam name="T">Type of target.</typeparam>
        /// <param name="references">The references.</param>
        /// <param name="target">The target.</param>
        /// <returns>The new <see cref="Reference"/>.</returns>
        internal static Reference MoveHeadTarget<T>(this ReferenceCollection references, T target)
        {
            var method = _referenceCollectionMoveHeadTarget.MakeGenericMethod(typeof(T));
            return (Reference)method.Invoke(references, new object[] { target });
        }
    }
}
