using System.Threading.Tasks;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 异步拦截器中间件。
    /// </summary>
    public class MiddlewareInterceptAsync : InterceptAsync
    {
        private int _interceptCount = -1;
        private readonly InterceptAsyncAttribute[] _interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareInterceptAsync(IInvocation invocation, InterceptAsyncAttribute[] interceptAttributes) : base(invocation)
        {
            _interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override async Task RunAsync(InterceptContext context)
        {
            try
            {
                _interceptCount++;

                if (_interceptAttributes.Length == _interceptCount)
                {
                    await base.RunAsync(context);
                }
                else
                {
                    await _interceptAttributes[_interceptCount].RunAsync(context, this);
                }
            }
            finally
            {
                _interceptCount--;
            }
        }
    }

    /// <summary>
    /// 异步拦截器中间件。
    /// </summary>
    public class MiddlewareInterceptAsync<T> : InterceptAsync<T>
    {
        private int _interceptCount = -1;
        private readonly ReturnValueInterceptAsyncAttribute[] _interceptAttributes;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="invocation">调用。</param>
        /// <param name="interceptAttributes">拦截标记。</param>
        public MiddlewareInterceptAsync(IInvocation invocation, ReturnValueInterceptAsyncAttribute[] interceptAttributes) : base(invocation)
        {
            _interceptAttributes = interceptAttributes;
        }

        /// <inheritdoc/>
        public override async Task<T> RunAsync(InterceptContext context)
        {
            try
            {
                _interceptCount++;

                if (_interceptAttributes.Length == _interceptCount)
                {
                    return await base.RunAsync(context);
                }

                return await _interceptAttributes[_interceptCount].RunAsync(context, this);
            }
            finally
            {
                _interceptCount--;
            }
        }
    }
}
