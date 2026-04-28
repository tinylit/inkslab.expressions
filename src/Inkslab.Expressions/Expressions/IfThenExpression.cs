using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 判断。
    /// </summary>
    [DebuggerDisplay("if({testExp})\\{ {ifTrue} \\}")]
    public class IfThenExpression : Expression
    {
        private readonly Expression _test;
        private readonly Expression _ifTrue;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        internal IfThenExpression(Expression test, Expression ifTrue)
        {
            this._test = test ?? throw new ArgumentNullException(nameof(test));

            if (test.RuntimeType == typeof(bool))
            {
                this._ifTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue));
            }
            else
            {
                throw new ArgumentException("不是有效的条件语句!", nameof(test));
            }
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            var label = ilg.DefineLabel();

            _test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            _ifTrue.Load(ilg);

            if (_ifTrue.RuntimeType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(label);
        }

        /// <inheritdoc/>
        protected internal override void MarkLabel(Label label)
        {
            _ifTrue.MarkLabel(label);
        }
        /// <inheritdoc/>
        protected internal override void StoredLocal(VariableExpression variable)
        {
            _ifTrue.StoredLocal(variable);
        }
    }
}
