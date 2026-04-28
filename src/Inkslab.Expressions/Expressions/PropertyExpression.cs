using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 属性。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class PropertyExpression : MemberExpression
    {
        private readonly bool _isStatic;
        private readonly Expression _instanceAst;
        private readonly PropertyInfo _property;
        private readonly MethodInfo _getter;
        private readonly MethodInfo _setter;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="property">属性。</param>
        internal PropertyExpression(PropertyInfo property) : base(property.PropertyType)
        {
            _getter = property.GetMethod ?? property.GetGetMethod(true);
            _setter = property.SetMethod ?? property.GetSetMethod(true);

            MethodInfo methodInfo = _getter ?? _setter;

            if (_isStatic = methodInfo.IsStatic)
            {
                this._property = property;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="instanceAst">属性所在实例。</param>
        /// <param name="property">属性。</param>
        internal PropertyExpression(Expression instanceAst, PropertyInfo property) : base(property.PropertyType)
        {
            _getter = property.GetMethod ?? property.GetGetMethod(true);
            _setter = property.SetMethod ?? property.GetSetMethod(true);

            MethodInfo methodInfo = _getter ?? _setter;

            if (_isStatic = methodInfo.IsStatic)
            {
                if (instanceAst is null)
                {
                    this._property = property;
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
                this._instanceAst = instanceAst;
                this._property = property;
            }
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                var sb = new StringBuilder();

                sb.Append(RuntimeType.Name)
                    .Append(" ")
                    .Append(_property.Name)
                    .Append("{ ");

                if (_property.CanRead)
                {
                    sb.Append("get; ");
                }

                if (_property.CanWrite)
                {
                    sb.Append("set; ");
                }

                return sb.Append('}').ToString();
            }
        }


        /// <inheritdoc/>
        public override bool CanWrite => _property.CanWrite;


        /// <inheritdoc/>
        public override bool CanRead => _property.CanRead;

        /// <inheritdoc/>
        public override bool IsStatic => _isStatic;

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            if (!_property.CanRead)
            {
                throw new AstException($"{_property.Name}不可读!");
            }

            if (_isStatic)
            {
                ilg.Emit(OpCodes.Call, _getter);
            }
            else
            {
                _instanceAst.Load(ilg);

                if (_getter.DeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Call, _getter);
                }
                else
                {
                    ilg.Emit(OpCodes.Callvirt, _getter);
                }
            }
        }

        /// <inheritdoc/>
        protected override void Assign(ILGenerator ilg, Expression value)
        {
            if (!_isStatic)
            {
                _instanceAst.Load(ilg);
            }

            value.Load(ilg);

            if (_isStatic || _setter.DeclaringType.IsValueType)
            {
                ilg.Emit(OpCodes.Call, _setter);
            }
            else
            {
                ilg.Emit(OpCodes.Callvirt, _setter);
            }
        }
    }
}
