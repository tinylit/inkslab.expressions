using Microsoft.Extensions.DependencyInjection;
using System;

namespace Delta.AOP.Patterns
{
    class ProxyByFactory : ProxyByInstanceArgument
    {
        private readonly Type serviceType;
        private readonly Func<IServiceProvider, object> implementationFactory;
        private readonly ServiceLifetime lifetime;

        public ProxyByFactory(ModuleEmitter moduleEmitter, Type serviceType, Func<IServiceProvider, object> implementationFactory, ServiceLifetime lifetime) : base(moduleEmitter, serviceType)
        {
            this.serviceType = serviceType;
            this.implementationFactory = implementationFactory;
            this.lifetime = lifetime;
        }

        public override ServiceDescriptor Ref()
        {
            var overrideType = OverrideType();

            return new ServiceDescriptor(serviceType, serviceProvider =>
            {
                var instance = implementationFactory.Invoke(serviceProvider);

                return Activator.CreateInstance(overrideType, instance);

            }, lifetime);
        }
    }
}
