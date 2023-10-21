﻿using System.Diagnostics;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 空合并运算。
    /// </summary>
	[DebuggerDisplay("{left} ?? {right}")]
    public class CoalesceExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        internal CoalesceExpression(Expression left, Expression right) : base(left.RuntimeType)
        {
            this.left = left ?? throw new System.ArgumentNullException(nameof(left));
            this.right = right ?? throw new System.ArgumentNullException(nameof(right));
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            left.Load(ilg);
            ilg.Emit(OpCodes.Dup);
            var label = ilg.DefineLabel();
            ilg.Emit(OpCodes.Brtrue_S, label);
            ilg.Emit(OpCodes.Pop);
            right.Load(ilg);
            ilg.MarkLabel(label);
        }
    }
}
