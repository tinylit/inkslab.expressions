using System;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 跳出表达式。
    /// </summary>
    public class ContinueExpression : Expression
    {
        private Label label;

        internal ContinueExpression() { }

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            if (label is null)
            {
                throw new AstException("没有要继续的封闭式循环！");
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

            if (label.Kind == LabelKind.Continue)
            {
                this.label = label;
            }
        }
    }
}
