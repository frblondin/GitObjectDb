using LibGit2Sharp;
using System;

namespace GitObjectDb.Tests.Git.Backends
{
    public sealed partial class LiteDbBackend
    {
        internal class StoreItem
        {
            public StoreItem(string sha, ObjectType objectType, byte[] data)
            {
                if (string.IsNullOrWhiteSpace(sha))
                {
                    throw new ArgumentException("message", nameof(sha));
                }

                Sha = sha;
                ObjectType = objectType;
                Data = data ?? throw new ArgumentNullException(nameof(data));
            }

            public string Sha { get; }

            public byte[] Data { get; }

            public ObjectType ObjectType { get; }
        }
    }
}