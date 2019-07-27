using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Models.CherryPick;
using GitObjectDb.Models.Merge;
using GitObjectDb.Models.Rebase;
using GitObjectDb.Serialization;
using GitObjectDb.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Models
{
    /// <inheritdoc/>
    internal sealed class ObjectRepositoryContainerFactory : IObjectRepositoryContainerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryContainerFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ObjectRepositoryContainerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc/>
        public IObjectRepositoryContainer<TRepository> Create<TRepository>(string path)
            where TRepository : class, IObjectRepository
        {
            return new ObjectRepositoryContainer<TRepository>(path,
                _serviceProvider.GetRequiredService<IObjectRepositoryLoader>(),
                _serviceProvider.GetRequiredService<ComputeTreeChangesFactory>(),
                _serviceProvider.GetRequiredService<ObjectRepositoryMergeFactory>(),
                _serviceProvider.GetRequiredService<ObjectRepositoryRebaseFactory>(),
                _serviceProvider.GetRequiredService<ObjectRepositoryCherryPickFactory>(),
                _serviceProvider.GetRequiredService<IRepositoryProvider>(),
                _serviceProvider.GetRequiredService<GitHooks>(),
                _serviceProvider.GetRequiredService<ObjectRepositorySerializerFactory>(),
                _serviceProvider.GetRequiredService<ILogger<ObjectRepositoryContainer>>());
        }
    }
}
