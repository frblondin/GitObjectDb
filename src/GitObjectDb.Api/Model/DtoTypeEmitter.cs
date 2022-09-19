using GitObjectDb.Model;
using System.Reflection;
using System.Reflection.Emit;

namespace GitObjectDb.Api.Model;

#pragma warning disable SA1402 // File may only contain a single type
public sealed class DtoTypeEmitter : DtoTypeEmitter<TypeDescription>
{
    public DtoTypeEmitter(IDataModel model)
        : base(model)
    {
    }
}

public class DtoTypeEmitter<TTypeDescription>
    where TTypeDescription : TypeDescription
{
    protected DtoTypeEmitter(IDataModel model)
    {
        Model = model;

        var assemblyName = new AssemblyName($"{nameof(DtoTypeEmitter<TTypeDescription>)}{Guid.NewGuid()}");
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = AssemblyBuilder.DefineDynamicModule(nameof(Api.Model.DtoTypeEmitter<TTypeDescription>));
        TypeDescriptions = EmitTypes();
    }

    public IDataModel Model { get; }

    public IList<TTypeDescription> TypeDescriptions { get; }

    public AssemblyBuilder AssemblyBuilder { get; }

    protected ModuleBuilder ModuleBuilder { get; }

    private IList<TTypeDescription> EmitTypes()
    {
        var result = new List<TTypeDescription>();
        var dtoTypes = Model.NodeTypes.Select(EmitDto).ToList();
        foreach (var (type, dto) in dtoTypes)
        {
            AddDtoDescription(type, dto);
            EmitDtoConstructor(dto);
            EmitDtoProperties(type, dto,
                t => dtoTypes.FirstOrDefault(i => i.Type.Type == t).Dto ??
                throw new NotSupportedException("Could not find dto type."));

            result.Add((TTypeDescription)ProcessType(type, dto));
        }
        return result.AsReadOnly();
    }

    protected virtual TypeDescription ProcessType(NodeTypeDescription type, TypeBuilder dto)
    {
        return new TypeDescription(type, dto.CreateTypeInfo()!);
    }

    private (NodeTypeDescription Type, TypeBuilder Dto) EmitDto(NodeTypeDescription type)
    {
        var result = ModuleBuilder.DefineType($"{type.Name}DTO",
                                              TypeAttributes.Public |
                                              TypeAttributes.Class |
                                              TypeAttributes.AutoClass |
                                              TypeAttributes.AnsiClass |
                                              TypeAttributes.BeforeFieldInit |
                                              TypeAttributes.AutoLayout,
                                              typeof(NodeDto));

        return (type, result);
    }

    private static void AddDtoDescription(NodeTypeDescription type, TypeBuilder result)
    {
        result.SetCustomAttribute(
            new CustomAttributeBuilder(
                typeof(DtoDescriptionAttribute).GetConstructors().Single(),
                new object[] { type.Name }));
    }

    private static void EmitDtoConstructor(TypeBuilder result)
    {
        var baseConstructor = typeof(NodeDto).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
                                                             null,
                                                             new[] { typeof(Node) },
                                                             null)!;
        var constructor = result.DefineConstructor(MethodAttributes.Public,
                                                   CallingConventions.Standard | CallingConventions.HasThis,
                                                   new[] { typeof(Node) });
        var ilGenerator = constructor.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Ldarg_1);
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