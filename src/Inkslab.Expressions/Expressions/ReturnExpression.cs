using System;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 返回表达式。
    /// </summary>
    public class ReturnExpression : Expression
    {
        private Label label;
        private VariableExpression variable;
        private readonly Expression body;

        internal ReturnExpression() { }

        internal ReturnExpression(Expression body) : base(body.RuntimeType)
        {
            if (body.IsVoid)
            {
                throw new AstException("表达式“void”无效！");
            }

            this.body = body;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(System.Reflection.Emit.ILGenerator ilg)
        {
            if (label is null)
            {
                throw new NullReferenceException(nameof(label));
            }

            if (body is null)
            {

            }
            else if (variable is null)
            {
                throw new AstException("由于代码块是无返回值，返回表达式必须也是无返回值类型！");
            }
            else
            {
                Assign(variable, body)
                    .Load(ilg);
            }

            label.Goto(ilg);
        }

        /// <inheritdoc/>
        protected internal override void MarkLabel(Label label)
        {
            if (label is null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            if (label.Kind == LabelKind.Return)
            {
                this.label = label;
            }
        }

        /// <inheritdoc/>
        protected internal override void StoredLocal(VariableExpression variable)
        {
            if (body is null)
            {
                throw new NotSupportedException();
            }

            this.variable = variable ?? throw new ArgumentNullException(nameof(variable));
        }

        /// <inheritdoc/>
        protected internal override bool DetectionResult(Type returnType) => ConvertChecked(returnType, RuntimeType);
    }
}
