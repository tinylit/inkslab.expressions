using System;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 成员。
    /// </summary>
    public abstract class MemberExpression : Expression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回值类型。</param>
        protected MemberExpression(Type returnType) : base(returnType) { }

        /// <summary>
        /// 是否可读。
        /// </summary>
        public virtual bool CanRead => true;

        /// <summary>
        /// 是否为静态。
        /// </summary>
        public abstract bool IsStatic { get; }
    }
}
