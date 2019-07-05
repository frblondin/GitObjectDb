using System;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Creates new <see cref="IObjectRepositoryContainer"/> instances.
    /// </summary>
    public interface IObjectRepositoryContainerFactory
    {
        /// <summary>
        /// Creates a new instance of <see cref="IObjectRepositoryContainer{TRepository}"/>.
        /// </summary>
        /// <typeparam name="TRepository">The type of the repositories.</typeparam>
        /// <param name="path">The path where repositories should be stored.</param>
        /// <returns>The newly created instance.</returns>
        IObjectRepositoryContainer<TRepository> Create<TRepository>(string path)
            where TRepository : class, IObjectRepository;
    }
}