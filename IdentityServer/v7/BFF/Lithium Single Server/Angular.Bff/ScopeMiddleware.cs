using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

public class ScopeMiddleware
{
    private readonly RequestDelegate _next;

    public ScopeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;
        var a  = await context.GetTokenAsync("access_token");
        // Adicionar a claim 'scope' se necessário, por exemplo, de um cabeçalho ou outra fonte
        if (user.Identity.IsAuthenticated && !user.HasClaim(c => c.Type == "scope"))
        {
            var scopeClaim = new Claim("scope", "li-fes2");
            var identity = (ClaimsIdentity)user.Identity;
            identity.AddClaim(scopeClaim);
        }

        await _next(context);
    }
}
