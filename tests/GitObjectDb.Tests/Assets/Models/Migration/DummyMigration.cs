using GitObjectDb.Models;
using GitObjectDb.Models.Migration;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models.Migration
{
    [Model]
    public partial class DummyMigration : IMigration
    {
        [DataMember]
        public bool CanDowngrade { get; }

        [DataMember]
        public bool IsIdempotent { get; }

        [DataMember]
        public string Formula { get; }

        public void Up()
        {
            Console.WriteLine($"{Id}: Up");
        }

        public void Down()
        {
            Console.WriteLine($"{Id}: Down");
        }
    }
}
