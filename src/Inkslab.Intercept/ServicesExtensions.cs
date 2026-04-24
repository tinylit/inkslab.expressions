using Microsoft.Extensions.DependencyInjection;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public static class ServicesExtensions
    {
        /// <summary>
        /// 全局共享的代理方案，使用 <see cref="System.Lazy{T}"/> 保证线程安全的单次初始化。
        /// 多次调用 <see cref="UseIntercept"/> 复用同一 <see cref="ProxySolution"/> 和 <see cref="ModuleEmitter"/>，
        /// 避免重复生成代理类型和重复创建动态程序集。
        /// </summary>
        private static readonly System.Lazy<ProxySolution> _sharedSolution = new System.Lazy<ProxySolution>(() =>
        {
#if NET461_OR_GREATER && DEBUG
            var moduleEmitter = new ModuleEmitter(true, "Inkslab.Override.Intercept");
#else
            var moduleEmitter = new ModuleEmitter("Inkslab.Override.Intercept");
#endif
            return new ProxySolution(moduleEmitter);
        }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 使用拦截器。
        /// 为标记了 <see cref="InterceptAttribute"/> 的接口、类或方法生成代理类。
        /// 多次调用时，已分析过的类型直接命中缓存，不会重复代理或重复解析。
        /// </summary>
        /// <param name="services">服务集合。</param>
        /// <returns></returns>
        public static IServiceCollection UseIntercept(this IServiceCollection services)
        {
            var solution = _sharedSolution.Value;

            for (int i = 0; i < services.Count; i++)
            {
                services[i] = solution.Proxy(services[i]);
            }

            return services;
        }
    }
}
