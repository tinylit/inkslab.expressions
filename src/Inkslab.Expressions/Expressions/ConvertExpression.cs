using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 类型转换。
    /// </summary>
    [DebuggerDisplay("({RuntimeType.Name}){body}")]
    public class ConvertExpression : Expression
    {
        private readonly Expression body;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <param name="convertToType">转换类型。</param>
        internal ConvertExpression(Expression body, Type convertToType) : base(convertToType)
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));

            if (body.IsVoid)
            {
                throw new AstException("表达式“void”无效！");
            }

            if (convertToType == typeof(void))
            {
                throw new AstException($"无法将“{body.RuntimeType}”转为“void”！");
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            body.Load(ilg);

            Type typeFrom = body.RuntimeType;

            if (typeFrom == RuntimeType)
            {
                return;
            }

            if (IsVoid)
            {
                ilg.Emit(OpCodes.Pop);

                return;
            }

            EmitUtils.EmitConvertToType(ilg, typeFrom, RuntimeType.IsByRef ? RuntimeType.GetElementType() : RuntimeType, true);
        }
    }
}
