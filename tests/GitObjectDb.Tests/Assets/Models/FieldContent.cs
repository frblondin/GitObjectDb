using CSharpDiscriminatedUnion.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models
{
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable SA1205 // Partial elements must declare access
#pragma warning disable SA1307 // Accessible fields must begin with upper-case letter
    [GenerateDiscriminatedUnion]
    [DataContract]
    public partial class FieldContent
    {
        public bool IsLink => this is Cases.Link;

        public T MatchOrDefault<T>(Func<T> matchDefault = null, Func<FieldLinkContent, T> matchLink = null)
        {
            return Match(() => default, matchDefault, matchLink);
        }

        partial class Cases
        {
            [DataContract]
            partial class Default : FieldContent
            {
            }

            [DataContract]
            partial class Link : FieldContent
            {
                [DataMember]
                public readonly FieldLinkContent fieldLinkContent;
            }
        }
    }
#pragma warning restore SA1307 // Accessible fields must begin with upper-case letter
#pragma warning restore SA1205 // Partial elements must declare access
#pragma warning restore IDE1006 // Naming Styles
}
