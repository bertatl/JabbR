using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Nancy.Security;

namespace JabbR.Nancy
{
    public class ClaimsPrincipalUserIdentity : IUserIdentity
    {
        public ClaimsPrincipalUserIdentity(ClaimsPrincipal claimsPrincipal)
        {
            ClaimsPrincipal = claimsPrincipal;
            UserName = claimsPrincipal?.Identity?.Name;
            Claims = claimsPrincipal?.Claims.Select(c => c.Value).ToList() ?? new List<string>();
        }

        public ClaimsPrincipal ClaimsPrincipal { get; private set; }

        public IEnumerable<string> Claims { get; }

        public string UserName { get; }
    }
}