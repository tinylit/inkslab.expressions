using System;
using System.Threading.Tasks;

namespace Delta.Intercept
{
    /// <summary>
    /// 拦截属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public abstract class InterceptAttribute : Attribute
    {
        /// <summary>
        /// 运行方法（无返回值）。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>
        public virtual void Run(InterceptContext context, Intercept intercept) => intercept.Run(context);

        /// <summary>
        /// 运行方法（非异步且有返回值）。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>
        public virtual T Run<T>(InterceptContext context, Intercept<T> intercept) => intercept.Run(context);

        /// <summary>
        /// 运行方法（异步无返回值）。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>

        public virtual Task RunAsync(InterceptContext context, InterceptAsync intercept) => intercept.RunAsync(context);

        /// <summary>
        /// 运行方法（异步有返回值）。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>

        public virtual Task<T> RunAsync<T>(InterceptContext context, InterceptAsync<T> intercept) => intercept.RunAsync(context);
    }
}
