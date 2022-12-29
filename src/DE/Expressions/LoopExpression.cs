using System;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 循环表达式。
    /// </summary>
    public class LoopExpression : BlockExpression
    {
        private readonly Label breakLabel;
        private readonly Label continueLabel;
        private GotoExpression breakAst;
        private GotoExpression continueAst;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">表达式hi类型。</param>
        /// <param name="breakLabel">跳出循环标记。</param>
        /// <param name="continueLabel">继续循环标记。</param>
        internal LoopExpression(Type returnType, Label breakLabel, Label continueLabel) : base(returnType)
        {
            this.breakLabel = breakLabel;
            this.continueLabel = continueLabel;
        }

        /// <summary>
        /// 继续执行代码块。
        /// </summary>
        public GotoExpression Continue => continueAst ??= Goto(continueLabel);

        /// <summary>
        /// 跳出循环。
        /// </summary>
        public GotoExpression Break => breakAst ??= Goto(breakLabel);

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (IsEmpty)
            {
                throw new AstException("代码块为空！");
            }

            if (RuntimeType == typeof(void))
            {
                continueLabel.MarkLabel(ilg);

                base.Load(ilg);

                breakLabel.MarkLabel(ilg);

                ilg.Emit(OpCodes.Nop);
            }
            else
            {
                var local = ilg.DeclareLocal(RuntimeType);

                continueLabel.MarkLabel(ilg);

                base.Load(ilg);

                breakLabel.MarkLabel(ilg);

                ilg.Emit(OpCodes.Ldloc, local);
            }
        }
    }
}
