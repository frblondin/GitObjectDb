using GitObjectDb.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitObjectDb.Serialization.Json
{
    [ExcludeFromGuardForNull]
    internal class JsonModelObjectReader : JsonTextReader
    {
        public JsonModelObjectReader(Func<string, string> relativeFileDataResolver, TextReader reader)
            : base(reader)
        {
            RelativeFileDataResolver = relativeFileDataResolver ?? throw new ArgumentNullException(nameof(relativeFileDataResolver));
        }

        public Func<string, string> RelativeFileDataResolver { get; }
    }
}
