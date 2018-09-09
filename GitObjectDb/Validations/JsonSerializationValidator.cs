using FluentValidation;
using FluentValidation.Results;
using FluentValidation.Validators;
using GitObjectDb.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// <see cref="Type"/> validator that ensures that instances are immutable.
    /// </summary>
    /// <seealso cref="AbstractValidator{Type}" />
    public class JsonSerializationValidator : AbstractValidator<Type>
    {
        static readonly JsonSerializer _serializer = JsonSerializer.CreateDefault();

        readonly IEnumerable<ICreatorParameterResolver> _creatorParameterResolvers;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializationValidator"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <exception cref="ArgumentNullException">serviceProvider</exception>
        public JsonSerializationValidator(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _creatorParameterResolvers = serviceProvider.GetRequiredService<IEnumerable<ICreatorParameterResolver>>();

            RuleForEach(t => GetContract(t).CreatorParameters)
                .Must(HasMatchingProperty)
                .WithMessage("The constructor parameter {PropertyValue} has no matching serialized property: {Message}.")
                .OverridePropertyName("Constructor parameters");
        }

        static JsonObjectContract GetContract(Type type) =>
            (JsonObjectContract)_serializer.ContractResolver.ResolveContract(type);

        bool HasMatchingProperty(Type type, JsonProperty constructorParameter, PropertyValidatorContext context)
        {
            var contract = GetContract(type);
            if (_creatorParameterResolvers.Any(r => r.CanResolve(constructorParameter, type)))
            {
                return true;
            }
            var matching = contract.Properties.FirstOrDefault(p => p.PropertyName.Equals(constructorParameter.PropertyName, StringComparison.OrdinalIgnoreCase));
            if (matching == null)
            {
                context.MessageFormatter.AppendArgument("Message", $"no property named '{constructorParameter.PropertyName}' could be found.");
                return false;
            }
            if (matching.Ignored)
            {
                context.MessageFormatter.AppendArgument("Message", $"the property named '{constructorParameter.PropertyName}' is not serialized.");
                return false;
            }
            if (matching.PropertyType != constructorParameter.PropertyType)
            {
                context.MessageFormatter.AppendArgument("Message", $"the property type '{matching.PropertyType}' does not match.");
                return false;
            }
            return true;
        }
    }
}
