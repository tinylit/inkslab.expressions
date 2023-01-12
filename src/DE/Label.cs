using System;
using System.Reflection.Emit;

namespace Delta
{
    /// <summary>
    /// 标记类型。
    /// </summary>
    public enum LabelKind
    {
        /// <summary>
        /// 跳转。
        /// </summary>
        Goto,
        /// <summary>
        /// 跳出。
        /// </summary>
        Break,
        /// <summary>
        /// 继续。
        /// </summary>
        Continue,
        /// <summary>
        /// 返回。
        /// </summary>
        Return
    }

    /// <summary>
    /// 标签。
    /// </summary>
    public sealed class Label
    {
        private bool initLabel = true;
        private bool markLabel = true;

        private readonly LabelKind labelKind;

        private System.Reflection.Emit.Label label;

        /// <summary>
        /// 构造函数。
        /// </summary>
        internal Label(LabelKind labelKind)
        {
            this.labelKind = labelKind;
        }

        /// <summary>
        /// 标记类型。
        /// </summary>
        public LabelKind Kind => labelKind;
        
        internal void Goto(ILGenerator ilg)
        {
            if (initLabel)
            {
                initLabel = false;

                label = ilg.DefineLabel();
            }

            switch (labelKind)
            {
                case LabelKind.Return:
                    ilg.Emit(OpCodes.Leave, label);
                    break;
                case LabelKind.Goto:
                case LabelKind.Break:
                case LabelKind.Continue:
                default:
                    ilg.Emit(OpCodes.Br, label);
                    break;
            }
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
