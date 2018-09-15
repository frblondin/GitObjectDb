using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models.Migration
{
    [DataContract]
    public class DummyMigration : GitObjectDb.Models.Migration.AbstractMigration
    {
        public DummyMigration(IServiceProvider serviceProvider, Guid id, string name, bool canDowngrade, bool isIdempotent, string formula)
            : base(serviceProvider, id, name)
        {
            CanDowngrade = canDowngrade;
            IsIdempotent = isIdempotent;
            Formula = formula ?? throw new ArgumentNullException(nameof(formula));
        }

        [DataMember]
        public override bool CanDowngrade { get; }

        [DataMember]
        public override bool IsIdempotent { get; }

        [DataMember]
        public string Formula { get; }

        public override void Up()
        {
            Console.WriteLine($"{Id}: Up");
        }

        public override void Down()
        {
            Console.WriteLine($"{Id}: Down");
        }
    }
}
