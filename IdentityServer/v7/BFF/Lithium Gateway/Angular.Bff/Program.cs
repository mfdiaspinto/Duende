using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

var conf = builder.Configuration.GetSection("ReverseProxy");

builder.Services.AddReverseProxy()
    .LoadFromConfig(conf).AddTransforms(builderContext =>
    {
        builderContext.AddRequestTransform(async transformContext =>
        {
            var accessToken = await transformContext.HttpContext.GetTokenAsync("access_token");
            if (accessToken != null)
            {
                transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        });
    });

builder.Services.AddBff(o => {
    o.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjM2M0REMDZBRUQ4NzI1MzVBODFEQzIwMTY0NUREQTQ4RDBCMThDMjlSUzI1NiIsIng1dCI6Ik5qM1FhdTJISlRXb0hjSUJaRjNhU05DeGpDayIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL3N0LmlkLmNlZ2lkLmNsb3VkIiwibmJmIjoxNzM3MDE5MzI4LCJpYXQiOjE3MzcwMTkzMjgsImV4cCI6MTczNzAzMzcyOCwiYXVkIjpbImxpLWZlcyIsImh0dHBzOi8vc3QuaWQuY2VnaWQuY2xvdWQvcmVzb3VyY2VzIl0sInNjb3BlIjpbIm9wZW5pZCIsInByb2ZpbGUiLCJsaS1mZXMiXSwiYW1yIjpbInB3ZCJdLCJjbGllbnRfaWQiOiJmaW5hbmNpYWwtYXNzaXN0YW50LWNsaWVudC1hcHAiLCJzdWIiOiI5MGJiOGQ2ZS1hOGZhLTQyZDAtOTVlYS00Y2E1NjE5Mjk5NjQiLCJhdXRoX3RpbWUiOjE3MzcwMTkzMTcsImlkcCI6ImxvY2FsIiwiZW1haWwiOiJtZGlhc0BjZWdpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJzZWN1cml0eV9zdGFtcCI6IlNQN0xTU1lCQkk0TkZMTklaVUpXQ0dER05EUzZPQ01LIiwiZGlzcGxheV9uYW1lIjoiTWlndWVsIERpYXMiLCJjdWx0dXJlIjoiZW4tdXMiLCJwaWN0dXJlIjoiaHR0cHM6Ly9zdC1zcy5saXRoaXVtLnByaW1hdmVyYWJzcy5jb20vYXBpL3Y0LjAvdXNlcnBpY3R1cmUvOTBiYjhkNmUtYThmYS00MmQwLTk1ZWEtNGNhNTYxOTI5OTY0L3B1YmxpYyIsInNpZCI6IjFBQzVCQkZCMkE4MDMwRkQ4MERCNjIxRDNCNzZFMTMyIiwianRpIjoiRkJBNjA2RTU0Njk1MzM5QjcxNERGNUEwNzBCMjMxMjYifQ.55eqxnaWwgwfmzLtqv6HYMOAuxGI5UfCYhHT04Xy39EyWdQbuzmY_Oeost1ImCsV5omYuivVgc01l9HuDqxO-s7TR1mv9YX8kmLd7dXyFAbYbFEaFdU-2u4RicTf7y9noUFS9WFeidAiqCfKT6Qfpk4V1ODAWkBxB6ppHQPpP8SvdryFwWbhkFOMGZ6iB84FlrahVq0gkWFmNYHs13gCL9ERENQ7t-dzxO5Kkjq67Bubuoz6vBDNCwe7pgM9yvZCbOGJR3vlTGTZzdeHLBRIfR_Si6k5D6Sm5N-WYXQsy-qAScRvYWMjvMBdHmEpUxYt3bhOQWnYFfQnlKua0iwvsw";
    })
    .AddRemoteApis();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "cookie";
    options.DefaultChallengeScheme = "oidc";
    options.DefaultSignOutScheme = "oidc";
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
    });

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseBff();

app.MapBffReverseProxy();
app.MapBffManagementEndpoints();

app.UseRouting();

// Comment this out to use the external api

app.MapFallbackToFile("/index.html");

app.Run();

