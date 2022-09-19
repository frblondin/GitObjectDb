using LibGit2Sharp;

namespace GitObjectDb.Internal;
internal interface ISubmoduleProvider
{
    Repository GetOrCreateSubmoduleRepository(DataPath path, string url);
}
