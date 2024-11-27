using System.Collections.Generic;
using System.Security.Claims;
using Nancy;
using Nancy.Security;

namespace JabbR.Nancy
{
    public class ClaimsPrincipalUserIdentity : IUserIdentity
    {
        public ClaimsPrincipalUserIdentity(ClaimsPrincipal claimsPrincipal)
        {
            ClaimsPrincipal = claimsPrincipal;
        }

        public ClaimsPrincipal ClaimsPrincipal { get; private set; }

        public IEnumerable<string> Claims
        {
            get => ClaimsPrincipal.Claims.Select(c => c.Type);
            set { }
        }

        public string UserName
        {
            get => ClaimsPrincipal.Identity.Name;
            set { }
        }
    }
}