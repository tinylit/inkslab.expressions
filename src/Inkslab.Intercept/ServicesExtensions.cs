using Microsoft.Extensions.DependencyInjection;
using System;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public static class ServicesExtensions
    {
        /// <summary>
        /// 使用拦截器。
        /// 为标记了 <see cref="InterceptAttribute"/> 的接口、类或方法生成代理类。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
        public static IServiceCollection UseIntercept(this IServiceCollection services)
        {
            // 为每次调用创建独立的ModuleEmitter，使用GUID确保唯一性
            var uniqueModuleName = $"Inkslab.Override.Intercept.{Guid.NewGuid():N}";
            
#if NET461_OR_GREATER && DEBUG
            var moduleEmitter = new ModuleEmitter(true, uniqueModuleName);
#else
            var moduleEmitter = new ModuleEmitter(uniqueModuleName);
#endif

            var solution = new ProxySolution(moduleEmitter);

            for (int i = 0; i < services.Count; i++)
            {
                services[i] = solution.Proxy(services[i]);
            }

#if NET461_OR_GREATER && DEBUG
            moduleEmitter.SaveAssembly();
#endif
            return services;
        }
    }
}
