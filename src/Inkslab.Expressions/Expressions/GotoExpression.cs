using System;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 返回。
    /// </summary>
    public class GotoExpression : Expression
    {
        private readonly Label _label;

        internal GotoExpression(Label label)
        {
            _label = label ?? throw new ArgumentNullException(nameof(label));
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg) => _label.Goto(ilg);
    }
}
