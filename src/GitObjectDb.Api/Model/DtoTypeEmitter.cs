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

    internal ModuleBuilder ModuleBuilder { get; }

    private IList<TTypeDescription> EmitTypes()
    {
        var result = new List<TTypeDescription>();
        foreach (var type in Model.NodeTypes)
        {
            var description = (TTypeDescription)ProcessType(type, ModuleBuilder);
            result.Add(description);
        }
        return result.AsReadOnly();
    }

    protected virtual TypeDescription ProcessType(NodeTypeDescription type, ModuleBuilder moduleBuilder)
    {
        var dtoType = EmitDto(moduleBuilder, type).CreateTypeInfo()!;

        return new TypeDescription(type, dtoType);
    }

    private static TypeBuilder EmitDto(ModuleBuilder moduleBuilder, NodeTypeDescription type)
    {
        var result = moduleBuilder.DefineType($"{type.Name}DTO",
                                              TypeAttributes.Public |
                                              TypeAttributes.Class |
                                              TypeAttributes.AutoClass |
                                              TypeAttributes.AnsiClass |
                                              TypeAttributes.BeforeFieldInit |
                                              TypeAttributes.AutoLayout,
                                              typeof(NodeDto));

        AddDtoDescription(type, result);
        EmitDtoConstructor(result);
        EmirDtoProperties(type, result);

        return result;
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

    private static void EmirDtoProperties(NodeTypeDescription type, TypeBuilder result)
    {
        var properties = typeof(NodeDto).GetProperties();
        var names = properties.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var property in type.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (names.Contains(property.Name))
            {
                continue;
            }
            EmitDtoProperty(result, property);
        }
    }

    private static void EmitDtoProperty(TypeBuilder result, PropertyInfo property)
    {
        var fieldBuilder = result.DefineField($"_{property.Name}",
                                              property.PropertyType,
                                              FieldAttributes.Private);
        var propertyBuilder = result.DefineProperty(property.Name,
                                                    PropertyAttributes.HasDefault,
                                                    property.PropertyType,
                                                    null);

        EmitDtoPropertyGetter(result, property, fieldBuilder, propertyBuilder);
        EmitDtoPropertySetter(result, property, fieldBuilder, propertyBuilder);
    }

    private static void EmitDtoPropertyGetter(TypeBuilder result,
                                              PropertyInfo property,
                                              FieldInfo field,
                                              PropertyBuilder propertyBuilder)
    {
        var getter = result.DefineMethod($"get_{property.Name}",
                                         MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                                         property.PropertyType,
                                         Type.EmptyTypes);

        var getterIL = getter.GetILGenerator();
        getterIL.Emit(OpCodes.Ldarg_0);
        getterIL.Emit(OpCodes.Ldfld, field);
        getterIL.Emit(OpCodes.Ret);

        propertyBuilder.SetGetMethod(getter);
    }

    private static void EmitDtoPropertySetter(TypeBuilder result,
                                              PropertyInfo property,
                                              FieldInfo field,
                                              PropertyBuilder propertyBuilder)
    {
        var setter = result.DefineMethod($"set_{property.Name}",
                                         MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                                         null,
                                         new[] { property.PropertyType });

        var setterIL = setter.GetILGenerator();
        setterIL.Emit(OpCodes.Ldarg_0);
        setterIL.Emit(OpCodes.Ldarg_1);
        setterIL.Emit(OpCodes.Stfld, field);
        setterIL.Emit(OpCodes.Ret);

        propertyBuilder.SetSetMethod(setter);
    }
}