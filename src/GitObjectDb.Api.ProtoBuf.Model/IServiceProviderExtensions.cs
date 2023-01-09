using GitObjectDb.Api.ProtoBuf.Model.Surrogates;
using GitObjectDb.Model;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Meta;

namespace GitObjectDb.Api.ProtoBuf.Model;

/// <summary>A set of methods for instances of <see cref="IServiceProviderExtensions"/>.</summary>
public static partial class IServiceProviderExtensions
{
    private static bool _alreadyInitialized;

    internal static INodeSerializer? Serializer { get; set; }

    /// <summary>
    /// Configures default <see cref="RuntimeTypeModel"/> with internal GitObjectDbTypes and
    /// <see cref="IDataModel"/> type surrogates.
    /// </summary>
    /// <param name="serviceProvider">
    /// The <see cref="IServiceProvider"/> containing registered service such as <see cref="IDataModel"/>.
    /// </param>
    public static void ConfigureGitObjectDbProtoRuntimeTypeModel(this IServiceProvider serviceProvider)
    {
        var model = serviceProvider.GetRequiredService<IDataModel>();
        Serializer = serviceProvider.GetRequiredService<INodeSerializer>();

        if (_alreadyInitialized)
        {
            return;
        }
        _alreadyInitialized = true;
        var runtimeModel = RuntimeTypeModel.Default;
        runtimeModel.Add(typeof(ObjectId), false)
             .SetSurrogate(typeof(ObjectIdSurrogate));
        runtimeModel.Add(typeof(DataPath), false)
             .SetSurrogate(typeof(DataPathSurrogate));

        foreach (var nodeType in model.NodeTypes)
        {
            SetSurrogate(nodeType);
        }
    }

    private static void SetSurrogate(NodeTypeDescription nodeType)
    {
        RuntimeTypeModel.Default
            .Add(nodeType.Type, false)
            .SetSurrogate(typeof(NodeSurrogate<>).MakeGenericType(nodeType.Type));
    }
}
