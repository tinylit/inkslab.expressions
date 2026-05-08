using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 数组。
    /// </summary>
    [DebuggerDisplay("{elementType.Name}[]")]
    public class ArrayExpression : Expression
    {
        private readonly Type _elementType;
        private readonly Expression[] _expressions;

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
            _expressions = expressions;
            _elementType = elementType;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            bool isObjectElememt = _elementType == typeof(object);

            EmitUtils.EmitInt(ilg, _expressions.Length);

            ilg.Emit(OpCodes.Newarr, _elementType);

            for (int i = 0; i < _expressions.Length; i++)
            {
                var expressionAst = _expressions[i];

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
