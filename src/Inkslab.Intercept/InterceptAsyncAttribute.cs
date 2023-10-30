using System;
using System.Threading.Tasks;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 异步拦截器。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class InterceptAsyncAttribute : ReturnValueInterceptAsyncAttribute
    {
        /// <summary>
        /// 运行方法（异步无返回值）。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>

        public virtual Task RunAsync(InterceptContext context, InterceptAsync intercept) => intercept.RunAsync(context);
    }
}
