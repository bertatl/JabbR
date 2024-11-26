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
public class JabbRFormsAuthenticationProvider : ICookieAuthenticationEvents
{
    private readonly IJabbrRepository _repository;
    private readonly IMembershipService _membershipService;

    public JabbRFormsAuthenticationProvider(IJabbrRepository repository, IMembershipService membershipService)
    {
        _repository = repository;
        _membershipService = membershipService;
    }

    public Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        return Task.CompletedTask;
    }

    public Task SigningIn(CookieSigningInContext context)
        {
            var authResult = new AuthenticationResult
            {
                Success = true
            };

            ChatUser loggedInUser = GetLoggedInUser(context);

            var principal = new ClaimsPrincipal(context.Identity);

            // Do nothing if it's authenticated
            if (principal.IsAuthenticated())
            {
                EnsurePersistentCookie(context);
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
                    context.Identity.AddClaim(new Claim(JabbRClaimTypes.Identifier, loggedInUser.Id));
                }
                else
                {
                    // Login this user
                    AddClaim(context, user);
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

    private static void AddClaim(CookieSigningInContext context, ChatUser user)
    {
        // Do nothing if the user is banned
        if (user.IsBanned)
        {
            return;
        }

        // Add the jabbr id claim
        context.Principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(JabbRClaimTypes.Identifier, user.Id) }));

        // Add the admin claim if the user is an Administrator
        if (user.IsAdmin)
        {
            context.Principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(JabbRClaimTypes.Admin, "true") }));
        }

        EnsurePersistentCookie(context);
    }

    private static void EnsurePersistentCookie(CookieSigningInContext context)
    {
        if (context.Properties == null)
        {
            context.Properties = new AuthenticationProperties();
        }

        context.Properties.IsPersistent = true;
    }

    private ChatUser GetLoggedInUser(CookieSigningInContext context)
    {
        var principal = context.Principal;

        if (principal != null)
        {
            return _repository.GetLoggedInUser(principal);
        }

        return null;
    }

    public Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    public Task RedirectToLogout(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    public Task RedirectToReturnUrl(RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
    }
}