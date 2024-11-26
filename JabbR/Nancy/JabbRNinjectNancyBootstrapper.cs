using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;

using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Ninject;
using Nancy.Owin;
using Nancy.Security;
using Nancy.Configuration;
using Microsoft.AspNetCore.Http;

using Ninject;

namespace JabbR.Nancy
{
    public class JabbRNinjectNancyBootstrapper : NinjectNancyBootstrapper
    {
        private readonly IKernel _kernel;

        public JabbRNinjectNancyBootstrapper(IKernel kernel)
        {
            _kernel = kernel;
        }

        protected override IKernel GetApplicationContainer()
        {
            return _kernel;
        }

        public override INancyEnvironment GetEnvironment()
        {
            return new DefaultNancyEnvironment();
        }

        protected override void RegisterNancyEnvironment(IKernel container, INancyEnvironment environment)
        {
            container.Bind<INancyEnvironment>().ToConstant(environment);
        }

        protected override void ApplicationStartup(IKernel container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            Csrf.Enable(pipelines);

            pipelines.BeforeRequest.AddItemToStartOfPipeline(FlowPrincipal);
            pipelines.BeforeRequest.AddItemToStartOfPipeline(SetCulture);
        }

        private Response FlowPrincipal(NancyContext context)
        {
            var httpContext = context.GetHttpContext();
            if (httpContext != null)
            {
                var principal = httpContext.User as ClaimsPrincipal;
                if (principal != null)
                {
                    context.CurrentUser = new ClaimsPrincipalUserIdentity(principal);
                }

                var appMode = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                context.Items["_debugMode"] = !string.IsNullOrEmpty(appMode) &&
                    appMode.Equals("Development", StringComparison.OrdinalIgnoreCase);
            }

            return null;
        }

        private Response SetCulture(NancyContext ctx)
        {
            Thread.CurrentThread.CurrentCulture = ctx.Culture;
            Thread.CurrentThread.CurrentUICulture = ctx.Culture;
            return null;
        }

        private static T Get<T>(IDictionary<string, object> env, string key)
        {
            object value;
            if (env.TryGetValue(key, out value))
            {
                return (T)value;
            }
            return default(T);
        }
    }
}