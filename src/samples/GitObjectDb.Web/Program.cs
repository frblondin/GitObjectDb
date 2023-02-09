using GitObjectDb;
using GitObjectDb.Api.GraphQL;
using GitObjectDb.Api.OData;
using GitObjectDb.Api.ProtoBuf;
using GitObjectDb.Web;
using GraphQL;
using LibGit2Sharp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Models.Organization;
using Models.Organization.Converters;
using Models.Software;
using ProtoBuf.Grpc.Server;
using System.IO.Compression;

#pragma warning disable SA1516 // ElementsMustBeSeparatedByBlankLine

var builder = WebApplication.CreateBuilder(args);
builder.AddAuthentication()
    .AddAuthorization();

var repositoryType = args.Length > 1 ? args[1] : builder.Configuration["RepositoryType"];

switch (repositoryType)
{
    case "Organization":
        builder.Services
            .AddMemoryCache()
            .AddGitObjectDb()
            .AddGitObjectDbSystemTextJson(o => o.Converters.Add(new TimeZoneInfoConverter()))
            .AddOrganizationModel()
            .AddGitObjectDbOData()
            .AddSingleton<TimeZoneInfoGraphType>()
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
    .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
    .AddSingleton<IAuthorizationProvider, AuthorizationProvider>()
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

app.UseHttpsRedirection()
   .UseAuthentication()
   .UseAuthorization()
   .Use(async (context, next) =>
   {
       if (!context.User.Identity?.IsAuthenticated ?? false)
       {
           await context.ChallengeAsync(new AuthenticationProperties
           {
               RedirectUri = context.Request.Path,
           });
           return;
       }
       await next.Invoke(context);
   });

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseGraphQLAltair("/", new() { GraphQLEndPoint = "/api/graphql" });
}

app.MapControllers();
app.AddGitObjectProtobufControllers();

app.Run();
