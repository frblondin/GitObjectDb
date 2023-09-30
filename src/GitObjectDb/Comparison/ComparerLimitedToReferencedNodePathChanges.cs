using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using System;

namespace GitObjectDb.Comparison;
internal class ComparerLimitedToReferencedNodePathChanges : ClassComparer
{
    public ComparerLimitedToReferencedNodePathChanges(RootComparer rootComparer)
        : base(rootComparer)
    {
    }

    public override bool IsTypeMatch(Type? type1, Type? type2) =>
        (type1 is not null && typeof(Node).IsAssignableFrom(type1)) &&
        (type2 is not null && typeof(Node).IsAssignableFrom(type2));

    public override void CompareType(CompareParms parms)
    {
        if (parms.ParentObject1 is null && parms.ParentObject2 is null)
        {
            base.CompareType(parms);
        }
        else
        {
            RootComparer.Compare(new CompareParms
            {
                Result = parms.Result,
                Config = parms.Config,
                ParentObject1 = parms.Object1,
                ParentObject2 = parms.Object2,
                Object1 = (parms.Object1 as Node)?.Path,
                Object2 = (parms.Object2 as Node)?.Path,
                BreadCrumb = AddBreadCrumb(parms.Config, parms.BreadCrumb, nameof(Node.Path)),
                Object1DeclaredType = typeof(DataPath),
                Object2DeclaredType = typeof(DataPath),
            });
        }
    }
}
