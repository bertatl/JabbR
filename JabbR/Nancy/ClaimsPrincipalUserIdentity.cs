using System.Collections.Generic;
using System.Security.Claims;
using Nancy.Security;
using System.Security.Principal;

namespace JabbR.Nancy
{
    public class ClaimsPrincipalUserIdentity : IUserIdentity, IIdentity
    {
        public ClaimsPrincipalUserIdentity(ClaimsPrincipal claimsPrincipal)
        {
            ClaimsPrincipal = claimsPrincipal;
            UserName = claimsPrincipal?.Identity?.Name;
        }

        public ClaimsPrincipal ClaimsPrincipal { get; private set; }

        public IEnumerable<string> Claims
        {
            get => ClaimsPrincipal?.Claims.Select(c => c.Value);
            set { }
        }

        public string UserName { get; set; }

        public string AuthenticationType => ClaimsPrincipal?.Identity?.AuthenticationType;

        public bool IsAuthenticated => ClaimsPrincipal?.Identity?.IsAuthenticated ?? false;

        public string Name => UserName;
    }
}