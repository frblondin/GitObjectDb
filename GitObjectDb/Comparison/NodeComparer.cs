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

        internal static object HasConflicts(object ancestor, object ours, object theirs, NodeMergerPolicy policy = null)
        {
            if (policy == null)
            {
                policy = NodeMergerPolicy.Default;
            }
            var type = ancestor.GetType();
            var result = Reflect.Constructor(type).Invoke();
            foreach (var property in GetProperties(type))
            {
                var getter = Reflect.PropertyGetter(property);
                var ancestorValue = getter(ancestor);
                var ourValue = getter(ours);
                var theirValue = getter(theirs);
                if (!_compareLogic.Compare(ourValue, theirValue).AreEqual &&
                    !_compareLogic.Compare(ancestorValue, ourValue).AreEqual &&
                    !_compareLogic.Compare(ancestorValue, theirValue).AreEqual)
                {
                    return true;
                }
            }
            return false;
        }

        internal static object Merge(object ancestor, object ours, object theirs, ConflictResolver resolver, NodeMergerPolicy policy = null)
        {
            if (policy == null)
            {
                policy = NodeMergerPolicy.Default;
            }
            var type = ancestor.GetType();
            var result = Reflect.Constructor(type).Invoke();
            foreach (var property in GetProperties(type))
            {
                var getter = Reflect.PropertyGetter(property);
                if (!TryMergePropertyValue(property, getter(ancestor), getter(ours), getter(theirs), resolver, out var mergedValue))
                {
                    return null;
                }

                var setter = Reflect.PropertySetter(property);
                setter(result, mergedValue);
            }
            return result;
        }

        internal static IEnumerable<PropertyInfo> GetProperties(Type type) =>
            type.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite);

        private static bool TryMergePropertyValue(PropertyInfo property, object ancestorValue, object ourValue, object theirValue, ConflictResolver resolver, out object result)
        {
            if (_compareLogic.Compare(ourValue, theirValue).AreEqual)
            {
                result = ourValue;
            }
            else if (_compareLogic.Compare(ancestorValue, ourValue).AreEqual)
            {
                result = theirValue;
            }
            else if (_compareLogic.Compare(ancestorValue, theirValue).AreEqual)
            {
                result = ancestorValue;
            }
            else
            {
                return resolver(property, ancestorValue, ourValue, theirValue, out result);
            }
            return true;
        }

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
                        foreach (var ourAddition in localChanges.Added.Where(c => c.New.Path.DataPath.StartsWith(their.Old.Path.FolderPath)))
                        {
                            throw new NotImplementedException();
                        }
                        break;
                }
            }
        }
    }
}
