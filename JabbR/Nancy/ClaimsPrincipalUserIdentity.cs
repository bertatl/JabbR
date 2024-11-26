using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using Nancy.Security;

namespace JabbR.Nancy
{
    public class ClaimsPrincipalUserIdentity : IUserIdentity, IIdentity
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

        public string UserName
        {
            get;
            set;
        }

        public string AuthenticationType => ClaimsPrincipal?.Identity?.AuthenticationType;

        public bool IsAuthenticated => ClaimsPrincipal?.Identity?.IsAuthenticated ?? false;

        public string Name => UserName;
    }
}