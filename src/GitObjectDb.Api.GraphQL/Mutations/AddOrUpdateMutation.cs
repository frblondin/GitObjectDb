using Fasterflect;
using GitObjectDb.Api.GraphQL.Graph;
using GitObjectDb.Api.GraphQL.Model;
using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Resolvers;
using GraphQLParser.AST;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Mutations;

internal static class AddOrUpdateMutation
{
    internal static IFieldResolver For(NodeTypeDescription description) =>
        (IFieldResolver)Activator.CreateInstance(typeof(AddOrUpdate<>).MakeGenericType(description.Type))!;
}

#pragma warning disable SA1402 // File may only contain a single type
internal class AddOrUpdate<TNode> : IFieldResolver
    where TNode : Node
{
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        var mutationContext = MutationContext.Current.Value = MutationContext.GetCurrent(context);

        try
        {
            var node = GetNodeArgument(context, mutationContext);
            var serializer = mutationContext.QueryAccessor.Serializer;
            var parentPath = node.Path!.IsRootNode ? default : node.Path!.GetParentNode(serializer);
            var result = mutationContext.Transformations.CreateOrUpdate(node, parentPath);

            mutationContext.ModifiedNodesByPath[result.Path!] = result;
            mutationContext.ModifiedNodesById[result.Id!] = result;

            return ValueTask.FromResult((object?)result.Path);
        }
        catch
        {
            mutationContext.AnyException |= true;
            throw;
        }
        finally
        {
            MutationContext.Current.Value = null;
        }
    }

    private static TNode GetNodeArgument(IResolveFieldContext context, MutationContext mutationContext)
    {
        var dto = context.GetArgument<NodeInputDto<TNode>>(Mutation.NodeArgument);
        var modifiedMembers = GetModifiedMembers(context);
        var @new = ConvertDtoToNode(dto, mutationContext, modifiedMembers);
        @new.Path = GetPath(@new, mutationContext.Connection.Model, context, mutationContext);
        var existing = (TNode?)mutationContext.TryResolve(@new.Path);
        return Merge(existing, @new, modifiedMembers);
    }

    private static TNode ConvertDtoToNode(NodeInputDto<TNode> dto,
                                          MutationContext mutationContext,
                                          IEnumerable<GraphQLObjectField> modifiedMembers)
    {
        var dtoType = dto.GetType();
        var result = (TNode)Activator.CreateInstance(typeof(TNode))!;
        foreach (var field in modifiedMembers)
        {
            var property = GetProperty(field);

            if (property is not null && property.CanWrite)
            {
                var value = GetDtoPropertyValue(dto, dtoType, property, mutationContext);
                Reflect.Setter(property).Invoke(result, value);
            }
        }
        return result;
    }

    private static object? GetDtoPropertyValue(NodeInputDto<TNode> dto,
                                               Type dtoType,
                                               PropertyInfo property,
                                               MutationContext mutationContext)
    {
        var value = Reflect.Getter(dtoType, property.Name).Invoke(dto);
        if (property.PropertyType.IsAssignableTo(typeof(Node)))
        {
            value = value is DataPath path ?
                mutationContext.TryResolve(path) :
                null;
        }
        if (property.PropertyType.IsEnumerable(t => t.IsAssignableTo(typeof(Node)), out var _))
        {
            value = value is IEnumerable<DataPath> paths ?
                paths.Select(p => mutationContext.TryResolve(p)).ToList() :
                null;
        }

        return value;
    }

    private static DataPath GetPath(TNode node,
                                    IDataModel model,
                                    IResolveFieldContext context,
                                    MutationContext mutationContext)
    {
        if (node.Path is not null)
        {
            return node.Path;
        }
        var fileExtension = mutationContext.QueryAccessor.Serializer.FileExtension;
        var parentPath = context.GetArgument<DataPath?>(Mutation.ParentPathArgument, default);
        if (parentPath is not null)
        {
            return parentPath.AddChild(node.Id, typeof(TNode), model, fileExtension);
        }
        var parentId = context.GetArgument<UniqueId?>(Mutation.ParentIdArgument, default);
        if (parentId.HasValue)
        {
            var parent = mutationContext.TryResolve(parentId.Value)?.Path ??
                throw new RequestError($"Parent {parentId} could not be found from its identifier.");
            return parent!.AddChild(node.Id, typeof(TNode), model, fileExtension);
        }
        return DataPath.Root(node, model, fileExtension);
    }

    private static List<GraphQLObjectField> GetModifiedMembers(IResolveFieldContext context)
    {
        if (context.FieldAst.Arguments is null ||
            context.FieldDefinition.Arguments is null)
        {
            throw new RequestError("Missing metadata context.");
        }
        var argument = context.FieldAst
            .Arguments
            .FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.Name.StringValue, Mutation.NodeArgument))?
            .Value as GraphQLObjectValue ??
            throw new RequestError("Could not get input values.");
        return argument.Fields ??
            throw new RequestError("Could not get fields.");
    }

    internal static TNode Merge(TNode? original, TNode @new, IEnumerable<GraphQLObjectField> modifiedMembers)
    {
        if (original is null)
        {
            return @new;
        }
        var result = (TNode)Reflect.Method(typeof(TNode), "<Clone>$").Invoke(original);
        foreach (var field in modifiedMembers)
        {
            var property = GetProperty(field);

            if (property is not null && property.CanWrite)
            {
                var newValue = Reflect.Getter(property).Invoke(@new);
                Reflect.Setter(property).Invoke(result, newValue);
            }
        }
        return result;
    }

    private static PropertyInfo? GetProperty(GraphQLObjectField value)
    {
        try
        {
            return typeof(TNode).GetProperty(value.Name.StringValue,
                                             BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        }
        catch (AmbiguousMatchException)
        {
            return typeof(TNode).GetProperty(value.Name.StringValue,
                                             BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance |
                                             BindingFlags.DeclaredOnly);
        }
    }
}
