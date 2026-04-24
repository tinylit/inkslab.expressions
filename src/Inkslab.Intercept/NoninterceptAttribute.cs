using System;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 不拦截拦截属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public sealed class NoninterceptAttribute : Attribute
    {
    }
}
