using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using System.Reflection.Emit;

namespace GitObjectDb.Api.OData.Model;

internal class GeneratedTypesApplicationPart : ApplicationPart, IApplicationPartTypeProvider
{
    private readonly DtoTypeEmitter _emitter;

    public GeneratedTypesApplicationPart(DtoTypeEmitter emitter)
    {
        var assemblyName = new AssemblyName($"{nameof(GeneratedTypesApplicationPart)}{Guid.NewGuid()}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = assemblyBuilder.DefineDynamicModule(nameof(DtoTypeEmitter));
        _emitter = emitter;
        Types = EmitControllers().ToList();
    }

    public override string Name => nameof(GeneratedTypesApplicationPart);

    public IList<TypeInfo> Types { get; }

    IEnumerable<TypeInfo> IApplicationPartTypeProvider.Types => Types;

    public ModuleBuilder ModuleBuilder { get; }

    public IEnumerable<TypeInfo> EmitControllers()
    {
        foreach (var description in _emitter.TypeDescriptions)
        {
            var controllerTypeBuilder = EmitController(description);
            yield return controllerTypeBuilder.CreateTypeInfo()!;
        }
    }

    private TypeBuilder EmitController(DataTransferTypeDescription description)
    {
        var genericType = typeof(NodeController<,>).MakeGenericType(description.NodeType.Type, description.DtoType);
        var result = ModuleBuilder.DefineType($"{DtoTypeEmitter.GetTypeName(description.NodeType.Type)}Controller",
            TypeAttributes.Public |
            TypeAttributes.Class |
            TypeAttributes.AutoClass |
            TypeAttributes.AnsiClass |
            TypeAttributes.BeforeFieldInit |
            TypeAttributes.AutoLayout,
            genericType);

        EmitConstructor(genericType, result);
        return result;
    }

    private static void EmitConstructor(Type genericType, TypeBuilder controllerType)
    {
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var baseConstructor = genericType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
#pragma warning restore S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
        var parameterTypes = baseConstructor.GetParameters().Select(p => p.ParameterType).ToArray();
        var constructor = controllerType.DefineConstructor(
            MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis,
            parameterTypes);
        var ilGenerator = constructor.GetILGenerator();

        ilGenerator.Emit(OpCodes.Ldarg_0); // push "this"
        for (var i = 0; i < parameterTypes.Length; i++)
        {
            ilGenerator.Emit(OpCodes.Ldarg, i + 1);
        }
        ilGenerator.Emit(OpCodes.Call, baseConstructor);
        ilGenerator.Emit(OpCodes.Nop);
        ilGenerator.Emit(OpCodes.Nop);
        ilGenerator.Emit(OpCodes.Ret);
    }
}
