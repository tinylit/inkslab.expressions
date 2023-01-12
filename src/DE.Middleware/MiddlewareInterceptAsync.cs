using System.Threading.Tasks;

namespace Delta.Middleware
{
    /// <summary>
    /// 异步拦截器中间件。
    /// </summary>
    public class MiddlewareInterceptAsync : InterceptAsync
    {
        private int interceptCount = 0;
        private readonly InterceptAttribute[] interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareInterceptAsync(IInvocation invocation, InterceptAttribute[] interceptAttributes) : base(invocation)
        {
            this.interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override Task RunAsync(InterceptContext context)
        {
            if (interceptAttributes.Length > interceptCount)
            {
                InterceptAttribute interceptAttribute = interceptAttributes[interceptCount];

                interceptCount++;

                try
                {
                    return interceptAttribute.RunAsync(context, this);
                }
                finally
                {
                    interceptCount--;
                }
            }
            else
            {
                return base.RunAsync(context);
            }
        }
    }

    /// <summary>
    /// 异步拦截器中间件。
    /// </summary>
    public class MiddlewareInterceptAsync<T> : InterceptAsync<T>
    {
        private int interceptCount = 0;
        private readonly InterceptAttribute[] interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareInterceptAsync(IInvocation invocation, InterceptAttribute[] interceptAttributes) : base(invocation)
        {
            this.interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override Task<T> RunAsync(InterceptContext context)
        {
            if (interceptAttributes.Length > interceptCount)
            {
                InterceptAttribute interceptAttribute = interceptAttributes[interceptCount];

                interceptCount++;

                try
                {
                    return interceptAttribute.RunAsync(context, this);
                }
                finally
                {
                    interceptCount--;
                }
            }
            else
            {
                return base.RunAsync(context);
            }
        }
    }
}
