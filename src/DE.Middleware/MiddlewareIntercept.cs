namespace Delta.Middleware
{
    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public class MiddlewareIntercept : Intercept
    {
        private int interceptCount = 0;
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
            if (interceptAttributes.Length > interceptCount)
            {
                InterceptAttribute interceptAttribute = interceptAttributes[interceptCount];

                interceptCount++;

                try
                {
                    interceptAttribute.Run(context, this);
                }
                finally
                {
                    interceptCount--;
                }
            }
            else
            {
                base.Run(context);
            }
        }
    }

    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public class MiddlewareIntercept<T> : Intercept<T>
    {
        private int interceptCount = 0;
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
            if (interceptAttributes.Length > interceptCount)
            {
                InterceptAttribute interceptAttribute = interceptAttributes[interceptCount];

                interceptCount++;

                try
                {
                    return interceptAttribute.Run(context, this);
                }
                finally
                {
                    interceptCount--;
                }
            }
            else
            {
                return base.Run(context);
            }
        }
    }
}
