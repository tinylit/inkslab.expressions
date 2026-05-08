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
        private readonly Expression _body;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <param name="convertToType">转换类型。</param>
        internal ConvertExpression(Expression body, Type convertToType) : base(convertToType)
        {
            _body = body;
        }

        /// <summary>
        /// 生成。
        /// <para>委托 <see cref="EmitUtils.EmitConvertToType"/> 处理，
        /// 该路径已适配 TypeBuilder 场景（使用 TypeBuilder.GetMethod / GetConstructor 替代反射）。</para>
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            _body.Load(ilg);

            Type typeFrom = _body.RuntimeType;

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
