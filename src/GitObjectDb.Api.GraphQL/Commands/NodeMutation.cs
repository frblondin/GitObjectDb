using Fasterflect;
using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Model;
using GraphQL;
using GraphQL.Execution;
using GraphQLParser.AST;
using LibGit2Sharp;
using Models.Organization;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Commands;
internal static partial class NodeMutation
{
    private const string NodeMutationVariableName = $"${nameof(NodeMutation)}";

    internal static Func<IResolveFieldContext<object?>, object?> CreateAddOrUpdate(NodeTypeDescription description)
    {
        var method = ExpressionReflector.GetMethod(() => AddOrUpdate<Node>(default!), returnGenericDefinition: true);
        return new(
            method.MakeGenericMethod(description.Type)
            .CreateDelegate<Func<IResolveFieldContext<object?>, object?>>());
    }

    internal static string Checkout(IResolveFieldContext<object?> context)
    {
        var branch = context.GetArgument<string>(GitObjectDbMutation.BranchArgument);
        GetCurrentContext(context).BranchName = branch;
        return branch;
    }

    internal static DataPath AddOrUpdate<TNode>(IResolveFieldContext<object?> context)
        where TNode : Node
    {
        var mutationContext = Context.Current.Value = GetCurrentContext(context);

        try
        {
            var node = GetNodeArgument<TNode>(context, mutationContext);
            var serializer = mutationContext.QueryAccessor.Serializer;
            var parentPath = node.Path!.IsRootNode ? default : node.Path!.GetParentNode(serializer);
            var result = mutationContext.Transformations.CreateOrUpdate(node, parentPath);

            mutationContext.ModifiedNodesByPath[result.Path!] = result;
            mutationContext.ModifiedNodesById[result.Id!] = result;

            return result.Path!;
        }
        catch
        {
            mutationContext.AnyException |= true;
            throw;
        }
        finally
        {
            Context.Current.Value = null;
        }
    }

    internal static DataPath Delete(IResolveFieldContext<object?> context)
    {
        var mutationContext = GetCurrentContext(context);

        try
        {
            var path = context.GetArgument<DataPath>(GitObjectDbMutation.PathArgument);
            mutationContext.Transformations.Revert(path);

            return path;
        }
        catch
        {
            mutationContext.AnyException |= true;
            throw;
        }
    }

    internal static ObjectId Commit(IResolveFieldContext<object?> context)
    {
        var mutationContext = GetCurrentContext(context);
        try
        {
            var message = context.GetArgument<string>(GitObjectDbMutation.MessageArgument);
            var signature = CreateSignature(context);
            var commit = mutationContext.Transformations.Commit(new(message, signature, signature));
            return commit.Id;
        }
        finally
        {
            mutationContext.Reset();
        }
    }

    private static Context GetCurrentContext(IResolveFieldContext context)
    {
        if (!context.UserContext.TryGetValue(NodeMutationVariableName, out var existing))
        {
            var serviceProvider = context.RequestServices ??
                throw new ExecutionError("No service provider could be found.");
            existing = new Context(serviceProvider);
            context.UserContext[NodeMutationVariableName] = existing;
        }

        var result = (Context)existing!;
        result.ThrowIfAnyException();
        return result;
    }

    private static TNode GetNodeArgument<TNode>(IResolveFieldContext context, Context mutationContext)
        where TNode : Node
    {
        var @new = context.GetArgument<TNode>(GitObjectDbMutation.NodeArgument);
        @new.Path = GetPath(@new, mutationContext.Connection.Model, context, mutationContext);
        var existing = (TNode?)mutationContext.TryResolve(@new.Path);
        return Merge(existing, @new, GetModifiedMembers(context));
    }

    private static DataPath GetPath<TNode>(TNode node, IDataModel model, IResolveFieldContext context, Context mutationContext)
        where TNode : Node
    {
        if (node.Path is not null)
        {
            return node.Path;
        }
        var fileExtension = mutationContext.QueryAccessor.Serializer.FileExtension;
        var parentPath = context.GetArgument<DataPath?>(GitObjectDbMutation.ParentPathArgument, default);
        if (parentPath is not null)
        {
            return parentPath.AddChild(node.Id, typeof(TNode), model, fileExtension);
        }
        var parentId = context.GetArgument<UniqueId?>(GitObjectDbMutation.ParentIdArgument, default);
        if (parentId.HasValue)
        {
            var parent = mutationContext.TryResolve(parentId.Value)?.Path ??
                throw new RequestError($"Parent {parentId} could not be found from its identifier.");
            return parent!.AddChild(node.Id, typeof(TNode), model, fileExtension);
        }
        return DataPath.Root(node, model, fileExtension);
    }

    private static IEnumerable<GraphQLObjectField> GetModifiedMembers(IResolveFieldContext context)
    {
        if (context.FieldAst.Arguments is null ||
            context.FieldDefinition.Arguments is null)
        {
            throw new RequestError("Missing metadata context.");
        }
        var argument = context.FieldAst
            .Arguments
            .FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.Name.StringValue, GitObjectDbMutation.NodeArgument))?
            .Value as GraphQLObjectValue ??
            throw new RequestError("Could not get input values.");
        return argument.Fields ??
            throw new RequestError("Could not get fields.");
    }

    internal static TNode Merge<TNode>(TNode? original, TNode @new, IEnumerable<GraphQLObjectField> modifiedMembers)
        where TNode : Node
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

        static PropertyInfo? GetProperty(GraphQLObjectField value)
        {
            try
            {
                return typeof(TNode).GetProperty(value.Name.StringValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            }
            catch (AmbiguousMatchException)
            {
                return typeof(TNode).GetProperty(value.Name.StringValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            }
        }
    }

    private static Signature CreateSignature(IResolveFieldContext context)
    {
        var name = context.GetArgument<string>(GitObjectDbMutation.AuthorArgument);
        var email = context.GetArgument<string>(GitObjectDbMutation.EMailArgument);
        return new Signature(new Identity(name, email), DateTimeOffset.Now);
    }
}
