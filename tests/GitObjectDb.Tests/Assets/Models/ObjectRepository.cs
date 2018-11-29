using GitObjectDb.Models;
using GitObjectDb.Models.Migration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models
{
    [Repository]
    public partial class ObjectRepository
    {
        public ILazyChildren<Application> Applications { get; }
    }
}
