using FluentValidation;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// <see cref="ILazyLink"/> validator.
    /// </summary>
    public sealed class LazyLinkValidator : AbstractValidator<ILazyLink>
    {
        LazyLinkValidator()
        {
            RuleFor(l => l.Path).SetValidator(new ObjectPathPropertyValidator());
        }

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static LazyLinkValidator Instance { get; } = new LazyLinkValidator();
    }
}
