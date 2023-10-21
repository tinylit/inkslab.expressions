using Microsoft.Extensions.DependencyInjection;
using System;

namespace Inkslab.Intercept.Patterns
{
    class ProxyByInstance : ProxyByInstanceArgument
    {
        private readonly Type serviceType;
        private readonly object instance;

        public ProxyByInstance(ModuleEmitter moduleEmitter, Type serviceType, object instance) : base(moduleEmitter, serviceType)
        {
            this.serviceType = serviceType;
            this.instance = instance;
        }

        public override ServiceDescriptor Ref()
        {
            var overrideType = OverrideType();

            return new ServiceDescriptor(serviceType, serviceProvider =>
            {
                return Activator.CreateInstance(overrideType, serviceProvider, instance);

            }, ServiceLifetime.Singleton);
        }
    }
}
