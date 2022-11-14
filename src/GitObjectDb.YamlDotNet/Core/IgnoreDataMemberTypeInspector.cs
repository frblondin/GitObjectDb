using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace GitObjectDb.YamlDotNet.Core;
internal class IgnoreDataMemberTypeInspector : TypeInspectorSkeleton
{
    private readonly ITypeInspector _innerTypeDescriptor;

    public IgnoreDataMemberTypeInspector(ITypeInspector innerTypeDescriptor)
    {
        _innerTypeDescriptor = innerTypeDescriptor;
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) => _innerTypeDescriptor
        .GetProperties(type, container)
        .Where(p => p.GetCustomAttribute<IgnoreDataMemberAttribute>() == null);
}
