using Angular.Bff;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBff()
    .AddRemoteApis();

builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "DualScheme";
            options.DefaultChallengeScheme = "DualScheme";
        }).AddPolicyScheme("DualScheme", "Dual Authentication", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                // Check if the request contains "Authorization" header (JWT)
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                return !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ")
                    ? "BearerDefault"
                    : "cookie";
            };
        }).AddCookie("cookie", options =>
        {
            options.Cookie.Name = "__Host-bff";
            options.Cookie.SameSite = SameSiteMode.Strict;
        }).AddOpenIdConnect("oidc", options =>
        {
            options.Authority = "https://st.id.cegid.cloud";
            options.ClientId = "financial-assistant-client-app";
            options.ClientSecret = "financial-assistant-client-app";
            options.ResponseType = "code";
            options.ResponseMode = "query";

            options.MapInboundClaims = false;
            options.RefreshInterval = TimeSpan.FromSeconds(30);
            options.RefreshOnIssuerKeyNotFound = true;
            options.RequireHttpsMetadata = true;
            options.SaveTokens = true;

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");

            options.Scope.Add("li-fes");

            options.TokenValidationParameters = new()
            {
                NameClaimType = "name",
                RoleClaimType = "role",
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = false,
                ValidateLifetime = true,
                ValidateTokenReplay = true,
                PropertyBag = new System.Collections.Generic.Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase)
            };
            })
            .AddScheme<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions, CustomJwtBearerHandler>(
                "BearerDefault",
                options =>
                {
                    options.Authority = "https://st.id.cegid.cloud";
                    options.Audience = "li-fes";
                    options.IncludeErrorDetails = true;
                    options.MapInboundClaims = false;
                    options.RefreshInterval = TimeSpan.FromSeconds(30);
                    options.RefreshOnIssuerKeyNotFound = true;
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = true;

                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role",
                        ValidateAudience = true,
                        ValidAudience = "li-fes",
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = false,
                        ValidateLifetime = true,
                        ValidateTokenReplay = true,
                        PropertyBag = new System.Collections.Generic.Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase)
                    };

                    var metadataAddress = "https://st.id.cegid.cloud/.well-known/openid-configuration";
                    options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        metadataAddress,
                        new OpenIdConnectConfigurationRetriever());

                    ////options.Events = new Cegid.Hydrogen.AspNetCore.Authentication.JwtBearer.HttpBearerChallengeEvents(
                    ////    this.BuildHttpBearerChallengeScope(hostConfiguration))
                    ////{
                    ////    OnAuthenticationFailed = this.OnJwtBearerAuthenticationFailedAsync,
                    ////    OnForbidden = this.OnJwtBearerForbiddenAsync,
                    ////    OnMessageReceived = this.OnJwtBearerMessageReceivedAsync,
                    ////    OnTokenValidated = this.OnJwtBearerTokenValidatedAsync,
                    ////    OnChallenge = this.OnJwtBearerChallengeAsync
                    ////};

                    ////this.ConfigureJwtBearerOptions(services, builder, options, hostConfiguration);
                });

////builder.Services.AddAuthorization(
////            (Microsoft.AspNetCore.Authorization.AuthorizationOptions options) =>
////            {
////                List<string> authenticationSchemes =
////                [
////                    "cookie"
////                ];

////                options.AddPolicy(
////                    "DefaultScope",
////                    (Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder policy) =>
////                    {
////                        policy.AuthenticationSchemes = authenticationSchemes;
////                        policy.RequireAssertion(context =>
////                            context.User.HasClaim(c => c.Type == "scope" &&
////                            c.Value.Contains("li-fes")));
////                    });
////            });

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseAuthentication();
////app.UseMiddleware<ScopeMiddleware>();
app.UseBff();
app.UseAuthorization();
app.MapBffManagementEndpoints();


// Comment this out to use the external api
app.MapGroup("/todos")
    .ToDoGroup()
    ////.RequireAuthorization("DefaultScope")
    .AsBffApiEndpoint();

app.MapFallbackToFile("/index.html");

app.Run();

