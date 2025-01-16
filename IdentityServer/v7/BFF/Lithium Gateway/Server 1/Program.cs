using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
     .AddScheme<JwtBearerOptions, CustomJwtBearerHandler>(
        JwtBearerDefaults.AuthenticationScheme,
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
                ValidateIssuer = true,
                AudienceValidator = (audiences, securityToken, validationParameters) =>
                {
                    return true;
                },
                ValidateIssuerSigningKey = false,
                ValidateLifetime = true,
                ValidateTokenReplay = true,
                PropertyBag = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            };

            var metadataAddress = "https://st.id.cegid.cloud/.well-known/openid-configuration";

            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever());
        });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Comment this out to use the external api
app.MapGroup("/todos")
    .ToDoGroup();
    

app.Run();
