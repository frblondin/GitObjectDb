using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    public class MissingDependencyException : Exception
    {
        public MissingDependencyException(Type type) : base($"The service '{type}' could not be found in current service provider.") { }
    }

    internal static class IServiceProviderExtensions
    {
        internal static TService GetService<TService>(this IServiceProvider serviceProvider) where TService : class =>
            (TService)serviceProvider.GetService(typeof(TService)) ?? throw new MissingDependencyException(typeof(TService));
    }
}
