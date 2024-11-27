using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.RegularExpressions;

using JabbR.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace JabbR.Nancy
{
    public class JabbRAuthenticationCallbackProvider
    {
        private readonly IJabbrRepository _repository;

        public JabbRAuthenticationCallbackProvider(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public async Task<IResult> ProcessAsync(HttpContext context, AuthenticateResult result)
        {
            if (!result.Succeeded)
            {
                return Results.Redirect("/");
            }

            var principal = result.Principal;
            var claims = new List<Claim>(principal.Claims);

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var providerName = principal.FindFirstValue(ClaimTypes.AuthenticationMethod);

            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            if (!string.IsNullOrEmpty(providerName))
            {
                claims.Add(new Claim(ClaimTypes.AuthenticationMethod, providerName));
            }

            await context.SignInAsync(new ClaimsPrincipal(new ClaimsIdentity(claims, "ExternalLogin")));

            return Results.Redirect("/account/#identityProviders");
        }
    }
}