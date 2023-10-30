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
        private readonly bool isStatic;
        private readonly Expression instanceAst;
        private readonly PropertyInfo property;
        private readonly MethodInfo getter;
        private readonly MethodInfo setter;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="property">属性。</param>
        internal PropertyExpression(PropertyInfo property) : base(property.PropertyType)
        {
            getter = property.GetMethod ?? property.GetGetMethod(true);
            setter = property.SetMethod ?? property.GetSetMethod(true);

            MethodInfo methodInfo = getter ?? setter;

            if (isStatic = methodInfo.IsStatic)
            {
                this.property = property;
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
            getter = property.GetMethod ?? property.GetGetMethod(true);
            setter = property.SetMethod ?? property.GetSetMethod(true);

            MethodInfo methodInfo = getter ?? setter;

            if (isStatic = methodInfo.IsStatic)
            {
                if (instanceAst is null)
                {
                    this.property = property;
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
                this.instanceAst = instanceAst;
                this.property = property;
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


        /// <inheritdoc/>
        public override bool CanWrite => property.CanWrite;


        /// <inheritdoc/>
        public override bool CanRead => property.CanRead;

        /// <inheritdoc/>
        public override bool IsStatic => isStatic;

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            if (!property.CanRead)
            {
                throw new AstException($"{property.Name}不可读!");
            }

            if (isStatic)
            {
                ilg.Emit(OpCodes.Call, getter);
            }
            else
            {
                instanceAst.Load(ilg);

                if (getter.DeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Call, getter);
                }
                else
                {
                    ilg.Emit(OpCodes.Callvirt, getter);
                }
            }
        }

        /// <inheritdoc/>
        protected override void Assign(ILGenerator ilg, Expression value)
        {
            if (!isStatic)
            {
                instanceAst.Load(ilg);
            }

            value.Load(ilg);

            if (isStatic || setter.DeclaringType.IsValueType)
            {
                ilg.Emit(OpCodes.Call, setter);
            }
            else
            {
                ilg.Emit(OpCodes.Callvirt, setter);
            }
        }
    }
}
