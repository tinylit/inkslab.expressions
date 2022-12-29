using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Delta.Expressions
{
    /// <summary>
    /// 属性。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class PropertyExpression : Expression
    {
        private readonly PropertyInfo property;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="property">属性。</param>
        internal PropertyExpression(PropertyInfo property) : base(property.PropertyType)
        {
            this.property = property;
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                var sb = new StringBuilder();

                sb.Append(RuntimeType.Name)
                    .Append(" ")
                    .Append(property.Name)
                    .Append("{ ");

                if (property.CanRead)
                {
                    sb.Append("get; ");
                }

                if (property.CanWrite)
                {
                    sb.Append("set; ");
                }

                return sb.Append('}').ToString();
            }
        }

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => property.CanWrite;

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (!property.CanRead)
            {
                throw new AstException($"{property.Name}不可读!");
            }

            var method = property.GetGetMethod() ?? property.GetGetMethod(true);

            if (method.IsStatic || method.DeclaringType.IsValueType)
            {
                ilg.Emit(OpCodes.Call, method);
            }
            else
            {
                ilg.Emit(OpCodes.Callvirt, method);
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void Assign(ILGenerator ilg, Expression value)
        {
            value.Load(ilg);

            var method = property.GetSetMethod() ?? property.GetSetMethod(true);

            if (method.IsStatic || method.DeclaringType.IsValueType)
            {
                ilg.Emit(OpCodes.Call, method);
            }
            else
            {
                ilg.Emit(OpCodes.Callvirt, method);
            }
        }
    }
}
