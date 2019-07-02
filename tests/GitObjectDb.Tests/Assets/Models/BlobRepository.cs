using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using GitObjectDb.Attributes;
using GitObjectDb.Models;

namespace GitObjectDb.Tests.Assets.Models
{
    [Repository]
    public partial class BlobRepository
    {
        [DataMember]
        [Modifiable]
        public StringBlob Blob { get; }

        public ILazyChildren<Car> Cars { get; }
    }
}
