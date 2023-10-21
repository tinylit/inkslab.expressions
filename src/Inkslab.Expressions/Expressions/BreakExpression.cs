using System;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 跳出表达式。
    /// </summary>
    public class BreakExpression : Expression
    {
        private Label label;

        internal BreakExpression() { }

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            if (label is null)
            {
                throw new AstException("没有要中断的封闭式循环！");
            }

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected internal override void MarkLabel(Label label)
        {
            if (label is null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            if (label.Kind == LabelKind.Break)
            {
                this.label = label;
            }
        }
    }
}
