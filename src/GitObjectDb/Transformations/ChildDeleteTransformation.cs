using GitObjectDb.Models;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static GitObjectDb.Transformations.PropertyTransformation;

namespace GitObjectDb.Transformations
{
    internal partial class ChildDeleteTransformation : ITransformation
    {
        internal ChildDeleteTransformation(IModelObject child)
            : this(child.GetDataPath(), child.Id)
        {
        }

        internal ChildDeleteTransformation(string path, UniqueId childId)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            ChildId = childId;
        }

        /// <inheritdoc/>
        public string Path { get; }

        /// <summary>
        /// Gets the child id to delete.
        /// </summary>
        public UniqueId ChildId { get; }
    }
}
