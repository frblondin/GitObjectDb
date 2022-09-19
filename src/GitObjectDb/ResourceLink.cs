using LibGit2Sharp;
using System;

namespace GitObjectDb;

/// <summary>Link to an other repository containing resources.</summary>
public sealed class ResourceLink
{
    /// <summary>Initializes a new instance of the <see cref="ResourceLink"/> class.</summary>
    /// <param name="repository">The url of remote repository.</param>
    /// <param name="sha">The remote commit to point to.</param>
    public ResourceLink(string repository, string sha)
    {
        if (!ObjectId.TryParse(sha, out _))
        {
            throw new ArgumentException("Sha resource link is not a valid object id.");
        }
        Repository = repository;
        Sha = sha;
    }

    /// <summary>Gets the url of remote repository.</summary>
    public string Repository { get; }

    /// <summary>Gets the branch in remote repository.</summary>
    public string Sha { get; }
}
