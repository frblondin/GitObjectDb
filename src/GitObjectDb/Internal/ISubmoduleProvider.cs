using GitDotNet;

namespace GitObjectDb.Internal;
internal interface ISubmoduleProvider
{
    IGitConnection GetOrCreateSubmoduleRepository(DataPath path, string url);
}
