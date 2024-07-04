using Microsoft.Extensions.DependencyInjection;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public static class ServicesExtensions
    {
#if NET461_OR_GREATER && DEBUG
        private static readonly ModuleEmitter moduleEmitter = new ModuleEmitter(true, "Inkslab.Override.Intercept");
#else
        private static readonly ModuleEmitter moduleEmitter = new ModuleEmitter("Inkslab.Override.Intercept");
#endif

        /// <summary>
        /// 使用拦截器。
        /// 为标记了 <see cref="InterceptAttribute"/> 的接口、类或方法生成代理类。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
        public static IServiceCollection UseIntercept(this IServiceCollection services)
        {
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
