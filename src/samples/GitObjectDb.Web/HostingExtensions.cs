using Fga.Net.AspNetCore;
using Fga.Net.AspNetCore.Authorization;
using Fga.Net.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace GitObjectDb.Web;

internal static class HostingExtensions
{
    public static WebApplicationBuilder AddAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "oidc";
        })
            .AddJwtBearer("Bearer", options =>
            {
                //options.Authority = builder.Configuration.GetServiceUri("identityserver", "https")!.AbsoluteUri;
                options.Authority = "https://localhost:5001";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                };
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
            })
            .AddOpenIdConnect("oidc", options =>
            {
                //options.Authority = builder.Configuration.GetServiceUri("identityserver", "https")!.AbsoluteUri;
                options.Authority = "https://localhost:5001";

                options.ClientId = "web";
                options.ClientSecret = "secret";
                options.ResponseType = "code";

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
                options.GetClaimsFromUserInfoEndpoint = true;

                options.SaveTokens = true;
            });
        return builder;
    }

    public static WebApplicationBuilder AddAuthorization(this WebApplicationBuilder builder)
    {
        // https://docs.fga.dev/integration
        // https://github.com/Hawxy/Fga.Net

        builder.Services.AddOpenFga(options =>
        {
            options.WithAuth0FgaDefaults(builder.Configuration["Auth0Fga:ClientId"], builder.Configuration["Auth0Fga:ClientSecret"]);

            options.StoreId = builder.Configuration["Auth0Fga:StoreId"];
        }, config =>
        {
            config.UserIdentityResolver = principal => principal.Claims?.FirstOrDefault(c => c.Type == "given_name")?.Value?.ToLower()!;
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiScope", policy =>
            {
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "api1");
            });
            options.AddPolicy(FgaAuthorizationDefaults.PolicyKey, policy =>
            {
                policy.RequireAuthenticatedUser()
                      .AddFgaRequirement();
            });
        });

        return builder;
    }
}