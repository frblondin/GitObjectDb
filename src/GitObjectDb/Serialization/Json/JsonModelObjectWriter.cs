using GitObjectDb.Attributes;
using GitObjectDb.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitObjectDb.Serialization.Json
{
    [ExcludeFromGuardForNull]
    internal class JsonModelObjectWriter : JsonTextWriter
    {
        public JsonModelObjectWriter(IModelObject node, TextWriter textWriter)
            : base(textWriter)
        {
            AdditionalObjects = new List<ModelNestedObjectInfo>();
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public IList<ModelNestedObjectInfo> AdditionalObjects { get; }

        public IModelObject Node { get; }
    }
}
