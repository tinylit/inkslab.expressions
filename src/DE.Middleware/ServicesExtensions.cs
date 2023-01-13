using Delta.Middleware.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Delta.Middleware
{
    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public static class ServicesExtensions
    {
#if NET461_OR_GREATER && DEBUG
        private static readonly ModuleEmitter moduleEmitter = new ModuleEmitter(true, "Delta.Override.Middleware");
#else
        private static readonly ModuleEmitter moduleEmitter = new ModuleEmitter("Delta.Override.Middleware");
#endif

        /// <summary>
        /// 使用拦截器。
        /// 为标记了 <see cref="InterceptAttribute"/> 的接口、类或方法生成代理类。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
        public static IServiceCollection UseIntercept(this IServiceCollection services)
        {
            for (int i = 0; i < services.Count; i++)
            {
                ServiceDescriptor descriptor = services[i];

                if (!ProxyByServiceType.Intercept(descriptor))
                {
                    continue;
                }

                IProxyByPattern byPattern;

                if (descriptor.ImplementationType is null)
                {
                    if (descriptor.ImplementationInstance is null)
                    {
                        byPattern = new ProxyByFactory(moduleEmitter, descriptor.ServiceType, descriptor.ImplementationFactory, descriptor.Lifetime);
                    }
                    else
                    {
                        byPattern = new ProxyByInstance(moduleEmitter, descriptor.ServiceType, descriptor.ImplementationInstance);
                    }
                }
                else
                {
                    byPattern = new ProxyByImplementationType(moduleEmitter, descriptor.ServiceType, descriptor.ImplementationType, descriptor.Lifetime);
                }

                services[i] = byPattern.Ref();
            }

#if NET461_OR_GREATER && DEBUG
            moduleEmitter.SaveAssembly();
#endif

            return services;
        }
    }
}
