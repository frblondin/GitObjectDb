using AutoFixture;
using AutoFixture.Kernel;
using GitObjectDb.Api.ProtoBuf.Model;
using Grpc.Net.Client;
using LibGit2Sharp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models.Organization;
using Models.Organization.Converters;
using NUnit.Framework;
using ProtoBuf.Grpc.Server;
using System;
using System.IO.Compression;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GitObjectDb.Api.ProtoBuf.Tests.Assets;
internal class TestServerCustomization : ICustomization, ISpecimenBuilder
{
    public void Customize(IFixture fixture)
    {
        var server = fixture.Freeze<TestServer>(c => c.FromFactory(CreateTestServer));
        fixture.Register(() => server.Services);
        fixture.Register(() => CreateGrpcChannel(server));
        fixture.Register(() => CreateCommitDescription(fixture));

        fixture.Customizations.Add(this);
    }

    private static TestServer CreateTestServer() => new(
        new WebHostBuilder()
            .UseKestrel()
            .UseStartup<Startup>());

    private static GrpcChannel CreateGrpcChannel(TestServer server)
    {
        server.Services.ConfigureGitObjectDbProtoRuntimeTypeModel();

        var httpClient = server.CreateClient();
        return GrpcChannel.ForAddress(
            httpClient.BaseAddress!,
            new()
            {
                HttpClient = httpClient,
            });
    }

    private static CommitDescription CreateCommitDescription(IFixture fixture) =>
        new(
            fixture.Create<string>(),
            fixture.Create<Signature>(),
            fixture.Create<Signature>());

    public object Create(object request, ISpecimenContext context) =>
        (request is Type t && t != typeof(TestServer) ? context.Create<TestServer>().Services.GetService(t) : null) ??
        new NoSpecimen();

    public class Startup : IStartup
    {
        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging(builder => builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddConsole())
                .AddMemoryCache()
                .AddGitObjectDb()
                .AddGitObjectDbSystemTextJson(o => o.Converters.Add(new TimeZoneInfoConverter()))
                .AddOrganizationModel()
                .AddGitObjectDbConnection(
                    TestContext.CurrentContext.Test.ID,
                    connection => new DataGenerator(connection, 5, 5).CreateInitData())
                .AddCodeFirstGrpc(config =>
                {
                    config.ResponseCompressionLevel = CompressionLevel.Optimal;
                    config.ResponseCompressionAlgorithm = "gzip";
                });
            return services.BuildServiceProvider();
        }

        public void Configure(IApplicationBuilder app) => app
            .UseRouting()
            .UseEndpoints(endpoints => endpoints.AddGitObjectProtobufControllers());
    }
}
