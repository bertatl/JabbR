using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using JabbR.Models;
using JabbR.Services;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace JabbR.Infrastructure
{
    public class JabbRFormsAuthenticationProvider : IAuthenticationSignInHandler
    {
        private readonly IJabbrRepository _repository;
        private readonly IMembershipService _membershipService;
        private AuthenticationScheme _scheme;
        private HttpContext _context;

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
            // Implement authentication logic here
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            // Implement challenge logic here
            return Task.CompletedTask;
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            // Implement forbid logic here
            return Task.CompletedTask;
        }

        public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
        {
            var authResult = new AuthenticationResult
            {
                Success = true
            };

            ChatUser loggedInUser = GetLoggedInUser(user);

            // Do nothing if it's authenticated
            if (user.Identity.IsAuthenticated)
            {
                EnsurePersistentCookie(properties);
                return Task.CompletedTask;
            }

            ChatUser chatUser = _repository.GetUser(user);
            authResult.ProviderName = user.Identity.AuthenticationType;

            // The user exists so add the claim
            if (chatUser != null)
            {
                if (loggedInUser != null && chatUser != loggedInUser)
                {
                    // Set an error message
                    authResult.Message = String.Format(LanguageResources.Account_AccountAlreadyLinked, authResult.ProviderName);
                    authResult.Success = false;

                    // Keep the old user logged in
                    ((ClaimsIdentity)user.Identity).AddClaim(new Claim(JabbRClaimTypes.Identifier, loggedInUser.Id));
                }
                else
                {
                    // Login this user
                    AddClaim(user, chatUser, properties);
                }
            }
            else if (HasAllRequiredClaims(user))
            {
                ChatUser targetUser = null;

                // The user doesn't exist but the claims to create the user do exist
                if (loggedInUser == null)
                {
                    // New user so add them
                    chatUser = _membershipService.AddUser(user);

                    targetUser = chatUser;
                }
                else
                {
                    // If the user is logged in then link
                    _membershipService.LinkIdentity(loggedInUser, user);

                    _repository.CommitChanges();

                    authResult.Message = String.Format(LanguageResources.Account_AccountLinkedSuccess, authResult.ProviderName);

                    targetUser = loggedInUser;
                }

                AddClaim(user, targetUser, properties);
            }
            else if (!HasPartialIdentity(user))
            {
                // A partial identity means the user needs to add more claims to login
                ((ClaimsIdentity)user.Identity).AddClaim(new Claim(JabbRClaimTypes.PartialIdentity, "true"));
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true
            };

            _context.Response.Cookies.Append(Constants.AuthResultCookie,
                                       JsonConvert.SerializeObject(authResult),
                                       cookieOptions);

            return Task.CompletedTask;
        }

        public Task SignOutAsync(AuthenticationProperties properties)
        {
            // Implement sign out logic here
            return Task.CompletedTask;
        }

        private static void AddClaim(ClaimsPrincipal principal, ChatUser user, AuthenticationProperties properties)
        {
            // Do nothing if the user is banned
            if (user.IsBanned)
            {
                return;
            }

            // Add the jabbr id claim
            ((ClaimsIdentity)principal.Identity).AddClaim(new Claim(JabbRClaimTypes.Identifier, user.Id));

            // Add the admin claim if the user is an Administrator
            if (user.IsAdmin)
            {
                ((ClaimsIdentity)principal.Identity).AddClaim(new Claim(JabbRClaimTypes.Admin, "true"));
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
            return _repository.GetLoggedInUser(principal);
        }

        private bool HasAllRequiredClaims(ClaimsPrincipal principal)
        {
            // Implement the logic to check if the principal has all required claims
            return true; // Placeholder
        }

        private bool HasPartialIdentity(ClaimsPrincipal principal)
        {
            // Implement the logic to check if the principal has a partial identity
            return false; // Placeholder
        }
    }
}