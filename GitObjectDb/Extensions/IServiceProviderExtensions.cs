using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    /// <summary>
    /// A set of methods for instances of <see cref="IServiceProvider"/>.
    /// </summary>
    internal static class IServiceProviderExtensions
    {
        /// <summary>
        /// Gets the service of the specified type.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>A service of type <typeparamref name="TService"/>.</returns>
        /// <exception cref="MissingDependencyException">Service could not be found.</exception>
        internal static TService GetService<TService>(this IServiceProvider serviceProvider)
            where TService : class
        {
            return (TService)serviceProvider.GetService(typeof(TService)) ?? throw new MissingDependencyException(typeof(TService));
        }
    }
}
