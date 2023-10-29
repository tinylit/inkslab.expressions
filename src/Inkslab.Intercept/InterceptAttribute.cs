﻿using System;

namespace Inkslab.Intercept
{
    /// <summary>
    /// 拦截属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class InterceptAttribute : Attribute
    {
        /// <summary>
        /// 运行方法（无返回值）。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>
        public virtual void Run(InterceptContext context, Intercept intercept) => intercept.Run(context);

        /// <summary>
        /// 运行方法（非异步且有返回值）。
        /// </summary>
        /// <param name="context">上下文。</param>
        /// <param name="intercept">拦截器。</param>
        public virtual T Run<T>(InterceptContext context, Intercept<T> intercept) => intercept.Run(context);
    }
}
