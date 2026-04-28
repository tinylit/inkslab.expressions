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
        private readonly Expression _instanceAst;
        private readonly FieldInfo _field;

        /// <inheritdoc/>
        public override bool CanWrite => !_field.IsInitOnly;

        /// <inheritdoc/>
        public override bool IsStatic => _field.IsStatic;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="field">字段。</param>
        internal FieldExpression(FieldInfo field) : base(field.FieldType)
        {
            if (field.IsStatic)
            {
                _field = field;
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
                    _field = field;
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
                _field = field;
                _instanceAst = instanceAst;
            }
        }


        /// <summary>
        /// 获取成员数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (_field.IsStatic)
            {
                ilg.Emit(OpCodes.Ldsfld, _field);
            }
            else
            {
                _instanceAst.Load(ilg);

                ilg.Emit(OpCodes.Ldfld, _field);
            }
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected override void Assign(ILGenerator ilg, Expression value)
        {
            if (_field.IsStatic)
            {
                value.Load(ilg);

                ilg.Emit(OpCodes.Stsfld, _field);
            }
            else
            {
                _instanceAst.Load(ilg);

                value.Load(ilg);

                ilg.Emit(OpCodes.Stfld, _field);
            }
        }
    }
}
