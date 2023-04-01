using GitObjectDb.Model;
using GitObjectDb.Tools;
using LibGit2Sharp;
using System.Reflection;
using System.Reflection.Emit;

namespace GitObjectDb.Api.OData.Model;

/// <summary>Emits data transfer types from a <see cref="IDataModel"/>.</summary>
internal sealed class DtoTypeEmitter
{
    /// <summary>Initializes a new instance of the <see cref="DtoTypeEmitter"/> class.</summary>
    /// <param name="model">The model for which data transfer types must be emitted.</param>
    public DtoTypeEmitter(IDataModel model)
    {
        Model = model;
        var assemblyName = new AssemblyName($"{nameof(DtoTypeEmitter)}{Guid.NewGuid()}");
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = AssemblyBuilder.DefineDynamicModule(nameof(DtoTypeEmitter));
        TypeDescriptions = EmitTypes();
    }

    /// <summary>Gets the model for which data transfer types must be emitted.</summary>
    public IDataModel Model { get; }

    /// <summary>Gets the list of produced descriptions..</summary>
    public IList<DataTransferTypeDescription> TypeDescriptions { get; }

    /// <summary>Gets the assembly builder used to emit types.</summary>
    public AssemblyBuilder AssemblyBuilder { get; }

    /// <summary>Gets the module builder used to emit types.</summary>
    private ModuleBuilder ModuleBuilder { get; }

    private IList<DataTransferTypeDescription> EmitTypes()
    {
        var result = new List<DataTransferTypeDescription>();
        var dtoTypes = Model.NodeTypes
            .Where(IsBrowsable)
            .Select(EmitDto)
            .ToList();
        foreach (var (type, dto) in dtoTypes)
        {
            AddDtoDescription(type, dto);
            EmitDtoFromNodeConstructor(dto);
            EmitDtoProperties(type, dto,
                t => dtoTypes.FirstOrDefault(i => i.Type.Type == t).Dto ??
                throw new NotSupportedException($"Could not find dto type for type {t}."));

            var dtoDescription = new DataTransferTypeDescription(type, dto.CreateTypeInfo()!);
            result.Add(dtoDescription);
        }
        return result.AsReadOnly();
    }

    /// <summary>Gets whether the type description is API browsable.</summary>
    /// <param name="description">The node type description.</param>
    /// <returns><c>true</c> if the node is browsable, <c>false</c> otherwise.</returns>
    public static bool IsBrowsable(NodeTypeDescription description) =>
        description.Type.GetCustomAttribute<ApiBrowsableAttribute>()?.Browsable ?? true;

    private (NodeTypeDescription Type, TypeBuilder Dto) EmitDto(NodeTypeDescription type)
    {
        var result = ModuleBuilder.DefineType($"{typeof(DtoTypeEmitter).Namespace}.{GetTypeName(type.Type)}DTO",
                                              TypeAttributes.Public |
                                              TypeAttributes.Class |
                                              TypeAttributes.AutoClass |
                                              TypeAttributes.AnsiClass |
                                              TypeAttributes.BeforeFieldInit |
                                              TypeAttributes.AutoLayout,
                                              typeof(NodeDto));

        return (type, result);
    }

    /// <summary>Converts the <see cref="Type"/> to its corresponding string representation.</summary>
    /// <param name="type">The type to be converted.</param>
    /// <returns>The string representation.</returns>
    public static string GetTypeName(Type type) =>
        type.IsGenericType ?
        $"{type.Name}`{string.Join(",", type.GetGenericArguments().Select(GetTypeName))}" :
        type.Name;

    private static void AddDtoDescription(NodeTypeDescription type, TypeBuilder result)
    {
        result.SetCustomAttribute(
            new CustomAttributeBuilder(
                typeof(DtoDescriptionAttribute).GetConstructors().Single(),
                new object[] { type.Type, type.Name }));
    }

    private static void EmitDtoFromNodeConstructor(TypeBuilder result)
    {
        var parameters = new[] { typeof(Node), typeof(ObjectId) };
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var baseConstructor = typeof(NodeDto).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                                                             null,
                                                             parameters,
                                                             null)!;
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var constructor = result.DefineConstructor(MethodAttributes.Public,
                                                   CallingConventions.Standard | CallingConventions.HasThis,
                                                   parameters);
        var ilGenerator = constructor.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Ldarg_1);
        ilGenerator.Emit(OpCodes.Ldarg_2);
        ilGenerator.Emit(OpCodes.Call, baseConstructor);
        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void EmitDtoProperties(NodeTypeDescription type,
                                          TypeBuilder result,
                                          Func<Type, TypeBuilder> nodeToDto)
    {
        var properties = typeof(NodeDto).GetProperties();
        var names = properties.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var property in type.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (names.Contains(property.Name))
            {
                continue;
            }
            var adaptedType = AdaptDtoPropertyType(property, nodeToDto);
            EmitDtoProperty(result, property, adaptedType);
        }
    }

    private static Type AdaptDtoPropertyType(PropertyInfo property, Func<Type, TypeBuilder> nodeToDto)
    {
        if (property.PropertyType.IsAssignableTo(typeof(Node)))
        {
            return nodeToDto(property.PropertyType);
        }
        if (property.PropertyType.IsEnumerable(t => t.IsAssignableTo(typeof(Node)), out var type))
        {
            var dtoType = nodeToDto(type!);
            return typeof(IEnumerable<>).MakeGenericType(dtoType);
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