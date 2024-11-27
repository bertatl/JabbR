using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;

namespace JabbR.Nancy
{
    public class ClaimsPrincipalUserIdentity : IIdentity
    {
        public ClaimsPrincipalUserIdentity(ClaimsPrincipal claimsPrincipal)
        {
            ClaimsPrincipal = claimsPrincipal;
        }

        public ClaimsPrincipal ClaimsPrincipal { get; private set; }

        public IEnumerable<string> Claims
        {
            get;
            set;
        }

        public string Name => ClaimsPrincipal.Identity.Name;

        public string AuthenticationType => ClaimsPrincipal.Identity.AuthenticationType;

        public bool IsAuthenticated => ClaimsPrincipal.Identity.IsAuthenticated;
    }
}