using System;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 返回表达式。
    /// </summary>
    public class ReturnExpression : Expression
    {
        private Label _label;
        private VariableExpression _variable;
        private readonly Expression _body;

        internal ReturnExpression() { }

        internal ReturnExpression(Expression body) : base(body.RuntimeType)
        {
            if (body.IsVoid)
            {
                throw new AstException("表达式“void”无效！");
            }

            this._body = body;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(System.Reflection.Emit.ILGenerator ilg)
        {
            if (_label is null)
            {
                throw new NullReferenceException(nameof(_label));
            }

            if (_body is null)
            {

            }
            else if (_variable is null)
            {
                throw new AstException("由于代码块是无返回值，返回表达式必须也是无返回值类型！");
            }
            else
            {
                Assign(_variable, _body)
                    .Load(ilg);
            }

            _label.Goto(ilg);
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
                this._label = label;
            }
        }

        /// <inheritdoc/>
        protected internal override void StoredLocal(VariableExpression variable)
        {
            if (_body is null)
            {
                throw new NotSupportedException();
            }

            this._variable = variable ?? throw new ArgumentNullException(nameof(variable));
        }

        /// <inheritdoc/>
        protected internal override bool DetectionResult(Type returnType) => ConvertChecked(returnType, RuntimeType);
    }
}
