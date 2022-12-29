using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Delta.Expressions
{
    /// <summary>
    /// 字段。
    /// </summary>
    [DebuggerDisplay("{RuntimeType.Name} {field.Name}")]
    public class FieldExpression : MemberExpression
    {
        private readonly FieldInfo field;

        /// <summary>
        /// 是否可写。
        /// </summary>
        public override bool CanWrite => !(field.IsStatic || field.IsInitOnly);

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="field">字段。</param>
        internal FieldExpression(FieldInfo field) : base(field.FieldType)
        {
            this.field = field;
        }

        /// <summary>
        /// 获取成员数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected override void LoadCore(ILGenerator ilg)
        {
            if (field.IsStatic)
            {
                ilg.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                ilg.Emit(OpCodes.Ldfld, field);
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void AssignCore(ILGenerator ilg, Expression value)
        {
            if (field.IsStatic)
            {
                ilg.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                ilg.Emit(OpCodes.Stfld, field);
            }

            value.Load(ilg);
        }
    }
}
