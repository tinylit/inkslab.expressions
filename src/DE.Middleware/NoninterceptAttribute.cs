using System;

namespace Delta.Middleware
{
    /// <summary>
    /// 不拦截拦截属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class NoninterceptAttribute : Attribute
    {
    }
}
