using System.Diagnostics;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 数组长度。
    /// </summary>
    [DebuggerDisplay("{array}.Length")]
    public class ArrayLengthExpression : Expression
    {
        private readonly Expression _array;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="array"></param>
        internal ArrayLengthExpression(Expression array) : base(typeof(int))
        {
            _array = array;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            _array.Load(ilg);

            ilg.Emit(OpCodes.Ldlen);
        }
    }
}
