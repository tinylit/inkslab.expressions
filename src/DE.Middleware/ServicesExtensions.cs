using Delta.Emitters;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Delta.Middleware
{
    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public static class ServicesExtensions
    {
        private static readonly ModuleEmitter moduleEmitter = new ModuleEmitter();
        private static readonly Type interceptAttributeType = typeof(InterceptAttribute);

        /// <summary>
        /// 使用拦截器。
        /// 为标记了 <see cref="InterceptAttribute"/> 的接口、类或方法生成代理类。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
        public static IServiceCollection UseMiddleware(this IServiceCollection services)
        {
            for (int i = 0; i < services.Count; i++)
            {
                ServiceDescriptor descriptor = services[i];

                if (!Intercept(descriptor.ServiceType) && !Intercept(descriptor.ImplementationType))
                {
                    continue;
                }

                IProxyByPattern byPattern;

                if (descriptor.ImplementationType is null)
                {
                    if (descriptor.ImplementationInstance is null)
                    {
                        byPattern = new ProxyByInstance(moduleEmitter, descriptor.ServiceType, descriptor.ImplementationInstance);
                    }
                    else
                    {
                        byPattern = new ProxyByFactory(moduleEmitter, descriptor.ServiceType, descriptor.ImplementationFactory, descriptor.Lifetime);
                    }
                }
                else
                {
                    byPattern = new ProxyByImplementationType(moduleEmitter, descriptor.ServiceType, descriptor.ImplementationType, descriptor.Lifetime);

                }

                services[i] = byPattern.Ref();
            }

            return services;
        }

        private static bool Intercept(Type serviceType)
        {
            if (serviceType is null)
            {
                return false;
            }

            if (serviceType.IsDefined(interceptAttributeType, true))
            {
                return true;
            }

            foreach (var methodInfo in serviceType.GetMethods())
            {
                if (methodInfo.IsDefined(interceptAttributeType, true))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
