using System;
using System.Reflection;

namespace Delta.AOP
{
    /// <summary>
    /// 实现接口的调用。
    /// </summary>
    public class ImplementInvocation : IInvocation
    {
        private readonly object target;
        private readonly MethodInfo methodInfo;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="methodInfo">调用方法。</param>
        /// <exception cref="ArgumentNullException">参数 <paramref name="target"/> 或 <paramref name="methodInfo"/> 为 null。</exception>
        public ImplementInvocation(object target, MethodInfo methodInfo)
        {
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.methodInfo = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
        }

        /// <inheritdoc/>
        public object Invoke(object[] parameters) => methodInfo.Invoke(target, parameters);
    }
}
