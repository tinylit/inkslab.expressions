using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 数组长度。
    /// </summary>
    [DebuggerDisplay("{array}.Length")]
    public class ArrayLengthExpression : Expression
    {
        private readonly Expression array;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="array"></param>
        internal ArrayLengthExpression(Expression array) : base(typeof(int))
        {
            this.array = array ?? throw new ArgumentNullException(nameof(array));

            if (!array.RuntimeType.IsArray || !EmitUtils.IsAssignableFromSignatureTypes(typeof(Array), array.RuntimeType) || array.RuntimeType.GetArrayRank() > 1)
            {
                throw new ArgumentException("不是数组，或不是一维数组!", nameof(array));
            }
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            array.Load(ilg);

            ilg.Emit(OpCodes.Ldlen);
        }
    }
}
