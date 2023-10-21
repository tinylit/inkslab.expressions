using System;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 标签表达式。
    /// </summary>
    public class LabelExpression : Expression
    {
        private readonly Label label;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="label">标签。</param>
        internal LabelExpression(Label label) : base(typeof(void))
        {
            this.label = label ?? throw new ArgumentNullException(nameof(label));
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg) => label.MarkLabel(ilg);
    }
}
