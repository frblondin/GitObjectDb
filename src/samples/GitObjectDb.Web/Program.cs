using GitObjectDb.Api.ProtoBuf;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Models.Organization.Converters;
using ProtoBuf.Grpc.Server;
using System.IO.Compression;

#pragma warning disable SA1516 // ElementsMustBeSeparatedByBlankLine

var builder = WebApplication.CreateBuilder(args);
var repositoryType = args.Length > 0 ? args[0] : null;

switch (repositoryType)
{
    case "Organization":
        builder.Services
            .AddMemoryCache()
            .AddGitObjectDb()
            .AddGitObjectDbSystemTextJson(o => o.Converters.Add(new TimeZoneInfoConverter()))
            .AddOrganizationModel()
            .AddGitObjectDbOData()
            .AddGitObjectDbGraphQLSchema(options =>
            {
                options.ConfigureSchema = s => s.RegisterTypeMapping<TimeZoneInfo, TimeZoneInfoGraphType>();
                options.CacheEntryStrategy = CacheStrategy;
            })
            .AddGraphQL(builder => builder
                .UseApolloTracing(true)
                .AddSystemTextJson()
                .UseMemoryCache(options =>
                {
                    options.SizeLimit = 1_000_000;
                    options.SlidingExpiration = null;
                }))
            .AddGitObjectDbConnection("Organization");
        break;
    case "Software":
        builder.Services
            .AddMemoryCache()
            .AddGitObjectDb()
            .AddGitObjectDbSystemTextJson()
            .AddSoftwareModel()
            .AddGitObjectDbOData()
            .AddGitObjectDbGraphQLSchema(builder => builder.CacheEntryStrategy = CacheStrategy)
            .AddGraphQL(builder => builder
                .UseApolloTracing(true)
                .AddSystemTextJson()
                .UseMemoryCache(options =>
                {
                    options.SizeLimit = 1_000_000;
                    options.SlidingExpiration = null;
                }))
            .AddGitObjectDbConnection("Software", connection =>
            {
                var software = new Models.Software.DataGenerator(connection);
                var signature = new Signature("foo", "foo@acme.com", DateTimeOffset.Now);
                software.CreateData("Initial commit", signature);

                var application = software.Connection.GetApplications().First();
                signature = new Signature("foo", "foo@acme.com", DateTimeOffset.Now);
                software.Connection
                    .Update("main", c => c.CreateOrUpdate(application with { Description = "New description" }))
                    .Commit(new("Update appication", signature, signature));
            });
        break;
    default:
        throw new NotSupportedException($"'{repositoryType}' is not supported.");
}

static void CacheStrategy(ICacheEntry entry) => entry.SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(1));

builder.Services
    .AddControllers()
    .AddGitObjectDbODataControllers("v1", o => o.Select().Filter().OrderBy().Expand())
    .AddGitObjectDbGraphQLControllers();

builder.Services
    .AddCodeFirstGrpc(config =>
    {
        config.ResponseCompressionLevel = CompressionLevel.Optimal;
        config.ResponseCompressionAlgorithm = "gzip";
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseGraphQLAltair("/", new() { GraphQLEndPoint = "/api/graphql" });
}

app.UseHttpsRedirection()
   .UseAuthorization();

app.MapControllers();
app.AddGitObjectProtobufControllers();

app.Run();
