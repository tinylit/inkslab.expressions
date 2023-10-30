using System;
using System.Threading.Tasks;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 异步有返回值拦截器。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ReturnValueInterceptAsyncAttribute : Attribute
    {
        /// <summary>
        /// 运行方法（异步有返回值）。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>

        public virtual Task<T> RunAsync<T>(InterceptContext context, InterceptAsync<T> intercept) => intercept.RunAsync(context);
    }
}
