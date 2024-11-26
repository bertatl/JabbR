using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using JabbR.Services;

namespace JabbR.Nancy
{
    public class JabbRAuthenticationCallbackProvider
    {
        private readonly IJabbrRepository _repository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public JabbRAuthenticationCallbackProvider(IJabbrRepository repository, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
        {
            _repository = repository;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> ProcessAsync(string returnUrl, ExternalLoginInfo info)
        {
            if (info == null)
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user == null)
            {
                user = new IdentityUser { UserName = info.Principal.FindFirstValue(ClaimTypes.Email) };
                await _userManager.CreateAsync(user);
                await _userManager.AddLoginAsync(user, info);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            return LocalRedirect(returnUrl ?? "~/");
        }

        public IActionResult OnRedirectToAuthenticationProviderError(string errorMessage)
        {
            // Handle error
            return null;
        }
    }
}