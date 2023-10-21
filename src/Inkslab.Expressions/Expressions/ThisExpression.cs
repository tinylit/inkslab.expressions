using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 当前上下文。
    /// </summary>
    [DebuggerDisplay("this")]
    public class ThisExpression : Expression
    {
        private readonly Type instanceType;

        internal ThisExpression(Type instanceType) : base(instanceType)
        {
            if (instanceType.IsAbstract)
            {
                throw new AstException($"抽象类型({instanceType.Name})不支持“this”关键字！");
            }

            this.instanceType = instanceType;
        }

        [DebuggerDisplay("base")]
        private class BaseExpression : ThisExpression
        {
            public BaseExpression(Type instanceType) : base(instanceType)
            {
            }
        }

        /// <summary>
        /// 父级上下文。
        /// </summary>
        public Expression Base
        {
            get
            {
                if (instanceType.IsValueType)
                {
                    throw new AstException($"值类型({instanceType})不支持“base”关键字！");
                }

                return new BaseExpression(instanceType.BaseType ?? typeof(object));
            }
        }

        /// <inheritdoc/>
        public override void Load(ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldarg_0);
        }
    }
}
