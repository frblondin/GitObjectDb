using GitObjectDb.Api.GraphQL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var controllers = builder.Services
    .AddGitObjectDb()
    .AddGitObjectDbConnection()
    .AddSoftwareModel()
    .AddControllers();

controllers
    .AddGitObjectDbOData("v1", ConnectionProvider.Model, o => o.Select().Filter().OrderBy().Expand())
    .AddGitObjectDbGraphQL(ConnectionProvider.Model);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseGraphQLAltair(new() { GraphQLEndPoint = "/api/graphql" }, "/");
}

app.UseHttpsRedirection()
   .UseAuthorization();

app.MapControllers();

app.Run();
