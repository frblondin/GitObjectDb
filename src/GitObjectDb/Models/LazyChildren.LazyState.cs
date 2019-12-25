namespace GitObjectDb.Models
{
    public sealed partial class LazyChildren<TChild>
        where TChild : class, IModelObject
    {
        internal enum LazyState
        {
            /// <summary>
            /// The children retrieval hasn't been started.
            /// </summary>
            NotStarted,

            /// <summary>
            /// The children retrieval is ongoing.
            /// </summary>
            Executing,

            /// <summary>
            /// The children have been loaded.
            /// </summary>
            Completed,
        }
    }
}