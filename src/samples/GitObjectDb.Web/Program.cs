using GitObjectDb.Api;
using GraphQL;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Models.Organization.Converters;

var builder = WebApplication.CreateBuilder(args);

var repositoryType = args.Length > 0 ? args[0] : null;

switch (repositoryType)
{
    case "Organization":
        builder.Services
            .AddMemoryCache()
            .AddGitObjectDb(c => c.AddSystemTextJson(o => o.Converters.Add(new TimeZoneInfoConverter())))
            .AddOrganizationModel()
            .AddGitObjectDbApi()
            .AddGitObjectDbGraphQL(builder =>
            {
                builder.Schema.RegisterTypeMapping<TimeZoneInfo, TimeZoneInfoGraphType>();
                builder.CacheEntryStrategy = CacheStrategy;
            })
            .AddGitObjectDbConnection("Organization");
        break;
    case "Software":
        builder.Services
            .AddMemoryCache()
            .AddGitObjectDb(c => c.AddSystemTextJson())
            .AddSoftwareModel()
            .AddGitObjectDbApi()
            .AddGitObjectDbGraphQL(builder => builder.CacheEntryStrategy = CacheStrategy)
            .AddGitObjectDbConnection("Software", connection =>
            {
                var software = new Models.Software.DataGenerator(connection);
                var signature = new Signature("foo", "foo@acme.com", DateTimeOffset.Now);
                software.CreateData("Initial commit", signature);

                var application = software.Connection.GetApplications().First();
                signature = new Signature("foo", "foo@acme.com", DateTimeOffset.Now);
                software.Connection
                    .Update(c => c.CreateOrUpdate(application with { Description = "New description" }))
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
    .AddGitObjectDbGraphQLControllers(builder => builder
                                          .AddSystemTextJson()
        .UseApolloTracing(true)
        .UseMemoryCache(options =>
        {
            options.SizeLimit = 1_000_000;
            options.SlidingExpiration = null;
        }));

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

app.Run();
