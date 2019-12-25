using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GitObjectDb.Models
{
    public sealed partial class LazyChildren<TChild>
        where TChild : class, IModelObject
    {
        [DebuggerNonUserCode]
        internal LazyState GetStateForDebugger
        {
            get
            {
                if (!_instance.IsValueCreated)
                {
                    return LazyState.NotStarted;
                }

                if (!_instance.Value.IsCompleted)
                {
                    return LazyState.Executing;
                }

                return LazyState.Completed;
            }
        }

        [DebuggerNonUserCode]
        internal sealed class DebugView
        {
            private readonly LazyChildren<TChild> _lazy;

            public DebugView(LazyChildren<TChild> lazy)
            {
                _lazy = lazy;
            }

            public LazyState State => _lazy.GetStateForDebugger;

            public Task Task
            {
                get
                {
                    if (!_lazy._instance.IsValueCreated)
                    {
                        throw new InvalidOperationException("Not yet created.");
                    }

                    return _lazy._instance.Value;
                }
            }

            public IImmutableList<TChild> Value
            {
                get
                {
                    if (!_lazy._instance.IsValueCreated || !_lazy._instance.Value.IsCompleted)
                    {
                        throw new InvalidOperationException("Not yet created.");
                    }

                    return _lazy._instance.Value.Result;
                }
            }
        }
    }
}
