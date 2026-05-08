using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 一元运算符。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class UnaryExpression : Expression
    {
        private readonly UnaryExpressionType _expressionType;
        private readonly Expression _body;

        private UnaryExpression(UnaryExpression unaryAst) : base(unaryAst.RuntimeType)
        {
            _expressionType = unaryAst._expressionType - 1;
            _body = unaryAst._body;
        }

        /// <summary>
        /// 一元运算。
        /// </summary>
        /// <param name="expressionType">一元运算类型。</param>
        /// <param name="body">表达式。</param>
        internal UnaryExpression(Expression body, UnaryExpressionType expressionType) : base(AnalysisType(expressionType, body))
        {
            _expressionType = expressionType;
            _body = body;
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                switch (_expressionType)
                {
                    case UnaryExpressionType.Increment:
                        return $"{_body} + 1";
                    case UnaryExpressionType.IncrementAssign:
                        return $"++{_body}";
                    case UnaryExpressionType.Decrement:
                        return $"{_body} - 1";
                    case UnaryExpressionType.DecrementAssign:
                        return $"--{_body}";
                    case UnaryExpressionType.UnaryPlus:
                        return $"+{_body}";
                    case UnaryExpressionType.Negate:
                        return $"-{_body}";
                    case UnaryExpressionType.Not:
                        return GetUnderlying(RuntimeType) == typeof(bool) ? $"!{_body}" : $"~{_body}";
                    case UnaryExpressionType.IsFalse:
                        return $"!{_body}";
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            // 赋值形式（IncrementAssign / DecrementAssign）：拆解为 Assign(body, 非赋值形式(body))。
            if (_expressionType < UnaryExpressionType.UnaryPlus && (_expressionType & UnaryExpressionType.Increment) == 0)
            {
                Assign(_body, new UnaryExpression(this))
                    .Load(ilg);

                return;
            }

            if (_body.RuntimeType.IsNullable())
            {
                EmitLiftedUnary(ilg);

                return;
            }

            _body.Load(ilg);

            EmitNonNullableOp(ilg, RuntimeType);
        }

        private void EmitNonNullableOp(ILGenerator ilg, Type type)
        {
            switch (_expressionType)
            {
                case UnaryExpressionType.UnaryPlus:
                    ilg.Emit(OpCodes.Nop);
                    break;
                case UnaryExpressionType.Negate:
                    ilg.Emit(OpCodes.Neg);
                    break;
                case UnaryExpressionType.Not:
                    if (type == typeof(bool))
                    {
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Ceq);
                    }
                    else
                    {
                        ilg.Emit(OpCodes.Not);
                    }
                    break;
                case UnaryExpressionType.Increment:
                    TryEmitConstantOne(ilg, type);
                    ilg.Emit(OpCodes.Add);
                    break;
                case UnaryExpressionType.Decrement:
                    TryEmitConstantOne(ilg, type);
                    ilg.Emit(OpCodes.Sub);
                    break;
                case UnaryExpressionType.IsFalse:
                    ilg.Emit(OpCodes.Ldc_I4_0);
                    ilg.Emit(OpCodes.Ceq);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        // 提升语义：null → null，否则在底层类型上执行运算并重新装入 Nullable&lt;T&gt;。
        private void EmitLiftedUnary(ILGenerator ilg)
        {
            var nullableType = _body.RuntimeType;
            var underlyingType = Nullable.GetUnderlyingType(nullableType);
            var hasValueGetter = nullableType.GetProperty(nameof(Nullable<int>.HasValue)).GetGetMethod();
            var getValueOrDefault = nullableType.GetMethod(nameof(Nullable<int>.GetValueOrDefault), Type.EmptyTypes);
            var nullableCtor = nullableType.GetConstructor(new[] { underlyingType });

            var locSrc = ilg.DeclareLocal(nullableType);
            var locResult = ilg.DeclareLocal(nullableType);

            _body.Load(ilg);
            ilg.Emit(OpCodes.Stloc, locSrc);

            ilg.Emit(OpCodes.Ldloca, locSrc);
            ilg.Emit(OpCodes.Call, hasValueGetter);

            var skipLabel = ilg.DefineLabel();
            ilg.Emit(OpCodes.Brfalse_S, skipLabel);

            ilg.Emit(OpCodes.Ldloca, locSrc);
            ilg.Emit(OpCodes.Call, getValueOrDefault);
            EmitNonNullableOp(ilg, underlyingType);
            ilg.Emit(OpCodes.Newobj, nullableCtor);
            ilg.Emit(OpCodes.Stloc, locResult);

            ilg.MarkLabel(skipLabel);
            ilg.Emit(OpCodes.Ldloc, locResult);
        }

        private static Type AnalysisType(UnaryExpressionType expressionType, Expression body)
        {
            Type bodyType = body.RuntimeType;
            bool isNullable = bodyType.IsNullable();
            Type checkType = isNullable ? Nullable.GetUnderlyingType(bodyType) : bodyType;

            switch (expressionType)
            {
                case UnaryExpressionType.UnaryPlus:
                case UnaryExpressionType.Increment:
                case UnaryExpressionType.Decrement:
                    if (IsArithmetic(checkType))
                    {
                        return bodyType;
                    }
                    break;
                case UnaryExpressionType.Negate:
                    if (IsArithmetic(checkType) && !IsUnsignedInt(checkType))
                    {
                        return bodyType;
                    }
                    break;
                case UnaryExpressionType.Not:
                    if (IsIntegerOrBool(checkType))
                    {
                        return bodyType;
                    }
                    break;
                case UnaryExpressionType.IsFalse:
                    // C# 原生语法不支持 bool? 的 IsFalse（false 运算符仅作用于 bool），仅允许 bool。
                    if (!isNullable && checkType == typeof(bool))
                    {
                        return bodyType;
                    }
                    break;
                case UnaryExpressionType.IncrementAssign:
                case UnaryExpressionType.DecrementAssign:
                    if (!body.CanWrite)
                    {
                        throw new AstException("表达式不可写!");
                    }
                    goto case UnaryExpressionType.Increment;
            }

            throw new InvalidOperationException($"\"{bodyType}\"不支持\"{expressionType}\"一元操作!");
        }

        private static Type GetUnderlying(Type type) => type.IsNullable() ? Nullable.GetUnderlyingType(type) : type;

        private static bool IsUnsignedInt(Type type)
        {
            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsIntegerOrBool(Type type)
        {
            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.Int64:
                case TypeCode.Int32:
                case TypeCode.Int16:
                case TypeCode.UInt64:
                case TypeCode.UInt32:
                case TypeCode.UInt16:
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsArithmetic(Type type)
        {
            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private static void TryEmitConstantOne(ILGenerator ilg, Type type)
        {
            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.Byte:
                    EmitUtils.EmitByte(ilg, 1);
                    break;
                case TypeCode.Int16:
                    EmitUtils.EmitInt16(ilg, 1);
                    break;
                case TypeCode.UInt16:
                    EmitUtils.EmitUInt16(ilg, 1);
                    break;
                case TypeCode.Int32:
                    EmitUtils.EmitInt(ilg, 1);
                    break;
                case TypeCode.UInt32:
                    EmitUtils.EmitUInt(ilg, 1);
                    break;
                case TypeCode.Int64:
                    EmitUtils.EmitLong(ilg, 1L);
                    break;
                case TypeCode.UInt64:
                    EmitUtils.EmitULong(ilg, 1uL);
                    break;
                case TypeCode.Single:
                    EmitUtils.EmitSingle(ilg, 1F);
                    break;
                case TypeCode.Double:
                    EmitUtils.EmitDouble(ilg, 1D);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

    }
}
