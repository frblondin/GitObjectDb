using FluentValidation;
using FluentValidation.Results;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <summary>
    /// <see cref="ILazyChildren"/> validator.
    /// </summary>
    public sealed class LazyChildrenValidator : AbstractValidator<ILazyChildren>
    {
        LazyChildrenValidator()
        {
        }

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static LazyChildrenValidator Instance { get; } = new LazyChildrenValidator();
    }
}
