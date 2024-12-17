using GitObjectDb.Api.GraphQL.Model;
using GitObjectDb.Api.GraphQL.Tools;
using GitObjectDb.Model;
using GitObjectDb.Tools;
using LibGit2Sharp;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace GitObjectDb.Api.GraphQL.GraphModel;

/// <summary>Emits data transfer types from a <see cref="IDataModel"/>.</summary>
public sealed class NodeInputDtoTypeEmitter
{
    /// <summary>Initializes a new instance of the <see cref="NodeInputDtoTypeEmitter"/> class.</summary>
    /// <param name="model">The model for which data transfer types must be emitted.</param>
    public NodeInputDtoTypeEmitter(IDataModel model)
    {
        Model = model;
        var assemblyName = new AssemblyName($"{nameof(NodeInputDtoTypeEmitter)}{Guid.NewGuid()}");
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = AssemblyBuilder.DefineDynamicModule(nameof(NodeInputDtoTypeEmitter));
        TypeToInputDto = EmitTypes();
    }

    /// <summary>Gets the model for which data transfer types must be emitted.</summary>
    public IDataModel Model { get; }

    /// <summary>Gets the list of produced descriptions..</summary>
    public IReadOnlyDictionary<Type, TypeInfo> TypeToInputDto { get; }

    /// <summary>Gets the assembly builder used to emit types.</summary>
    public AssemblyBuilder AssemblyBuilder { get; }

    /// <summary>Gets the module builder used to emit types.</summary>
    private ModuleBuilder ModuleBuilder { get; }

    private ImmutableDictionary<Type, TypeInfo> EmitTypes()
    {
        var result = ImmutableDictionary.CreateBuilder<Type, TypeInfo>();
        foreach (var (type, dto) in Model.NodeTypes.Select(EmitDto))
        {
            EmitDtoProperties(type, dto);

            result[type.Type] = dto.CreateTypeInfo()!;
        }
        return result.ToImmutable();
    }

    private (NodeTypeDescription Type, TypeBuilder Dto) EmitDto(NodeTypeDescription type)
    {
        var result = ModuleBuilder.DefineType($"{typeof(NodeInputDto).Namespace}.{GetTypeName(type.Type)}InputDto",
                                              TypeAttributes.Public |
                                              TypeAttributes.Class |
                                              TypeAttributes.AutoClass |
                                              TypeAttributes.AnsiClass |
                                              TypeAttributes.BeforeFieldInit |
                                              TypeAttributes.AutoLayout,
                                              typeof(NodeInputDto<>).MakeGenericType(type.Type));

        return (type, result);
    }

    /// <summary>Converts the <see cref="Type"/> to its corresponding string representation.</summary>
    /// <param name="type">The type to be converted.</param>
    /// <returns>The string representation.</returns>
    public static string GetTypeName(Type type) =>
        type.IsGenericType ?
        $"{type.Name}`{string.Join(",", type.GetGenericArguments().Select(GetTypeName))}" :
        type.Name;

    private static void EmitDtoProperties(NodeTypeDescription type,
                                          TypeBuilder result)
    {
        var properties = typeof(NodeInputDto).GetProperties();
        var names = properties.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var property in type.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (names.Contains(property.Name))
            {
                continue;
            }
            var adaptedType = AdaptDtoPropertyType(property);
            EmitDtoProperty(result, property, adaptedType);
        }
    }

    private static Type AdaptDtoPropertyType(PropertyInfo property)
    {
        if (property.PropertyType.IsAssignableTo(typeof(Node)))
        {
            return typeof(DataPath);
        }
        if (property.PropertyType.IsEnumerable(t => t.IsAssignableTo(typeof(Node)), out var _))
        {
            return typeof(IEnumerable<>).MakeGenericType(typeof(DataPath));
        }
        return property.PropertyType;
    }

    private static void EmitDtoProperty(TypeBuilder result, PropertyInfo property, Type newTargetType)
    {
        var fieldBuilder = result.DefineField($"_{property.Name}",
                                              newTargetType,
                                              FieldAttributes.Private);
        var propertyBuilder = result.DefineProperty(property.Name,
                                                    PropertyAttributes.HasDefault,
                                                    newTargetType,
                                                    null);

        EmitDtoPropertyGetter(result, property, newTargetType, fieldBuilder, propertyBuilder);
        EmitDtoPropertySetter(result, property, newTargetType, fieldBuilder, propertyBuilder);
    }

    private static void EmitDtoPropertyGetter(TypeBuilder result,
                                              PropertyInfo property,
                                              Type newTargetType,
                                              FieldInfo field,
                                              PropertyBuilder propertyBuilder)
    {
        var getter = result.DefineMethod($"get_{property.Name}",
                                         MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                                         newTargetType,
                                         Type.EmptyTypes);

        var getterIL = getter.GetILGenerator();
        getterIL.Emit(OpCodes.Ldarg_0);
        getterIL.Emit(OpCodes.Ldfld, field);
        getterIL.Emit(OpCodes.Ret);

        propertyBuilder.SetGetMethod(getter);
    }

    private static void EmitDtoPropertySetter(TypeBuilder result,
                                              PropertyInfo property,
                                              Type newTargetType,
                                              FieldInfo field,
                                              PropertyBuilder propertyBuilder)
    {
        var setter = result.DefineMethod($"set_{property.Name}",
                                         MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                                         null,
                                         new[] { newTargetType });

        var setterIL = setter.GetILGenerator();
        setterIL.Emit(OpCodes.Ldarg_0);
        setterIL.Emit(OpCodes.Ldarg_1);
        setterIL.Emit(OpCodes.Stfld, field);
        setterIL.Emit(OpCodes.Ret);

        propertyBuilder.SetSetMethod(setter);
    }
}