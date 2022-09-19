using GitObjectDb.Api.Model;
using GitObjectDb.Model;
using GraphQL;

var builder = WebApplication.CreateBuilder(args);

var repositoryType = args.Length > 0 ? args[0] : null;

IDataModel model;

DtoTypeEmitter emitter;

switch (repositoryType)
{
    case "Software":
        builder.Services
            .AddSoftwareModel(out model)
            .AddGitObjectDbOData(model, out emitter)
            .AddGitObjectDbGraphQL(model, out _)
            .AddGitObjectDbConnection(model, "Software", connection =>
            {
                var software = new DataGenerator(connection);
                software.CreateData("Initial commit", new("foo", "foo@acme.com", DateTimeOffset.Now));

                var application = software.Connection.GetApplications().First();
                software.Connection
                    .Update(c => c.CreateOrUpdate(application with { Description = "New description" }))
                    .Commit(new("Update appication",
                                new("foo", "foo@acme.com", DateTimeOffset.Now),
                                new("foo", "foo@acme.com", DateTimeOffset.Now)));
            });
        break;
    case "Organization":
        builder.Services
            .AddOrganizationModel(out model)
            .AddGitObjectDbOData(model, out emitter)
            .AddGitObjectDbGraphQL(model, out _)
            .AddGitObjectDbConnection(model, "Organization");
        break;
    default:
        throw new NotSupportedException($"'{repositoryType}' is not supported.");
}

builder.Services
    .AddControllers()
    .AddGitObjectDbODataControllers("v1", emitter, o => o.Select().Filter().OrderBy().Expand())
    .AddGitObjectDbGraphQLControllers(emitter, b => b.UseApolloTracing(true));

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
