using System.Runtime.Serialization;
using System;
using GitObjectDb.Attributes;
using GitObjectDb.Models;

namespace GitObjectDb.Tests.Assets.Models
{
    [Model]
    public partial class Car
    {
        [DataMember]
        [Modifiable]
        public StringBlob Blob { get; }
    }
}