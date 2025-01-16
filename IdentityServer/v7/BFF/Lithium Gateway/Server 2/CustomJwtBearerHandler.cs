
/// <summary>
/// Defines a custom <see cref="Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler" /> to validate token issuers depending on configuration.
/// </summary>
internal partial class CustomJwtBearerHandler : Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler
{
    #region Code

    #region Private Fields

    private Microsoft.Extensions.Logging.ILogger logger;

    #endregion

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomJwtBearerHandler" /> class.
    /// </summary>
    /// <param name="options">The configuration options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public CustomJwtBearerHandler(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
        this.logger = logger.CreateLogger<CustomJwtBearerHandler>();
    }

    #endregion

    #region Protected Methods

    /// <inheritdoc />
    protected override async System.Threading.Tasks.Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        // Expected issuer

        Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration? configuration = null;

        if (this.Options.ConfigurationManager != null)
        {
            configuration = await this.Options.ConfigurationManager.GetConfigurationAsync(
                this.Context.RequestAborted)
                .ConfigureAwait(false);
        }

        string? issuer = configuration?.Issuer;

        if (string.IsNullOrWhiteSpace(issuer))
        {
            LogInvalidConfiguration(this.logger!, this.Scheme.Name);

            return Microsoft.AspNetCore.Authentication.AuthenticateResult.Fail("Invalid configuration options. Issuer is not defined.");
        }

        // Read the token

        string? token = this.ReadToken();

        if (!string.IsNullOrWhiteSpace(token))
        {
            LogValidatingToken(this.logger!, this.Scheme.Name, issuer);

            System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();

            if (handler.CanReadToken(token))
            {
                System.IdentityModel.Tokens.Jwt.JwtSecurityToken securityToken = handler.ReadJwtToken(token);

                if (securityToken != null)
                {
                    if (issuer == securityToken.Issuer)
                    {
                        // The token was issued by the expected authority,
                        // perform the actual validation

                        Microsoft.AspNetCore.Authentication.AuthenticateResult result = await base.HandleAuthenticateAsync().ConfigureAwait(false);

                        if (result.Succeeded)
                        {
                            LogTokenValidationSucceeded(this.logger!, this.Scheme.Name);
                        }
                        else
                        {
                            LogTokenValidationFailed(this.logger!, this.Scheme.Name);
                        }

                        return result;
                    }

                    if (this.Options.TokenValidationParameters?.PropertyBag?.TryGetValue("AltAuthority", out object? altAuthorityObj) ?? false)
                    {
                        string? altAuthority = altAuthorityObj as string;
                        if (!string.IsNullOrWhiteSpace(altAuthority) && altAuthority == securityToken.Issuer)
                        {
                            // The token was issued by another authority that will be validated by
                            // a different handler instance,
                            // avoid failure to prevent logging errors that are not actually errors

                            LogTokenIssuedByAltAuthority(this.logger!, this.Scheme.Name, altAuthority);

                            return Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult();
                        }
                    }
                }
                else
                {
                    LogTokenCouldNotBeRead(this.logger!, this.Scheme.Name);
                }
            }
            else
            {
                LogTokenCannotBeRead(this.logger!, this.Scheme.Name);
            }
        }

        // Default behavior

        LogPerformingDefaultValidation(this.logger!, this.Scheme.Name);

        var a = await base.HandleAuthenticateAsync().ConfigureAwait(false);

        return a;
    }

    #endregion

    #region Private Methods

    [Microsoft.Extensions.Logging.LoggerMessage(Level = LogLevel.Error, Message = "Custom JWT ({Scheme}): JWT bearer handler configuration is invalid. Issuer is null.")]
    private static partial void LogInvalidConfiguration(Microsoft.Extensions.Logging.ILogger logger, string? scheme);

    [Microsoft.Extensions.Logging.LoggerMessage(Level = LogLevel.Debug, Message = "Custom JWT ({Scheme}): resorting to default (base) validation...")]
    private static partial void LogPerformingDefaultValidation(Microsoft.Extensions.Logging.ILogger logger, string? scheme);

    [Microsoft.Extensions.Logging.LoggerMessage(Level = LogLevel.Debug, Message = "Custom JWT ({Scheme}): the token cannot be read.")]
    private static partial void LogTokenCannotBeRead(Microsoft.Extensions.Logging.ILogger logger, string? scheme);

    [Microsoft.Extensions.Logging.LoggerMessage(Level = LogLevel.Warning, Message = "Custom JWT ({Scheme}): the token could not be read by JwtSecurityTokenHandler.")]
    private static partial void LogTokenCouldNotBeRead(Microsoft.Extensions.Logging.ILogger logger, string? scheme);

    [Microsoft.Extensions.Logging.LoggerMessage(Level = LogLevel.Debug, Message = "Custom JWT ({Scheme}): token was issued by alternative authority '{AltAuthority}'. Ignoring.")]
    private static partial void LogTokenIssuedByAltAuthority(Microsoft.Extensions.Logging.ILogger logger, string? scheme, string? altAuthority);

    [Microsoft.Extensions.Logging.LoggerMessage(Level = LogLevel.Warning, Message = "Custom JWT ({Scheme}): token validation failed.")]
    private static partial void LogTokenValidationFailed(Microsoft.Extensions.Logging.ILogger logger, string? scheme);

    [Microsoft.Extensions.Logging.LoggerMessage(Level = LogLevel.Debug, Message = "Custom JWT ({Scheme}): token validation succeeded.")]
    private static partial void LogTokenValidationSucceeded(Microsoft.Extensions.Logging.ILogger logger, string? scheme);

    [Microsoft.Extensions.Logging.LoggerMessage(Level = LogLevel.Debug, Message = "Custom JWT ({Scheme}): validating bearer token (expected issuer: '{Issuer}')...")]
    private static partial void LogValidatingToken(Microsoft.Extensions.Logging.ILogger logger, string? scheme, string? issuer);

    private string? ReadToken()
    {
        string? token = null;

        string? authorization = this.Request.Headers["Authorization"];

        if (string.IsNullOrEmpty(authorization))
        {
            return token;
        }

        if (authorization.StartsWith($"Bearer "))
        {
            token = authorization.Substring($"Bearer ".Length).Trim();
        }

        return token;
    }

    #endregion

    #endregion
}
