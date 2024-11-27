using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.RegularExpressions;

using JabbR.Services;

namespace JabbR.Nancy
{
    public class JabbRAuthenticationCallbackProvider
    {
        private readonly IJabbrRepository _repository;

        public JabbRAuthenticationCallbackProvider(IJabbrRepository repository)
        {
            _repository = repository;
        }

        public string Process(ClaimsPrincipal user, string returnUrl)
        {
            if (user == null || !user.Identity.IsAuthenticated)
            {
                return "~/";
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return "~" + returnUrl;
            }

            return "~/account/#identityProviders";
        }

        public void AddUserClaims(ClaimsPrincipal user, string providerName)
        {
            if (user == null || !user.Identity.IsAuthenticated)
            {
                return;
            }

            var claims = new List<Claim>();
            var nameIdentifier = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(nameIdentifier))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
            }

            claims.Add(new Claim(ClaimTypes.AuthenticationMethod, providerName));

            var userName = user.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(userName))
            {
                claims.Add(new Claim(ClaimTypes.Name, userName));
            }

            var email = user.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
            {
                claims.Add(new Claim(ClaimTypes.Email, email));
            }

            ((ClaimsIdentity)user.Identity).AddClaims(claims);
        }

        public string OnRedirectToAuthenticationProviderError(string errorMessage)
        {
            // You might want to log the error or handle it in some way
            return "~/error";
        }
    }
}