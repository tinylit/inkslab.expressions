using System;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 变量。
    /// </summary>
    public class VariableExpression : Expression
    {
        private LocalBuilder local;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="variableType">类型。</param>
        internal VariableExpression(Type variableType) : base(variableType)
        {
        }

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (local is null)
            {
                local = ilg.DeclareLocal(RuntimeType);
            }

            ilg.Emit(OpCodes.Ldloc, local);
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void Assign(ILGenerator ilg, Expression value)
        {
            if (local is null)
            {
                local = ilg.DeclareLocal(RuntimeType);
            }

            value.Load(ilg);

            ilg.Emit(OpCodes.Stloc, local);
        }

        /// <summary>
        /// 重写。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{RuntimeType.Name} variable";
        }
    }
}
