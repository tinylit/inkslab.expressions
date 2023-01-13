namespace Delta.Middleware
{
    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public class MiddlewareIntercept : Intercept
    {
        private int interceptCount = -1;
        private readonly InterceptAttribute[] interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareIntercept(IInvocation invocation, InterceptAttribute[] interceptAttributes) : base(invocation)
        {
            this.interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override void Run(InterceptContext context)
        {
            try
            {
                interceptCount++;

                if (interceptAttributes.Length == interceptCount)
                {
                    base.Run(context);
                }
                else
                {
                    interceptAttributes[interceptCount].Run(context, this);
                }
            }
            finally
            {
                interceptCount--;
            }
        }
    }

    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public class MiddlewareIntercept<T> : Intercept<T>
    {
        private int interceptCount = -1;
        private readonly InterceptAttribute[] interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareIntercept(IInvocation invocation, InterceptAttribute[] interceptAttributes) : base(invocation)
        {
            this.interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override T Run(InterceptContext context)
        {
            try
            {
                interceptCount++;

                if (interceptAttributes.Length == interceptCount)
                {
                    return base.Run(context);
                }
                else
                {
                    return interceptAttributes[interceptCount].Run(context, this);
                }
            }
            finally
            {
                interceptCount--;
            }
        }
    }
}
