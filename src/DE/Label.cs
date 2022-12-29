using System.Reflection.Emit;

namespace Delta
{
    /// <summary>
    /// 标签。
    /// </summary>
    public sealed class Label
    {
        private bool initLabel = true;
        private bool markLabel = true;
        private System.Reflection.Emit.Label label;

        /// <summary>
        /// 构造函数。
        /// </summary>
        internal Label()
        {
        }

        internal void Goto(ILGenerator ilg)
        {
            if (initLabel)
            {
                initLabel = false;

                label = ilg.DefineLabel();
            }

            ilg.Emit(OpCodes.Br_S, label);
        }

        internal void MarkLabel(ILGenerator ilg)
        {
            if (initLabel)
            {
                initLabel = false;

                label = ilg.DefineLabel();
            }

            if (markLabel)
            {
                markLabel = false;

                ilg.MarkLabel(label);
            }
            else
            {
                throw new AstException("相同标签不允许重复标记！");
            }
        }
    }
}
