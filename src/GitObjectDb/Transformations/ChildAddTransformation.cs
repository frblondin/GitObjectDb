using GitObjectDb.Models;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static GitObjectDb.Transformations.PropertyTransformation;

namespace GitObjectDb.Transformations
{
    internal partial class ChildAddTransformation : ITransformation
    {
        internal ChildAddTransformation(IModelObject parent, Expression propertyPicker, IModelObject child)
            : this(parent.Id, parent.GetDataPath(), ExtractProperty(parent.DataAccessor, propertyPicker).FolderName, child)
        {
        }

        internal ChildAddTransformation(UniqueId parentId, string path, string propertyName, IModelObject child)
        {
            InstanceId = parentId;
            Path = path ?? throw new ArgumentNullException(nameof(path));
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            Child = child ?? throw new ArgumentNullException(nameof(child));
        }

        /// <summary>
        /// Gets the instance <see cref="UniqueId"/>.
        /// </summary>
        public UniqueId InstanceId { get; }

        /// <inheritdoc/>
        public string Path { get; }

        /// <summary>
        /// Gets the property that will have the value modified.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the child to add or remove.
        /// </summary>
        public IModelObject Child { get; }

        private static ChildPropertyInfo ExtractProperty(IModelDataAccessor dataAccessor, Expression propertyPicker)
        {
            if (propertyPicker == null)
            {
                throw new ArgumentNullException(nameof(propertyPicker));
            }

            var property = PropertyVisitor.ExtractProperty(propertyPicker);
            var result = dataAccessor.ChildProperties.FirstOrDefault(p => p.Property == property);
            return result ?? throw new GitObjectDbException($"Member should be a child property.");
        }
    }
}
