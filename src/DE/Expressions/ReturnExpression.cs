using System;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 返回表达式。
    /// </summary>
    public class ReturnExpression : Expression
    {
        private LocalBuilder local;
        private System.Reflection.Emit.Label label;

        internal ReturnExpression(Type returnType) : base(returnType)
        {
            IsReturnVoid = returnType == typeof(void);
        }

        /// <summary>
        /// 无返回值。
        /// </summary>
        public bool IsReturnVoid { get; }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (!IsReturnVoid)
            {
                ilg.Emit(OpCodes.Stloc, local);
            }

            ilg.Emit(OpCodes.Leave, label);
        }

        internal void Emit(System.Reflection.Emit.Label label)
        {
            if (IsReturnVoid)
            {
                this.label = label;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal void Emit(LocalBuilder local, System.Reflection.Emit.Label label)
        {
            if (IsReturnVoid)
            {
                throw new NotImplementedException();
            }
            else
            {
                this.local = local;
                this.label = label;
            }
        }
    }
}
