using ObjectsComparer;
using System;

namespace GitObjectDb.Comparison;
internal class CustomStringComparer : AbstractValueComparer<string>
{
    public CustomStringComparer(bool treatStringEmptyAndNullTheSame, bool ignoreStringLeadingTrailingWhitespace)
    {
        TreatStringEmptyAndNullTheSame = treatStringEmptyAndNullTheSame;
        IgnoreStringLeadingTrailingWhitespace = ignoreStringLeadingTrailingWhitespace;
    }

    public bool TreatStringEmptyAndNullTheSame { get; }

    public bool IgnoreStringLeadingTrailingWhitespace { get; }

    public override bool Compare(string? obj1, string? obj2, ComparisonSettings settings)
    {
        if (obj1 == null && obj2 == null)
        {
            return true;
        }
        if (TreatStringEmptyAndNullTheSame)
        {
            obj1 ??= string.Empty;
            obj2 ??= string.Empty;
        }
        if (IgnoreStringLeadingTrailingWhitespace)
        {
            obj1 = obj1?.Trim();
            obj2 = obj2?.Trim();
        }
        return obj1?.Equals(obj2, StringComparison.Ordinal) ?? false;
    }
}
