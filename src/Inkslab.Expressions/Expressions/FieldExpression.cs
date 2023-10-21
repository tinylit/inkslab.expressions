using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 字段。
    /// </summary>
    [DebuggerDisplay("{RuntimeType.Name} {field.Name}")]
    public class FieldExpression : MemberExpression
    {
        private readonly Expression instanceAst;
        private readonly FieldInfo field;

        /// <inheritdoc/>
        public override bool CanWrite => !field.IsInitOnly;

        /// <inheritdoc/>
        public override bool IsStatic => field.IsStatic;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="field">字段。</param>
        internal FieldExpression(FieldInfo field) : base(field.FieldType)
        {
            if (field.IsStatic)
            {
                this.field = field;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceAst">字段所在实例。</param>
        /// <param name="field">字段。</param>
        internal FieldExpression(Expression instanceAst, FieldInfo field) : base(field.FieldType)
        {
            if (field.IsStatic)
            {
                if (instanceAst is null)
                {
                    this.field = field;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else if (instanceAst is null)
            {
                throw new InvalidOperationException();
            }
            else
            {
                this.field = field;
                this.instanceAst = instanceAst;
            }
        }


        /// <summary>
        /// 获取成员数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (field.IsStatic)
            {
                ilg.Emit(OpCodes.Ldsfld, field);
            }
            else
            {
                instanceAst.Load(ilg);

                ilg.Emit(OpCodes.Ldfld, field);
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void Assign(ILGenerator ilg, Expression value)
        {
            if (field.IsStatic)
            {
                value.Load(ilg);

                ilg.Emit(OpCodes.Stsfld, field);
            }
            else
            {
                instanceAst.Load(ilg);

                value.Load(ilg);

                ilg.Emit(OpCodes.Stfld, field);
            }
        }
    }
}
