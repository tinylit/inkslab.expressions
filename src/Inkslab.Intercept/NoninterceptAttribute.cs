using System;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 不拦截拦截属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class NoninterceptAttribute : Attribute
    {
    }
}
