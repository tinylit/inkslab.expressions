using System;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 返回。
    /// </summary>
    public class GotoExpression : Expression
    {
        private readonly Label label;

        internal GotoExpression(Label label) : base(typeof(void))
        {
            this.label = label;
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg) => label.Goto(ilg);
    }
}
