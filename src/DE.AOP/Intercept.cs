using System;

namespace Delta.AOP
{
    /// <summary>
    /// 拦截执行方法。
    /// </summary>
    public class Intercept
    {
        private readonly IInvocation invocation;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="invocation"/> 为null。</exception>
        public Intercept(IInvocation invocation)
        {
            this.invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
        }

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public virtual void Run(InterceptContext context) => invocation.Invoke(context.Inputs);
    }

    /// <summary>
    /// 拦截执行方法。
    /// </summary>
    public class Intercept<T>
    {
        private readonly IInvocation invocation;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="invocation"/> 为null。</exception>
        public Intercept(IInvocation invocation)
        {
            this.invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
        }

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public virtual T Run(InterceptContext context) => (T)invocation.Invoke(context.Inputs);
    }
}
