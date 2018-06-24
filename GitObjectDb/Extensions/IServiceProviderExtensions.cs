using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    internal static class IServiceProviderExtensions
    {
        internal static TService GetService<TService>(this IServiceProvider serviceProvider) =>
            (TService)serviceProvider.GetService(typeof(TService));
    }
}
