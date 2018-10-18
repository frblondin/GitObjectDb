using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GitObjectDb.Validations
{
    public partial class ValidatorFactory
    {
        [DebuggerDisplay("Validator target = {Validator.TargetType}")]
        class ValidatorComparerItem
        {
            public ValidatorComparerItem(Type targetType, (Type ValidatorType, Type TargetType) validator)
            {
                TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
                Validator = validator;

                Initialize();
            }

            public Type TargetType { get; }

            public (Type ValidatorType, Type TargetType) Validator { get; }

            public Type AdaptedType { get; private set; }

            public Type GenericParameter { get; private set; }

            void Initialize()
            {
                if (TargetType == Validator.TargetType)
                {
                    AdaptedType = Validator.ValidatorType;
                }
                else if (Validator.ValidatorType.IsGenericTypeDefinition)
                {
                    var genericParameters = Validator.ValidatorType.GetGenericArguments();
                    TryAdaptGenericParameters(Validator.TargetType, TargetType, genericParameters);
                    if (genericParameters.All(p => !p.IsGenericParameter))
                    {
                        AdaptedType = Validator.ValidatorType.MakeGenericType(genericParameters);
                    }
                }
                else
                {
                    if (Validator.TargetType.IsAssignableFrom(TargetType))
                    {
                        AdaptedType = Validator.ValidatorType;
                    }
                }
            }

            void TryAdaptGenericParameters(Type validatorTypeChunk, Type typeChunk, IList<Type> genericParameters)
            {
                if (validatorTypeChunk.IsGenericParameter)
                {
                    TryAdaptGenericParameter(validatorTypeChunk, typeChunk, genericParameters);
                }
                else if (validatorTypeChunk.IsGenericType)
                {
                    TryAdaptGenericParameterArgs(validatorTypeChunk, typeChunk, genericParameters);
                }
            }

            void TryAdaptGenericParameter(Type validatorTypeChunk, Type typeChunk, IList<Type> genericParameters)
            {
                var validConstraints = validatorTypeChunk.GetGenericParameterConstraints().All(constraint => constraint.IsAssignableFrom(typeChunk));
                if (validConstraints)
                {
                    if (GenericParameter != null)
                    {
                        throw new GitObjectDbException("No more than one generic parameter is supported while resolving validators.");
                    }
                    GenericParameter = validatorTypeChunk;
                    var index = genericParameters.IndexOf(validatorTypeChunk);
                    if (index != -1)
                    {
                        genericParameters[index] = typeChunk;
                    }
                }
            }

            void TryAdaptGenericParameterArgs(Type validatorTypeChunk, Type typeChunk, IList<Type> genericParameters)
            {
                var definition = validatorTypeChunk.GetGenericTypeDefinition();
                var matchingTypeDefinition = FlattenInterfacesAndBaseTypes(typeChunk).FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == definition);
                if (matchingTypeDefinition != null)
                {
                    var validatorGenericArgs = validatorTypeChunk.GetGenericArguments();
                    var typeGenericArgs = matchingTypeDefinition.GetGenericArguments();
                    for (int i = 0; i < validatorGenericArgs.Length; i++)
                    {
                        TryAdaptGenericParameters(validatorGenericArgs[i], typeGenericArgs[i], genericParameters);
                    }
                }
            }

            static IEnumerable<Type> FlattenInterfacesAndBaseTypes(Type type)
            {
                yield return type;
                foreach (var i in type.GetInterfaces())
                {
                    yield return i;
                }
                var baseType = type.BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    yield return baseType;
                    baseType = baseType.BaseType;
                }
            }
        }

        class ValidatorComparer : IComparer<ValidatorComparerItem>
        {
            const int NoConstraint = int.MaxValue;

            public ValidatorComparer(Type targetType)
            {
                TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
            }

            public Type TargetType { get; }

            public int Compare(ValidatorComparerItem x, ValidatorComparerItem y)
            {
                if (x == null)
                {
                    throw new ArgumentNullException(nameof(x));
                }
                if (y == null)
                {
                    throw new ArgumentNullException(nameof(y));
                }

                if (x.Validator.TargetType == TargetType)
                {
                    return -1;
                }

                if (y.Validator.TargetType == TargetType)
                {
                    return 1;
                }
                if (x.AdaptedType == null)
                {
                    return y.AdaptedType == null ? 0 : 1;
                }
                if (y.AdaptedType == null)
                {
                    return -1;
                }
                if (x.GenericParameter != null && y.GenericParameter == null)
                {
                    return -1;
                }
                if (x.GenericParameter == null && y.GenericParameter != null)
                {
                    return 1;
                }
                return GetDistance(x).CompareTo(GetDistance(y));
            }

            int GetDistance(ValidatorComparerItem item)
            {
                var constraints = item.GenericParameter.GetGenericParameterConstraints();
                return constraints.Any() ?
                    constraints.Select(c => GetDistance(c)).Min() :
                    NoConstraint;
            }

            int GetDistance(Type typeConstraint)
            {
                int result = 0;
                var type = TargetType;
                while (type != typeConstraint && type != typeof(object))
                {
                    type = type.BaseType;
                    result += 1;
                }
                return result;
            }
        }
    }
}
