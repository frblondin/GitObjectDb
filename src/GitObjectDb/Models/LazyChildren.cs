using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Threading;
using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Provides support for asynchronous lazy children loading.
    /// </summary>
    /// <typeparam name="TChild">The type of the children.</typeparam>
    [DebuggerDisplay("Id = {Id}, State = {GetStateForDebugger}")]
    [DebuggerTypeProxy(typeof(LazyChildren<>.DebugView))]
    public sealed partial class LazyChildren<TChild> : ILazyChildren<TChild>
        where TChild : class, IModelObject
    {
        private static readonly string _nullReturnedValueExceptionMessage =
            $"Value returned by {nameof(LazyChildren<TChild>)} was null.";

        /// <summary>
        /// The synchronization object protecting <c>_instance</c>.
        /// </summary>
        private readonly object _mutex = new object();

        /// <summary>
        /// The underlying lazy task.
        /// </summary>
        private Lazy<Task<IImmutableList<TChild>>> _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public LazyChildren(Func<IModelObject, IRepository, Task<IImmutableList<IModelObject>>> factory)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var internalFactory = RetryOnFailure(async () =>
            {
                ThrowIfNoParent();

                var objectRepository = Parent.Repository;
                return await objectRepository.Repository.ExecuteAsync(
                    async repository =>
                    {
                        var nodes = (await factory(Parent, repository).ConfigureAwait(false)) ?? throw new GitObjectDbException(_nullReturnedValueExceptionMessage);
                        return AttachChildrenToParent(Parent, nodes.Cast<TChild>().ToImmutableList());
                    }).ConfigureAwait(false);
            });
            _instance = new Lazy<Task<IImmutableList<TChild>>>(internalFactory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        /// <param name="factory">The asynchronous delegate that is invoked to produce the value when it is needed. May not be <c>null</c>.</param>
        public LazyChildren(Func<IModelObject, Task<IImmutableList<TChild>>> factory)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var internalFactory = RetryOnFailure(async () =>
            {
                ThrowIfNoParent();

                return await RepositoryTaskScheduler.ExecuteAsync(() => factory(Parent)).ConfigureAwait(false);
            });
            _instance = new Lazy<Task<IImmutableList<TChild>>>(internalFactory);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public LazyChildren(IImmutableList<TChild> value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _instance = new Lazy<Task<IImmutableList<TChild>>>(() =>
            {
                ThrowIfNoParent();

                return System.Threading.Tasks.Task.FromResult(AttachChildrenToParent(Parent, value));
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        public LazyChildren()
            : this(ImmutableList.Create<TChild>())
        {
        }

        /// <inheritdoc />
        public IModelObject Parent { get; private set; }

        /// <inheritdoc />
        public bool ForceVisit { get; private set; }

        /// <inheritdoc />
        public bool IsStarted
        {
            get
            {
                lock (_mutex)
                {
                    return _instance.IsValueCreated;
                }
            }
        }

        /// <inheritdoc />
        public Task<IImmutableList<TChild>> Task
        {
            get
            {
                lock (_mutex)
                {
                    return _instance.Value;
                }
            }
        }

        private Func<Task<IImmutableList<TChild>>> RetryOnFailure(Func<Task<IImmutableList<TChild>>> factory)
        {
            return async () =>
            {
                try
                {
                    return await factory().ConfigureAwait(false);
                }
                catch
                {
                    lock (_mutex)
                    {
                        _instance = new Lazy<Task<IImmutableList<TChild>>>(factory);
                    }
                    throw;
                }
            };
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TaskAwaiter<IImmutableList<TChild>> GetAwaiter()
        {
            return Task.GetAwaiter();
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ConfiguredTaskAwaitable<IImmutableList<TChild>> ConfigureAwait(bool continueOnCapturedContext)
        {
            return Task.ConfigureAwait(continueOnCapturedContext);
        }

        /// <inheritdoc />
        public void Start()
        {
        }

        TaskAwaiter<IEnumerable<IModelObject>> ILazyChildren.GetAwaiter() =>
            GetEnumerableTask().GetAwaiter();

        ConfiguredTaskAwaitable<IEnumerable<IModelObject>> ILazyChildren.ConfigureAwait(bool continueOnCapturedContext) =>
            GetEnumerableTask().ConfigureAwait(continueOnCapturedContext);

        private async Task<IEnumerable<IModelObject>> GetEnumerableTask() =>
            await Task.ConfigureAwait(false);

        void ThrowIfNoParent()
        {
            if (Parent == null)
            {
                throw new GitObjectDbException($"Parent is not attached to {nameof(LazyChildren<TChild>)}.");
            }
        }

        private IImmutableList<TChild> AttachChildrenToParent(IModelObject parent, IImmutableList<TChild> children)
        {
            foreach (var child in children)
            {
                child.AttachToParent(parent);
            }
            return children;
        }

        /// <inheritdoc />
        [ExcludeFromGuardForNull]
        public ILazyChildren Clone(bool forceVisit, Func<IModelObject, IModelObject> update, IEnumerable<IModelObject> added = null, IEnumerable<IModelObject> deleted = null)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            return new LazyChildren<TChild>(async parent =>
                (await this)
                .Except(deleted?.Cast<TChild>() ?? Enumerable.Empty<TChild>())
                .Select(c => (TChild)update.Invoke(c) ?? throw new ObjectNotFoundException("No child returned while cloning children."))
                .Union(added?.Cast<TChild>() ?? Enumerable.Empty<TChild>())
                .ToImmutableList())
            {
                ForceVisit = ForceVisit || forceVisit,
            };
        }

        /// <inheritdoc />
        public ILazyChildren<TChild> AttachToParent(IModelObject parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (Parent != null && Parent != parent)
            {
                throw new GitObjectDbException("A single model object cannot be attached to two different parents.");
            }

            Parent = parent;
            return this;
        }
    }
}
