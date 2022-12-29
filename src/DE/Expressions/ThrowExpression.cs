using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 抛出异常。
    /// </summary>
    [DebuggerDisplay("throw new {RuntimeType.Name}({errorMsg})")]
    public class ThrowExpression : Expression
    {
        private readonly Expression exception;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        internal ThrowExpression(Type exceptionType) : this(new NewInstanceExpression(exceptionType))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        /// <param name="errorMsg">异常消息。</param>
        internal ThrowExpression(Type exceptionType, string errorMsg) : this(new NewInstanceExpression(exceptionType, new ConstantExpression(errorMsg)))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="exception">异常。</param>
        internal ThrowExpression(Expression exception) : base(exception.RuntimeType)
        {
            this.exception = exception ?? throw new ArgumentNullException(nameof(exception));

            if (!exception.RuntimeType.IsSubclassOf(typeof(Exception)))
            {
                throw new AstException("参数不是异常类型!");
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            exception.Load(ilg);

            ilg.Emit(OpCodes.Throw);
        }
    }
}
