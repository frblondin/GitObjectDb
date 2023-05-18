using Fasterflect;
using GitObjectDb.Tools;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Comparison;
internal class NodeComparerLimitedToPath : ClassComparer
{
    private static readonly ConcurrentDictionary<Type, IEnumerable<PropertyInfo>> _cache = new();

    public NodeComparerLimitedToPath(RootComparer rootComparer)
        : base(rootComparer)
    {
    }

    public override bool IsTypeMatch(Type? type1, Type? type2) =>
        (type1 is not null && GetNodeReferences(type1).Any()) &&
        (type2 is not null && GetNodeReferences(type2).Any());

    public override void CompareType(CompareParms parms)
    {
        base.CompareType(parms);

        CompareReferencedNodePathChanges(parms);
    }

    private void CompareReferencedNodePathChanges(CompareParms parms)
    {
        try
        {
            parms.Result.AddParent(parms.Object1);
            parms.Result.AddParent(parms.Object2);
            if (parms.Object1 is IEnumerable || parms.Object1 != parms.Object2)
            {
                var type1 = parms.Object1.GetType();
                var type2 = parms.Object2.GetType();
                if (!ExcludeLogic.ShouldExcludeClass(parms.Config, type1, type2))
                {
                    parms.Object1Type = type1;
                    parms.Object2Type = type2;
                    PerformReferencedNodeCompareProperties(parms);
                }
            }
        }
        finally
        {
            parms.Result.RemoveParent(parms.Object1);
            parms.Result.RemoveParent(parms.Object2);
        }
    }

    private void PerformReferencedNodeCompareProperties(CompareParms parms)
    {
        var currentProperties1 = GetNodeReferences(parms.Object1Type);
        var currentProperties2 = GetNodeReferences(parms.Object2Type);
        foreach (var property in currentProperties1)
        {
            CompareReferencedNodeProperty(parms, property, currentProperties2);
            if (parms.Result.ExceededDifferences)
            {
                break;
            }
        }
    }

    private void CompareReferencedNodeProperty(CompareParms parms, PropertyInfo info, IEnumerable<PropertyInfo> object2Properties)
    {
        if (!info.CanRead || (!parms.Config.CompareReadOnly && !info.CanWrite))
        {
            return;
        }
        var secondObjectInfo = GetSecondObjectInfo(info, object2Properties);
        if ((parms.Config.IgnoreObjectTypes || parms.Config.IgnoreConcreteTypes) &&
            secondObjectInfo is null &&
            parms.Config.IgnoreMissingProperties)
        {
            return;
        }
        if (info.PropertyType.IsNodeEnumerable(out var _))
        {
            var values1 = (IEnumerable<Node>?)Reflect.PropertyGetter(info).Invoke(parms.Object1);
            var values2 = (IEnumerable<Node>?)(secondObjectInfo is not null ? Reflect.PropertyGetter(secondObjectInfo).Invoke(parms.Object2) : null);
            RootComparer.Compare(new CompareParms
            {
                Result = parms.Result,
                Config = parms.Config,
                ParentObject1 = parms.Object1,
                ParentObject2 = parms.Object2,
                Object1 = values1?.Select(n => n.Path),
                Object2 = values2?.Select(n => n.Path),
                BreadCrumb = AddBreadCrumb(parms.Config, parms.BreadCrumb, info.Name),
                Object1DeclaredType = typeof(IEnumerable<DataPath>),
                Object2DeclaredType = typeof(IEnumerable<DataPath>),
            });
        }
        else
        {
            var node1 = (Node?)Reflect.PropertyGetter(info).Invoke(parms.Object1);
            var node2 = (Node?)(secondObjectInfo is not null ? Reflect.PropertyGetter(secondObjectInfo).Invoke(parms.Object2) : null);
            RootComparer.Compare(new CompareParms
            {
                Result = parms.Result,
                Config = parms.Config,
                ParentObject1 = parms.Object1,
                ParentObject2 = parms.Object2,
                Object1 = node1?.Path,
                Object2 = node2?.Path,
                BreadCrumb = AddBreadCrumb(parms.Config, parms.BreadCrumb, info.Name),
                Object1DeclaredType = typeof(DataPath),
                Object2DeclaredType = typeof(DataPath),
            });
        }
    }

    private static PropertyInfo? GetSecondObjectInfo(PropertyInfo info, IEnumerable<PropertyInfo> object2Properties) =>
        object2Properties.FirstOrDefault(p => p.Name == info.Name);

    private static IEnumerable<PropertyInfo> GetNodeReferences(Type type) => _cache.GetOrAdd(type, static t =>
        (from p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
         where p.PropertyType.IsNode() || p.PropertyType.IsNodeEnumerable(out var _)
         select p).ToList());
}
