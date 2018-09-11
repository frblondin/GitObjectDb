using FluentValidation.Validators;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// <see cref="ObjectPath"/> property validator.
    /// </summary>
    /// <seealso cref="PropertyValidator" />
    public class ObjectPathPropertyValidator : PropertyValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPathPropertyValidator"/> class.
        /// </summary>
        public ObjectPathPropertyValidator()
            : base("{PropertyName} {Message}")
        {
        }

        /// <inheritdoc />
        protected override bool IsValid(PropertyValidatorContext context)
        {
            var path = context.PropertyValue as ObjectPath ?? throw new NotSupportedException($"Property of type {nameof(ObjectPath)} expected.");
            var lazyLink = context.Instance as ILazyLink ?? throw new NotSupportedException($"Instance of type {nameof(ILazyLink)} expected.");
            var instance = lazyLink.Parent;

            return IsValid(context, path, instance);
        }

        static bool IsValid(PropertyValidatorContext context, ObjectPath path, IMetadataObject instance)
        {
            if (instance.Repository != null)
            {
                if (!IsValidRepositoryDependency(path, instance))
                {
                    context.MessageFormatter.AppendArgument("Message", $"is using a repository '{path.Repository}' which is not added to the dependencies.");
                    return false;
                }

                if (!IsReferencedObjectExisting(path, instance))
                {
                    context.MessageFormatter.AppendArgument("Message", $"is referring an unexisting object {path}.");
                    return false;
                }
            }
            return true;
        }

        static bool IsValidRepositoryDependency(ObjectPath path, IMetadataObject instance) =>
            path.Repository == instance.Repository.Id ||
            instance.Repository.Dependencies.Select(d => d.Id).Contains(path.Repository);

        static bool IsReferencedObjectExisting(ObjectPath path, IMetadataObject instance) =>
            instance.Repository.TryGetFromGitPath(path) != null;
    }
}
