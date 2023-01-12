using System;
using System.Threading.Tasks;

namespace Delta.Middleware
{
    /// <summary>
    /// 拦截执行方法。
    /// </summary>
    public class InterceptAsync
    {
        private readonly IInvocation invocation;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="invocation"/> 为null。</exception>
        public InterceptAsync(IInvocation invocation)
        {
            this.invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
        }

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public virtual Task RunAsync(InterceptContext context)
        {
            var returnValue = invocation.Invoke(context.Inputs);

            if (returnValue is ValueTask valueTask)
            {
                return valueTask.AsTask();
            }

            return (Task)returnValue;
        }
    }

    /// <summary>
    /// 拦截执行方法。
    /// </summary>
    public class InterceptAsync<T>
    {
        private readonly IInvocation invocation;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="invocation"/> 为null。</exception>
        public InterceptAsync(IInvocation invocation)
        {
            this.invocation = invocation ?? throw new ArgumentNullException(nameof(invocation));
        }

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="context">上下文。</param>
        public virtual Task<T> RunAsync(InterceptContext context)
        {
            var returnValue = invocation.Invoke(context.Inputs);

            if (returnValue is ValueTask<T> valueTask)
            {
                return valueTask.AsTask();
            }

            return (Task<T>)returnValue;
        }
    }
}
