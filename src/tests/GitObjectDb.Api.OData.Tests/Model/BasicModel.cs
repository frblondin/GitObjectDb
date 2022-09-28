using FakeItEasy;
using GitObjectDb.Model;
using System;
using System.Linq;

namespace GitObjectDb.Api.OData.Tests.Model;

public class BasicModel
{
    public static IDataModel CreateDataModel(params Type[] types)
    {
        var model = A.Fake<IDataModel>(o => o.Strict());
        A.CallTo(() => model.NodeTypes).Returns(types.Select(t => new NodeTypeDescription(t, t.Name)));
        return model;
    }

    public record SimpleNode : Node
    {
        public string Name { get; init; }
    }

    public record SingleReferenceNode : Node
    {
        public SimpleNode SingleReference { get; init; }
    }

    public record MultiReferenceNode : Node
    {
        public SimpleNode[] MultiReference { get; init; }
    }

    [ApiBrowsable(false)]
    public record NotBrowsableNode : Node
    {
    }
}