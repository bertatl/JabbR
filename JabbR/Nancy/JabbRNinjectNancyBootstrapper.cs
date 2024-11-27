using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;

using Nancy;
using Nancy.Bootstrapper;
using Nancy.Bootstrappers.Ninject;
using Nancy.Configuration;
using Nancy.Owin;
using Nancy.Security;
using Microsoft.Owin;

using Ninject;
using JabbR.Nancy;

namespace JabbR.Nancy
{
    public class JabbRNinjectNancyBootstrapper : NinjectNancyBootstrapper
    {
        private readonly IKernel _kernel;
        private INancyEnvironment _environment;

        public JabbRNinjectNancyBootstrapper(IKernel kernel)
        {
            _kernel = kernel;
        }

        protected override IKernel GetApplicationContainer()
        {
            return _kernel;
        }

        protected override void ConfigureApplicationContainer(IKernel existingContainer)
        {
            base.ConfigureApplicationContainer(existingContainer);
        }

        protected override void RegisterNancyEnvironment(IKernel container, INancyEnvironment environment)
        {
            environment.AddValue("Environment", "Development");
            container.Bind<INancyEnvironment>().ToConstant(environment);
            _environment = environment;
        }

    public override INancyEnvironment GetEnvironment()
    {
        return _environment;
    }

    protected override Action<INancyEnvironment> GetEnvironmentConfigurator()
    {
        return environment =>
        {
            environment.AddValue("Environment", "Development");
        };
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
            var env = Get<IDictionary<string, object>>(context.Items, "owin.RequestEnvironment");
            if (env != null)
            {
                var principal = Get<IPrincipal>(env, "server.User") as ClaimsPrincipal;
                if (principal != null)
                {
                    context.CurrentUser = principal;
                }

                var appMode = Get<string>(env, "host.AppMode");

                if (!String.IsNullOrEmpty(appMode) &&
                    appMode.Equals("development", StringComparison.OrdinalIgnoreCase))
                {
                    context.Items["_debugMode"] = true;
                }
                else
                {
                    context.Items["_debugMode"] = false;
                }
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