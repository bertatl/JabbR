using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using JabbR.Infrastructure;
using Microsoft.Owin;
using Owin;

namespace JabbR.Middleware
{
    public class CustomAuthHandler : OwinMiddleware
    {
        public CustomAuthHandler(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            var claimsPrincipal = context.Authentication.User as ClaimsPrincipal;

            if (claimsPrincipal != null &&
                !(claimsPrincipal is WindowsPrincipal) &&
                claimsPrincipal.Identity.IsAuthenticated &&
                !claimsPrincipal.IsAuthenticated() &&
                claimsPrincipal.HasClaim(ClaimTypes.NameIdentifier))
            {
                var identity = new ClaimsIdentity(claimsPrincipal.Claims, Constants.JabbRAuthType);

                var providerName = claimsPrincipal.GetIdentityProvider();

                if (string.IsNullOrEmpty(providerName))
                {
                    // If there's no provider name just add custom as the name
                    identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, "Custom"));
                }

                context.Authentication.SignIn(identity);
            }

            await Next.Invoke(context);
        }
    }
}