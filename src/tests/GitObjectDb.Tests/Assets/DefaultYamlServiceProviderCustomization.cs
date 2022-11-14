using AutoFixture;
using AutoFixture.Kernel;
using GitObjectDb.Tests.Assets.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models.Software;
using System;

namespace GitObjectDb.Tests.Assets;

public class DefaultYamlServiceProviderCustomization : DefaultServiceProviderCustomization
{
    public DefaultYamlServiceProviderCustomization()
        : base(true)
    {
    }
}
