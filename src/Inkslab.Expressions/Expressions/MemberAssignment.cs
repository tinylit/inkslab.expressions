using System.Reflection;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 成员赋值。
    /// </summary>
    public sealed class MemberAssignment
    {
        internal MemberAssignment(MemberInfo member, Expression expression)
        {
            Member = member;
            Expression = expression;
        }

        /// <summary>
        /// 成员。
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// 表达式。
        /// </summary>
        public Expression Expression { get; }
    }
}
