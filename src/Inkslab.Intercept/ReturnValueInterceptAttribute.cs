using System;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 有返回值拦截器。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ReturnValueInterceptAttribute : ReturnValueInterceptAsyncAttribute
    {
        /// <summary>
        /// 运行方法（非异步且有返回值）。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>
        public virtual T Run<T>(InterceptContext context, Intercept<T> intercept) => intercept.Run(context);
    }
}
