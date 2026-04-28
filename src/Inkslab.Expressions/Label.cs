using System;
using System.Reflection.Emit;

namespace Inkslab
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
        private bool _initLabel = true;
        private bool _markLabel = true;

        private readonly LabelKind _labelKind;

        private System.Reflection.Emit.Label _label;

        /// <summary>
        /// 构造函数。
        /// </summary>
        internal Label(LabelKind labelKind)
        {
            this._labelKind = labelKind;
        }

        /// <summary>
        /// 标记类型。
        /// </summary>
        public LabelKind Kind => _labelKind;
        
        internal void Goto(ILGenerator ilg)
        {
            if (_initLabel)
            {
                _initLabel = false;

                _label = ilg.DefineLabel();
            }

            switch (_labelKind)
            {
                case LabelKind.Return:
                    ilg.Emit(OpCodes.Leave, _label);
                    break;
                case LabelKind.Goto:
                case LabelKind.Break:
                case LabelKind.Continue:
                default:
                    ilg.Emit(OpCodes.Br, _label);
                    break;
            }
        }

        internal void MarkLabel(ILGenerator ilg)
        {
            if (_initLabel)
            {
                _initLabel = false;

                _label = ilg.DefineLabel();
            }

            if (_markLabel)
            {
                _markLabel = false;

                ilg.MarkLabel(_label);
            }
            else
            {
                throw new AstException("相同标签不允许重复标记！");
            }
        }
    }
}
