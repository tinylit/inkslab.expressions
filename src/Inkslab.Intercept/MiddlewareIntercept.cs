namespace Inkslab.Intercept
{
    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public class MiddlewareIntercept : Intercept
    {
        private int _interceptCount = -1;
        private readonly InterceptAttribute[] _interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareIntercept(IInvocation invocation, InterceptAttribute[] interceptAttributes) : base(invocation)
        {
            _interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override void Run(InterceptContext context)
        {
            try
            {
                _interceptCount++;

                if (_interceptAttributes.Length == _interceptCount)
                {
                    base.Run(context);
                }
                else
                {
                    _interceptAttributes[_interceptCount].Run(context, this);
                }
            }
            finally
            {
                _interceptCount--;
            }
        }
    }

    /// <summary>
    /// 拦截中间件。
    /// </summary>
    public class MiddlewareIntercept<T> : Intercept<T>
    {
        private int _interceptCount = -1;
        private readonly ReturnValueInterceptAttribute[] _interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareIntercept(IInvocation invocation, ReturnValueInterceptAttribute[] interceptAttributes) : base(invocation)
        {
            _interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override T Run(InterceptContext context)
        {
            try
            {
                _interceptCount++;

                if (_interceptAttributes.Length == _interceptCount)
                {
                    return base.Run(context);
                }
                else
                {
                    return _interceptAttributes[_interceptCount].Run(context, this);
                }
            }
            finally
            {
                _interceptCount--;
            }
        }
    }
}
