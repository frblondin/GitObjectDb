using GitObjectDb.Tools;
using GitObjectDb.YamlDotNet.Core;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace GitObjectDb.YamlDotNet.Model;

[TypeConverter(typeof(NodeReferenceFakeConverter))]
internal class NodeReference : IValuePromise
{
    public NodeReference()
    {
        NodeReferenceParser.TryGetCurrentInstance()?.ReferencesToBeResolved.Add(this);
    }

    public event Action<object?>? ValueAvailable;

    public bool AlreadyResolved { get; private set; }

    public DataPath? Path { get; set; }

    public Node? Reference { get; private set; }

    public void ResolveReference()
    {
        if (AlreadyResolved)
        {
            throw new InvalidOperationException("Reference already set.");
        }
        AlreadyResolved = true;
        var currentParser = NodeReferenceParser.CurrentInstance;
        if (Path is not null)
        {
            Reference = currentParser.Nodes.FirstOrDefault(n => Path.Equals(n.Path)) ??
                (Node)currentParser.ReferenceResolver.Invoke(Path);

            ValueAvailable?.Invoke(Reference);
        }
    }

    public class NodeReferenceFakeConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
            destinationType.IsNode();

        public override object ConvertTo(ITypeDescriptorContext context,
                                         CultureInfo culture,
                                         object value,
                                         Type destinationType) => value;
    }
}
