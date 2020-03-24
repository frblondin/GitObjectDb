using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models.Software
{
    public class SoftwareBenchmarkCustomization : SoftwareCustomization
    {
        public new const int DefaultApplicationCount = 2;
        public new const int DefaultTablePerApplicationCount = 200;
        public new const int DefaultFieldPerTableCount = 30;
        public new const int DefaultResourcePerTableCount = 5;

        public SoftwareBenchmarkCustomization()
            : base(
                  DefaultApplicationCount,
                  DefaultTablePerApplicationCount,
                  DefaultFieldPerTableCount,
                  DefaultResourcePerTableCount,
                  GitObjectDbFixture.SoftwareBenchmarkRepositoryPath)
        {
        }
    }
}
