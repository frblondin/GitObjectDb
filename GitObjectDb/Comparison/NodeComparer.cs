using Fasterflect;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Comparison
{
    internal static class NodeComparer
    {
        private static readonly CompareLogic _compareLogic = new CompareLogic(
            new ComparisonConfig
            {
                MaxDifferences = int.MaxValue,
            });

        internal delegate bool ConflictResolver(PropertyInfo property, object ancestorValue, object ourValue, object theirValue, out object result);

        internal static ComparisonResult Compare(object expectedObject, object actualObject)
        {
            return _compareLogic.Compare(expectedObject, actualObject);
        }

        internal static IEnumerable<PropertyInfo> GetProperties(Type type) =>
            type.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite);

        internal static IEnumerable<NodeMergeChange> CollectChanges(NodeChanges localChanges, NodeChanges toBeMergedIntoLocal, NodeMergerPolicy policy, bool isRebase)
        {
            foreach (var their in toBeMergedIntoLocal)
            {
                switch (their.Status)
                {
                    case NodeChangeStatus.Edit:
                        yield return new NodeMergeChange(policy)
                        {
                            Ancestor = their.Old,
                            Ours = localChanges.Modified.FirstOrDefault(c => their.Old.Path.Equals((Path)c.Old.Path))?.New,
                            Theirs = their.New,
                            OurRootDeletedParent = (from c in localChanges.Deleted
                                                    where their.Old.Path.FolderPath.StartsWith(c.Old.Path.FolderPath)
                                                    orderby c.Old.Path.FolderPath.Length ascending
                                                    select c.Old).FirstOrDefault(),
                        }.Initialize();
                        break;
                    case NodeChangeStatus.Add:
                        yield return new NodeMergeChange(policy)
                        {
                            Ours = localChanges.Added.FirstOrDefault(c => c.New.Path == their.New.Path)?.New,
                            Theirs = their.New,
                            OurRootDeletedParent = (from c in localChanges.Deleted
                                                    where their.Old.Path.FolderPath.StartsWith(c.Old.Path.FolderPath)
                                                    orderby c.Old.Path.FolderPath.Length ascending
                                                    select c.Old).FirstOrDefault(),
                        }.Initialize();
                        break;
                    case NodeChangeStatus.Delete:
                        yield return new NodeMergeChange(policy)
                        {
                            Ancestor = their.Old,
                            TheirRootDeletedParent = (from c in toBeMergedIntoLocal.Deleted
                                                      where c.Old.Path.FolderPath.StartsWith(their.Old.Path.FolderPath)
                                                      orderby their.Old.Path.FolderPath.Length ascending
                                                      select c.Old).FirstOrDefault(),
                        }.Initialize();

                        // Node or child node added in our changes... ?
                        if (localChanges.Added.Any(c => c.New.Path.DataPath.StartsWith(their.Old.Path.FolderPath)))
                        {
                            throw new NotImplementedException();
                        }
                        break;
                }
            }
        }
    }
}
