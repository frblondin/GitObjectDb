using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.Injection
{
    /// <summary>
    /// Marks a type as requiring asynchronous initialization and provides the result of that initialization.
    /// </summary>
    internal interface IAsyncInitialization
    {
        /// <summary>
        /// Gets the result of the asynchronous initialization of this instance.
        /// </summary>
        Task Initialization { get; }
    }
}
