using System.Collections.Generic;
using System.Security.Claims;
using Nancy.Authentication;
using Microsoft.AspNetCore.Identity;

namespace JabbR.Nancy
{
    public class ClaimsPrincipalUserIdentity : IUserIdentity
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ClaimsPrincipalUserIdentity(ClaimsPrincipal claimsPrincipal, UserManager<ApplicationUser> userManager)
        {
            ClaimsPrincipal = claimsPrincipal;
            _userManager = userManager;
        }

        public ClaimsPrincipal ClaimsPrincipal { get; private set; }

        public IEnumerable<string> Claims
        {
            get;
            set;
        }

        public string UserName
        {
            get => _userManager.GetUserName(ClaimsPrincipal);
            set => UserName = value;
        }

        public string UserId => _userManager.GetUserId(ClaimsPrincipal);
    }
}