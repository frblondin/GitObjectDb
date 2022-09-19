using GitObjectDb.Model;
using LibGit2Sharp;
using System.Reflection;
using System.Reflection.Emit;

namespace GitObjectDb.Api.Model;

/// <summary>Emits data transfer types from a <see cref="IDataModel"/>.</summary>
#pragma warning disable SA1402 // File may only contain a single type
public sealed class DtoTypeEmitter : DtoTypeEmitter<DataTransferTypeDescription>
{
    /// <summary>Initializes a new instance of the <see cref="DtoTypeEmitter"/> class.</summary>
    /// <param name="model">The model for which data transfer types must be emitted.</param>
    public DtoTypeEmitter(IDataModel model)
        : base(model)
    {
    }
}

/// <summary>Emits data transfer types from a <see cref="IDataModel"/>.</summary>
/// <typeparam name="TDtoTypeDescription">The type of <see cref="DataTransferTypeDescription"/> to be created.</typeparam>
public class DtoTypeEmitter<TDtoTypeDescription>
    where TDtoTypeDescription : DataTransferTypeDescription
{
    /// <summary>Initializes a new instance of the <see cref="DtoTypeEmitter{TTypeDescription}"/> class.</summary>
    /// <param name="model">The model for which data transfer types must be emitted.</param>
    protected DtoTypeEmitter(IDataModel model)
    {
        Model = model;

        var assemblyName = new AssemblyName($"{nameof(DtoTypeEmitter<TDtoTypeDescription>)}{Guid.NewGuid()}");
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = AssemblyBuilder.DefineDynamicModule(nameof(Api.Model.DtoTypeEmitter<TDtoTypeDescription>));
        TypeDescriptions = EmitTypes();
    }

    /// <summary>Gets the model for which data transfer types must be emitted.</summary>
    public IDataModel Model { get; }

    /// <summary>Gets the list of produced descriptions..</summary>
    public IList<TDtoTypeDescription> TypeDescriptions { get; }

    /// <summary>Gets the assembly builder used to emit types.</summary>
    public AssemblyBuilder AssemblyBuilder { get; }

    /// <summary>Gets the module builder used to emit types.</summary>
    protected ModuleBuilder ModuleBuilder { get; }

    private IList<TDtoTypeDescription> EmitTypes()
    {
        var result = new List<TDtoTypeDescription>();
        var dtoTypes = Model.NodeTypes
            .Where(IsBrowsable)
            .Select(EmitDto)
            .ToList();
        foreach (var (type, dto) in dtoTypes)
        {
            AddDtoDescription(type, dto);
            EmitDtoConstructor(dto);
            EmitDtoProperties(type, dto,
                t => dtoTypes.FirstOrDefault(i => i.Type.Type == t).Dto ??
                throw new NotSupportedException($"Could not find dto type for type {t}."));

            result.Add((TDtoTypeDescription)ProcessType(type, dto.CreateTypeInfo()!));
        }
        return result.AsReadOnly();
    }

    /// <summary>Gets whether the type description is API browsable.</summary>
    /// <param name="description">The node type description.</param>
    /// <returns><c>true</c> if the node is browsable, <c>false</c> otherwise.</returns>
    public static bool IsBrowsable(NodeTypeDescription description) =>
        description.Type.GetCustomAttribute<ApiBrowsableAttribute>()?.Browsable ?? true;

    /// <summary>Creates a <typeparamref name="TDtoTypeDescription"/> instance.</summary>
    /// <param name="type">The type description.</param>
    /// <param name="dto">The emitted data transfer type.</param>
    /// <returns>The <typeparamref name="TDtoTypeDescription"/> instance.</returns>
    protected virtual DataTransferTypeDescription ProcessType(NodeTypeDescription type, TypeInfo dto)
    {
        return new DataTransferTypeDescription(type, dto);
    }

    private (NodeTypeDescription Type, TypeBuilder Dto) EmitDto(NodeTypeDescription type)
    {
        var result = ModuleBuilder.DefineType($"{GetTypeName(type.Type)}DTO",
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
    protected static string GetTypeName(Type type) =>
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

    private static void EmitDtoConstructor(TypeBuilder result)
    {
        var baseConstructor = typeof(NodeDto).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                                                             null,
                                                             new[] { typeof(Node), typeof(ObjectId) },
                                                             null)!;
        var constructor = result.DefineConstructor(MethodAttributes.Public,
                                                   CallingConventions.Standard | CallingConventions.HasThis,
                                                   new[] { typeof(Node), typeof(ObjectId) });
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
        if (property.IsEnumerable(t => t.IsAssignableTo(typeof(Node)), out var type))
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