using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    internal static class ObjectDatabaseExtensions
    {
        internal static void CopyAllBlobs(this ObjectDatabase source, OdbBackend backend)
        {
            foreach (var blob in source.OfType<Blob>())
            {
                if (!backend.Exists(blob.Id))
                {
                    var stream = blob.GetContentStream();
                    backend.Write(blob.Id, stream, stream.Length, ObjectType.Blob);
                }
            }
        }
    }
}
