using GitObjectDb.Model;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;
using System.Reflection.Emit;

namespace GitObjectDb.OData
{
    internal class GeneratedTypesApplicationPart : ApplicationPart, IApplicationPartTypeProvider
    {
        private readonly IDataModel _model;

        public GeneratedTypesApplicationPart(IDataModel model)
        {
            _model = model;
            TypeDescriptions = EmitTypes();
        }

        public override string Name => nameof(GeneratedTypesApplicationPart);

        public IEnumerable<TypeInfo> Types => TypeDescriptions.Select(d => d.ControllerType.GetTypeInfo());

        public IList<(NodeTypeDescription NodeType, Type DtoType, Type ControllerType)> TypeDescriptions { get; }

        private IList<(NodeTypeDescription NodeType, Type DtoType, Type ControllerType)> EmitTypes()
        {
            var result = new List<(NodeTypeDescription NodeType, Type DtoType, Type ControllerType)>();
            var dtoTypes = new List<TypeInfo>();

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{nameof(GeneratedTypesApplicationPart)}{Guid.NewGuid()}"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(nameof(GeneratedTypesApplicationPart));
            foreach (var type in _model.NodeTypes)
            {
                var dtoType = EmitDTO(moduleBuilder, type).CreateTypeInfo()!;
                var controllerType = EmitController(moduleBuilder, type, dtoType.AsType()).CreateTypeInfo()!;

                result.Add((type, dtoType.AsType(), controllerType.AsType()));
            }
            return result.AsReadOnly();
        }

        private static TypeBuilder EmitDTO(ModuleBuilder moduleBuilder, NodeTypeDescription type)
        {
            var result = moduleBuilder.DefineType($"{type.Name}DTO",
                                                  TypeAttributes.Public |
                                                  TypeAttributes.Class |
                                                  TypeAttributes.AutoClass |
                                                  TypeAttributes.AnsiClass |
                                                  TypeAttributes.BeforeFieldInit |
                                                  TypeAttributes.AutoLayout,
                                                  typeof(NodeDTO));

            result.SetCustomAttribute(
                new CustomAttributeBuilder(
                    typeof(DtoDescriptionAttribute).GetConstructors().Single(),
                    new object[] { type.Name }));

            var dtoPropertyNames = typeof(NodeDTO).GetProperties().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var property in type.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (dtoPropertyNames.Contains(property.Name))
                {
                    continue;
                }
                EmitDTOProperty(result, property);
            }

            return result;
        }

        private static void EmitDTOProperty(TypeBuilder result, PropertyInfo property)
        {
            var fieldBuilder = result.DefineField($"_{property.Name}",
                                                  property.PropertyType,
                                                  FieldAttributes.Private);
            var propertyBuilder = result.DefineProperty(property.Name,
                                                        PropertyAttributes.HasDefault,
                                                        property.PropertyType,
                                                        null);

            EmitDTPPropertyGetter(result, property, fieldBuilder, propertyBuilder);
            EmitDTPPropertySetter(result, property, fieldBuilder, propertyBuilder);
        }

        private static void EmitDTPPropertyGetter(TypeBuilder result, PropertyInfo property, FieldBuilder fieldBuilder, PropertyBuilder propertyBuilder)
        {
            var getter = result.DefineMethod($"get_{property.Name}",
                                             MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                                             property.PropertyType,
                                             Type.EmptyTypes);

            var getterIL = getter.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getter);
        }

        private static void EmitDTPPropertySetter(TypeBuilder result, PropertyInfo property, FieldBuilder fieldBuilder, PropertyBuilder propertyBuilder)
        {
            var setter = result.DefineMethod($"set_{property.Name}",
                                             MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                                             null,
                                             new Type[] { property.PropertyType });

            var setterIL = setter.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Stfld, fieldBuilder);
            setterIL.Emit(OpCodes.Ret);

            propertyBuilder.SetSetMethod(setter);
        }

        private static TypeBuilder EmitController(ModuleBuilder moduleBuilder, NodeTypeDescription type, Type dtoType)
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
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg, i + 1);
            }
            ilGenerator.Emit(OpCodes.Call, baseConstructor);
            ilGenerator.Emit(OpCodes.Nop);
            ilGenerator.Emit(OpCodes.Nop);
            ilGenerator.Emit(OpCodes.Ret);
        }
    }
}
