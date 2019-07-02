using GitObjectDb.Models.Compare;
using GitObjectDb.Transformations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Transformations
{
    /// <summary>
    /// Translates the changes described in chunk change descriptions.
    /// </summary>
    /// <seealso cref="ITransformation" />
    internal class TransformationFromChunkChanges : IEnumerable<ITransformation>
    {
        private readonly List<ITransformation> _transformations;

        [ActivatorUtilitiesConstructor]
        public TransformationFromChunkChanges(IList<ObjectRepositoryPropertyChange> modifiedChunks, IList<ObjectRepositoryAdd> addedObjects, IList<ObjectRepositoryDelete> deletedObjects)
        {
            if (modifiedChunks == null)
            {
                throw new ArgumentNullException(nameof(modifiedChunks));
            }
            if (addedObjects == null)
            {
                throw new ArgumentNullException(nameof(addedObjects));
            }
            if (deletedObjects == null)
            {
                throw new ArgumentNullException(nameof(deletedObjects));
            }

            var modified = from chunk in modifiedChunks
                           select new PropertyTransformation(chunk.Id, chunk.Path, chunk.Property.Property, chunk.MergeValue);
            var added = from chunk in addedObjects
                        let folderName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(chunk.Path)))
                        select new ChildAddTransformation(chunk.ParentId, chunk.Path, folderName, chunk.Child);
            var deleted = from chunk in deletedObjects
                          let folderName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(chunk.Path)))
                          select new ChildDeleteTransformation(chunk.Path, chunk.Id);
            _transformations = modified.Cast<ITransformation>().Concat(added).Concat(deleted).ToList();
        }

        public IEnumerator<ITransformation> GetEnumerator() => _transformations.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _transformations.GetEnumerator();
    }
}
