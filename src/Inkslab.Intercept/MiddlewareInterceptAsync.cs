using System.Threading.Tasks;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 异步拦截器中间件。
    /// </summary>
    public class MiddlewareInterceptAsync : InterceptAsync
    {
        private int interceptCount = -1;
        private readonly InterceptAsyncAttribute[] interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareInterceptAsync(IInvocation invocation, InterceptAsyncAttribute[] interceptAttributes) : base(invocation)
        {
            this.interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override async Task RunAsync(InterceptContext context)
        {
            try
            {
                interceptCount++;

                if (interceptAttributes.Length == interceptCount)
                {
                    await base.RunAsync(context);
                }
                else
                {
                    await interceptAttributes[interceptCount].RunAsync(context, this);
                }
            }
            finally
            {
                interceptCount--;
            }
        }
    }

    /// <summary>
    /// 异步拦截器中间件。
    /// </summary>
    public class MiddlewareInterceptAsync<T> : InterceptAsync<T>
    {
        private int interceptCount = -1;
        private readonly InterceptAsyncAttribute[] interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareInterceptAsync(IInvocation invocation, InterceptAsyncAttribute[] interceptAttributes) : base(invocation)
        {
            this.interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override async Task<T> RunAsync(InterceptContext context)
        {
            try
            {
                interceptCount++;

                if (interceptAttributes.Length == interceptCount)
                {
                    return await base.RunAsync(context);
                }

                return await interceptAttributes[interceptCount].RunAsync(context, this);
            }
            finally
            {
                interceptCount--;
            }
        }
    }
}
