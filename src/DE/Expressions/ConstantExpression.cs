using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 常量。
    /// </summary>
    public class ConstantExpression : Expression
    {
        private readonly object value;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值。</param>
        internal ConstantExpression(object value) : this(value, (value is MethodInfo || value is MethodEmitter) ? typeof(MethodInfo) : (value is Type || value is AbstractTypeEmitter) ? typeof(Type) : value?.GetType() ?? typeof(object))
        {
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="type">值类型。</param>
        internal ConstantExpression(object value, Type type) : base(type)
        {
            if (value is null)
            {
                if (type.IsValueType && !type.IsNullable())
                {
                    throw new NotSupportedException($"常量null，不能对值类型({type})进行转换!");
                }

                this.value = value;
            }
            else if ((value is Type || value is AbstractTypeEmitter) ? type == typeof(Type) : value is MethodEmitter ? type == typeof(MethodInfo) : value.GetType() == type || type.IsAssignableFrom(value.GetType()))
            {
                this.value = value;
            }
            else
            {
                throw new NotSupportedException($"常量值类型({value.GetType()})和指定类型({type})无法进行转换!");
            }
        }

        /// <summary>
        /// 空的。
        /// </summary>
        public bool IsNull => value is null;

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg) => EmitUtils.EmitConstantOfType(ilg, value, RuntimeType);

        /// <summary>
        /// 重写。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if(IsNull)
            {
                return "null";
            }

            return value.ToString();
        }
    }
}
