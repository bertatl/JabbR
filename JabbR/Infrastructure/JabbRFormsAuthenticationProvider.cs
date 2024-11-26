using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using JabbR.Models;
using JabbR.Services;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace JabbR.Infrastructure
{
    public class JabbRFormsAuthenticationProvider : IAuthenticationHandler
    {
        private readonly IJabbrRepository _repository;
        private readonly IMembershipService _membershipService;
        private HttpContext _context;
        private AuthenticationScheme _scheme;

        public JabbRFormsAuthenticationProvider(IJabbrRepository repository, IMembershipService membershipService)
        {
            _repository = repository;
            _membershipService = membershipService;
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _scheme = scheme;
            _context = context;
            return Task.CompletedTask;
        }

        public Task<AuthenticateResult> AuthenticateAsync()
        {
            // Implement your authentication logic here
            // For now, we'll return no result
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            // Implement your challenge logic here
            return Task.CompletedTask;
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            // Implement your forbid logic here
            return Task.CompletedTask;
        }

        [Obsolete("This method is obsolete. Use AuthenticateAsync instead.")]
        public Task ValidateIdentity(object context)
        {
            return Task.CompletedTask;
        }

        [Obsolete("This method is obsolete. Use AuthenticateAsync instead.")]
        public void ResponseSignIn(HttpContext context)
        {
            var authResult = new AuthenticationResult
            {
                Success = true
            };

            var principal = new ClaimsPrincipal(context?.User?.Identity ?? new ClaimsIdentity());

            ChatUser loggedInUser = GetLoggedInUser(principal);

            // Do nothing if it's authenticated
            if (principal.Identity.IsAuthenticated)
            {
                var properties = new AuthenticationProperties();
                EnsurePersistentCookie(properties);
                return;
            }

            ChatUser user = _repository.GetUser(principal);
            authResult.ProviderName = principal.GetIdentityProvider();

            // The user exists so add the claim
            if (user != null)
            {
                if (loggedInUser != null && user != loggedInUser)
                {
                    // Set an error message
                    authResult.Message = String.Format(LanguageResources.Account_AccountAlreadyLinked, authResult.ProviderName);
                    authResult.Success = false;

                    // Keep the old user logged in
                    context.User.AddIdentity(new ClaimsIdentity(new[] { new Claim(JabbRClaimTypes.Identifier, loggedInUser.Id) }));
                }
                else
                {
                    // Login this user
                    var properties = new AuthenticationProperties();
                    AddClaim(principal, properties, user);
                }
            }
            else if (principal.HasAllClaims())
            {
                ChatUser targetUser = null;

                // The user doesn't exist but the claims to create the user do exist
                if (loggedInUser == null)
                {
                    // New user so add them
                    user = _membershipService.AddUser(principal);

                    targetUser = user;
                }
                else
                {
                    // If the user is logged in then link
                    _membershipService.LinkIdentity(loggedInUser, principal);

                    _repository.CommitChanges();

                    authResult.Message = String.Format(LanguageResources.Account_AccountLinkedSuccess, authResult.ProviderName);

                    targetUser = loggedInUser;
                }

                AddClaim(context, targetUser);
            }
            else if(!principal.HasPartialIdentity())
            {
                // A partial identity means the user needs to add more claims to login
                context.Identity.AddClaim(new Claim(JabbRClaimTypes.PartialIdentity, "true"));
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true
            };

            context.Response.Cookies.Append(Constants.AuthResultCookie,
                                       JsonConvert.SerializeObject(authResult),
                                       cookieOptions);
        }

        private static void AddClaim(ClaimsPrincipal principal, AuthenticationProperties properties, ChatUser user)
        {
            // Do nothing if the user is banned
            if (user.IsBanned)
            {
                return;
            }

            // Add the jabbr id claim
            principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(JabbRClaimTypes.Identifier, user.Id) }));

            // Add the admin claim if the user is an Administrator
            if (user.IsAdmin)
            {
                principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(JabbRClaimTypes.Admin, "true") }));
            }

            EnsurePersistentCookie(properties);
        }

        private static void EnsurePersistentCookie(AuthenticationProperties properties)
        {
            if (properties == null)
            {
                properties = new AuthenticationProperties();
            }

            properties.IsPersistent = true;
        }

        private ChatUser GetLoggedInUser(ClaimsPrincipal principal)
        {
            return principal != null ? _repository.GetLoggedInUser(principal) : null;
        }
    }
}