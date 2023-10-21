﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 数组。
    /// </summary>
    [DebuggerDisplay("{elementType.Name}[]")]
    public class ArrayExpression : Expression
    {
        private readonly Type elementType;
        private readonly Expression[] expressions;

        private static bool IsValid(Expression[] expressions, Type elementType)
        {
            return expressions.Length == 0 || elementType == typeof(object) || expressions.All(x => EmitUtils.IsAssignableFromSignatureTypes(elementType, x.RuntimeType));
        }

        /// <summary>
        /// 元素集合。
        /// </summary>
        /// <param name="expressions">元素。</param>

        internal ArrayExpression(Expression[] expressions) : this(expressions, typeof(object))
        {
        }

        /// <summary>
        /// 元素集合。
        /// </summary>
        /// <param name="expressions">元素。</param>
        /// <param name="elementType">数组类型。</param>

        internal ArrayExpression(Expression[] expressions, Type elementType) : base(elementType.MakeArrayType())
        {
            if (elementType is null)
            {
                throw new ArgumentNullException(nameof(elementType));
            }

            if (!IsValid(expressions ?? throw new ArgumentNullException(nameof(expressions)), elementType))
            {
                throw new AstException($"表达式元素不能转换为数组元素类型!");
            }

            this.expressions = expressions;
            this.elementType = elementType;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            bool isObjectElememt = elementType == typeof(object);

            EmitUtils.EmitInt(ilg, expressions.Length);

            ilg.Emit(OpCodes.Newarr, elementType);

            for (int i = 0; i < expressions.Length; i++)
            {
                var expressionAst = expressions[i];

                ilg.Emit(OpCodes.Dup);

                EmitUtils.EmitInt(ilg, i);

                expressionAst.Load(ilg);

                if (isObjectElememt)
                {
                    var type = expressionAst.RuntimeType;

                    if (type.IsByRef)
                    {
                        type = type.GetElementType();
                    }

                    if (type.IsGenericParameter)
                    {
                        ilg.Emit(OpCodes.Box, type);
                    }
                    else if (type.IsValueType)
                    {
                        if (type.IsEnum)
                        {
                            type = Enum.GetUnderlyingType(type);
                        }

                        ilg.Emit(OpCodes.Box, type);
                    }

                }

                ilg.Emit(OpCodes.Stelem_Ref);
            }
        }
    }
}
