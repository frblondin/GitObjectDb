using GitObjectDb.Api.Model;
using GitObjectDb.Model;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using System.Reflection.Emit;

namespace GitObjectDb.Api.OData.Model;

internal class GeneratedTypesApplicationPart : ApplicationPart, IApplicationPartTypeProvider
{
    public GeneratedTypesApplicationPart(IDataModel model)
    {
        Model = model;
        var emitter = new ODataDtoTypeEmitter(model);
        TypeDescriptions = emitter.TypeDescriptions;
    }

    public IDataModel Model { get; }

    public override string Name => nameof(GeneratedTypesApplicationPart);

    public IEnumerable<TypeInfo> Types => TypeDescriptions.Select(d => d.ControllerType.GetTypeInfo());

    public IList<ODataTypeDescription> TypeDescriptions { get; }

    public class ODataDtoTypeEmitter : DtoTypeEmitter<ODataTypeDescription>
    {
        public ODataDtoTypeEmitter(IDataModel model)
            : base(model)
        {
        }

        protected override TypeDescription ProcessType(NodeTypeDescription type, TypeBuilder dto)
        {
            var baseDescription = base.ProcessType(type, dto);
            var controllerType = EmitController(type, baseDescription.DtoType, ModuleBuilder).CreateTypeInfo()!;
            return new ODataTypeDescription(baseDescription, controllerType);
        }

        private static TypeBuilder EmitController(NodeTypeDescription type, Type dtoType, ModuleBuilder moduleBuilder)
        {
            var genericType = typeof(NodeController<,>).MakeGenericType(type.Type, dtoType);
            var result = moduleBuilder.DefineType($"{type.Name}Controller",
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
            var baseConstructor = genericType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single();
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

    public class ODataTypeDescription : TypeDescription
    {
        public ODataTypeDescription(TypeDescription description, Type controllerType)
            : base(description.NodeType, description.DtoType)
        {
            ControllerType = controllerType;
        }

        public Type ControllerType { get; }
    }
}
