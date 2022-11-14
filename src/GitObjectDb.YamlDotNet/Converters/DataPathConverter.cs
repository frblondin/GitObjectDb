using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace GitObjectDb.YamlDotNet.Converters;

internal class DataPathConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(DataPath);

    public object? ReadYaml(IParser parser, Type type)
    {
        var value = parser.Consume<Scalar>().Value;
        return DataPath.Parse(value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var path = (DataPath)value!;
        var scalar = new Scalar(AnchorName.Empty,
                                TagName.Empty,
                                path.ToString()!,
                                ScalarStyle.Any,
                                true,
                                false);
        emitter.Emit(scalar);
    }
}
