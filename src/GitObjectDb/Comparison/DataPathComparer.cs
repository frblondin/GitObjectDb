using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using System;

namespace GitObjectDb.Comparison;

internal class DataPathComparer : BaseTypeComparer
{
    public DataPathComparer(RootComparer rootComparer)
        : base(rootComparer)
    {
    }

    public override bool IsTypeMatch(Type type1, Type type2) =>
        type1 == typeof(DataPath) && type2 == typeof(DataPath);

    public override void CompareType(CompareParms parms)
    {
        if (parms.Object1 is null || parms.Object2 is null)
        {
            return;
        }

        var path1 = (DataPath)parms.Object1;
        var path2 = (DataPath)parms.Object2;
        if (!path1.Equals(path2))
        {
            AddDifference(parms);
        }
    }
}
