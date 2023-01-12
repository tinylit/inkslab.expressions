using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 循环表达式。
    /// </summary>
    public class LoopExpression : BlockExpression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        internal LoopExpression()
        {
        }

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

            var breakLabel = new Label(LabelKind.Break);
            var continueLabel = new Label(LabelKind.Continue);

            MarkLabel(breakLabel);
            MarkLabel(continueLabel);

            continueLabel.MarkLabel(ilg);

            base.Load(ilg);

            breakLabel.MarkLabel(ilg);

            ilg.Emit(OpCodes.Nop);
        }

        /// <inheritdoc/>
        protected internal override void MarkLabel(Label label)
        {
            if (label.Kind == LabelKind.Return)
            {
                base.MarkLabel(label);
            }
        }
    }
}
