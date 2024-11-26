using System;
using System.Diagnostics.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Ninject;
using Ninject.Syntax;


namespace JabbR.Infrastructure
{
    public class NinjectDependencyScope : IServiceProvider, IDisposable
    {
        private IResolutionRoot resolver;

        internal NinjectDependencyScope(IResolutionRoot resolver)
        {
            Contract.Assert(resolver != null);

            this.resolver = resolver;
        }

        public void Dispose()
        {
            if (resolver is IDisposable disposable)
                disposable.Dispose();

            resolver = null;
        }

        public object GetService(Type serviceType)
        {
            if (resolver == null)
                throw new ObjectDisposedException("this", "This scope has already been disposed");

            return resolver.TryGet(serviceType);
        }
    }

    public class NinjectWebApiDependencyResolver : NinjectDependencyScope, IServiceScopeFactory
    {
        private readonly IKernel kernel;

        public NinjectWebApiDependencyResolver(IKernel kernel)
            : base(kernel)
        {
            this.kernel = kernel;
        }

        public IServiceScope CreateScope()
        {
            return new NinjectServiceScope(new NinjectDependencyScope(kernel.BeginBlock()));
        }
    }

    public class NinjectServiceScope : IServiceScope
    {
        private readonly NinjectDependencyScope scope;

        public NinjectServiceScope(NinjectDependencyScope scope)
        {
            this.scope = scope;
        }

        public IServiceProvider ServiceProvider => scope;

        public void Dispose()
        {
            scope.Dispose();
        }
    }
}