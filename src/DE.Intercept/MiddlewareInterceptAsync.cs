using System.Threading.Tasks;

namespace Delta.Intercept
{
    /// <summary>
    /// 异步拦截器中间件。
    /// </summary>
    public class MiddlewareInterceptAsync : InterceptAsync
    {
        private int interceptCount = -1;
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
            try
            {
                interceptCount++;

                if (interceptAttributes.Length == interceptCount)
                {
                    return base.RunAsync(context);
                }

                return interceptAttributes[interceptCount].RunAsync(context, this);
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
            try
            {
                interceptCount++;

                if (interceptAttributes.Length == interceptCount)
                {
                    return base.RunAsync(context);
                }

                return interceptAttributes[interceptCount].RunAsync(context, this);
            }
            finally
            {
                interceptCount--;
            }
        }
    }
}
