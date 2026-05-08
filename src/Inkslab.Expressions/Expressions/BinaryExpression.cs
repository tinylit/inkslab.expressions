using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 二进制。
    /// </summary>
    [DebuggerDisplay("{left} {expressionType} {right}")]
    public class BinaryExpression : Expression
    {
        private static readonly MethodInfo _referenceEqualsMethod =
            new Func<object, object, bool>(object.ReferenceEquals).Method;

        private readonly Expression _left;
        private readonly BinaryExpressionType _expressionType;
        private readonly Expression _right;
        private readonly MethodInfo _operatorMethod;
        private readonly NullableComparisonKind _nullableComparisonKind;
        private readonly Type _nullableUnderlyingType;

        /// <summary>
        /// 可空类型比较的种类
        /// </summary>
        private enum NullableComparisonKind : byte
        {
            None = 0,              // 不是可空类型比较
            BothNullable = 1,      // T? == T?
            LeftNullable = 2,      // T? == T
            RightNullable = 3      // T == T?
        }

        private BinaryExpression(BinaryExpression binaryAst) : base(binaryAst.RuntimeType)
        {
            _left = binaryAst._left;
            _expressionType = binaryAst._expressionType - 1;
            _right = binaryAst._right;
            _operatorMethod = binaryAst._operatorMethod;
            _nullableComparisonKind = binaryAst._nullableComparisonKind;
            _nullableUnderlyingType = binaryAst._nullableUnderlyingType;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="expressionType">计算方式。</param>
        /// <param name="right">右表达式。</param>
        internal BinaryExpression(Expression left, BinaryExpressionType expressionType, Expression right) : base(AnalysisType(left, expressionType, right, out MethodInfo operatorMethod, out NullableComparisonKind nullableComparisonKind, out Type nullableUnderlyingType))
        {
            _left = left;
            _expressionType = expressionType;
            _right = right;
            _operatorMethod = operatorMethod;
            _nullableComparisonKind = nullableComparisonKind;
            _nullableUnderlyingType = nullableUnderlyingType;
        }

        private static Type AnalysisType(Expression left, BinaryExpressionType expressionType, Expression right, out MethodInfo operatorMethod, out NullableComparisonKind nullableComparisonKind, out Type nullableUnderlyingType)
        {
            operatorMethod = null;
            nullableComparisonKind = NullableComparisonKind.None;
            nullableUnderlyingType = null;

            switch (expressionType)
            {
                case BinaryExpressionType.Add:
                case BinaryExpressionType.Subtract:
                case BinaryExpressionType.Multiply:
                case BinaryExpressionType.Divide:
                case BinaryExpressionType.Modulo:
                    if (left.RuntimeType == right.RuntimeType && IsArithmetic(left.RuntimeType))
                    {
                        return left.RuntimeType;
                    }
                    return AnalysisTypeByCustom(left, expressionType, right, ref operatorMethod, ref nullableUnderlyingType, ref nullableComparisonKind);
                case BinaryExpressionType.LessThan:
                case BinaryExpressionType.LessThanOrEqual:
                case BinaryExpressionType.Equal:
                case BinaryExpressionType.GreaterThanOrEqual:
                case BinaryExpressionType.GreaterThan:
                case BinaryExpressionType.NotEqual:
                    if (left.RuntimeType == right.RuntimeType && IsArithmetic(left.RuntimeType))
                    {
                        return typeof(bool);
                    }
                    if (IsEnumIntegerCompatible(left.RuntimeType, right.RuntimeType))
                    {
                        return typeof(bool);
                    }
                    return AnalysisTypeByCustom(left, expressionType, right, ref operatorMethod, ref nullableUnderlyingType, ref nullableComparisonKind);
                case BinaryExpressionType.OrElse:
                case BinaryExpressionType.AndAlso:
                    if (left.RuntimeType == right.RuntimeType && left.RuntimeType == typeof(bool))
                    {
                        return left.RuntimeType;
                    }
                    throw new AstException($"{left.RuntimeType}类型不支持“{expressionType}”运算!");
                case BinaryExpressionType.Or:
                case BinaryExpressionType.And:
                case BinaryExpressionType.ExclusiveOr:
                    if (left.RuntimeType == right.RuntimeType && IsIntegerOrBool(left.RuntimeType))
                    {
                        return left.RuntimeType;
                    }
                    return AnalysisTypeByCustom(left, expressionType, right, ref operatorMethod, ref nullableUnderlyingType, ref nullableComparisonKind);
                case BinaryExpressionType.LeftShift:
                case BinaryExpressionType.RightShift:
                    if (IsInteger(left.RuntimeType))
                    {
                        if (right.RuntimeType == typeof(int))
                        {
                            return left.RuntimeType;
                        }

                        throw new AstException($"“{expressionType}”运算，右侧表达式类型必须是“int”!");
                    }
                    throw new AstException($"{left.RuntimeType}类型不支持“{expressionType}”运算!");
                case BinaryExpressionType.Power:
                    if (left.RuntimeType == right.RuntimeType && left.RuntimeType == typeof(double))
                    {
                        return left.RuntimeType;
                    }

#if NETSTANDARD2_1_OR_GREATER
                    if (left.RuntimeType == right.RuntimeType && left.RuntimeType == typeof(float))
                    {
                        return left.RuntimeType;
                    }
#endif
                    throw new AstException($"{left.RuntimeType}类型不支持“{expressionType}”运算!");
                case BinaryExpressionType.OrAssign:
                case BinaryExpressionType.AndAssign:
                case BinaryExpressionType.ExclusiveOrAssign:
                    if (!left.CanWrite)
                    {
                        throw new AstException("左表达式不可写!");
                    }
                    goto case BinaryExpressionType.Or;
                case BinaryExpressionType.LeftShiftAssign:
                case BinaryExpressionType.RightShiftAssign:
                    if (!left.CanWrite)
                    {
                        throw new AstException("左表达式不可写!");
                    }
                    goto case BinaryExpressionType.LeftShift;
                case BinaryExpressionType.PowerAssign:
                    if (!left.CanWrite)
                    {
                        throw new AstException("左表达式不可写!");
                    }
                    goto case BinaryExpressionType.Power;
                case BinaryExpressionType.AddAssign:
                case BinaryExpressionType.SubtractAssign:
                case BinaryExpressionType.MultiplyAssign:
                case BinaryExpressionType.DivideAssign:
                case BinaryExpressionType.ModuloAssign:
                    if (!left.CanWrite)
                    {
                        throw new AstException("左表达式不可写!");
                    }
                    goto case BinaryExpressionType.Add;
                default:
                    throw new NotImplementedException();
            }
        }

        private static Type AnalysisTypeByCustom(Expression left, BinaryExpressionType expressionType, Expression right, ref MethodInfo operatorMethod, ref Type nullableUnderlyingType, ref NullableComparisonKind nullableComparisonKind)
        {
            var leftType = left.RuntimeType;
            var rightType = right.RuntimeType;

            // 对于相等性比较，优先尝试可空类型的特殊处理
            if (IsEqualityComparison(expressionType))
            {
                if (TryHandleNullableComparison(leftType, rightType, out var resultType, out nullableComparisonKind, out nullableUnderlyingType))
                {
                    return resultType;
                }
            }

            // 尝试查找操作符重载
            string operatorName = GetOperatorName(expressionType, leftType, rightType);
            operatorMethod = FindOperatorMethod(leftType, rightType, operatorName);

            if (operatorMethod is null)
            {
                // 对于相等性比较，尝试 IEquatable<T> 和 Equals 回退
                if (IsEqualityComparison(expressionType))
                {
                    operatorMethod = FindEqualityMethod(leftType, rightType);
                    if (operatorMethod is not null)
                    {
                        return typeof(bool);
                    }
                }

                throw new AstException($"不支持{leftType}与{rightType}的{expressionType}运算！");
            }

            ValidateOperatorReturnType(expressionType, leftType, operatorMethod);
            return operatorMethod.ReturnType;
        }

        private static bool IsEqualityComparison(BinaryExpressionType expressionType)
        {
            return expressionType is BinaryExpressionType.Equal or BinaryExpressionType.NotEqual;
        }

        /// <summary>
        /// 检查左右类型是否为 <c>enum</c> 与其底层整数类型的对比组合（顺序无关）。
        /// 例如 <c>MyEnum</c>（底层为 <see cref="int"/>）与 <see cref="int"/> 字面量比较。
        /// 二者在 IL 栈上表示一致，可直接进行比较指令而无需额外转换。
        /// </summary>
        private static bool IsEnumIntegerCompatible(Type leftType, Type rightType)
        {
            if (leftType.IsEnum && Enum.GetUnderlyingType(leftType) == rightType)
            {
                return true;
            }
            if (rightType.IsEnum && Enum.GetUnderlyingType(rightType) == leftType)
            {
                return true;
            }
            return false;
        }

        private static string GetOperatorName(BinaryExpressionType expressionType, Type leftType, Type rightType)
        {
            switch (expressionType)
            {
                case BinaryExpressionType.Add:
                case BinaryExpressionType.AddChecked:
                case BinaryExpressionType.AddAssign:
                case BinaryExpressionType.AddAssignChecked:
                    return "op_Addition";
                case BinaryExpressionType.Subtract:
                case BinaryExpressionType.SubtractAssign:
                case BinaryExpressionType.SubtractChecked:
                case BinaryExpressionType.SubtractAssignChecked:
                    return "op_Subtraction";
                case BinaryExpressionType.Multiply:
                case BinaryExpressionType.MultiplyAssign:
                case BinaryExpressionType.MultiplyChecked:
                case BinaryExpressionType.MultiplyAssignChecked:
                    return "op_Multiply";
                case BinaryExpressionType.Divide:
                case BinaryExpressionType.DivideAssign:
                    return "op_Division";
                case BinaryExpressionType.Modulo:
                case BinaryExpressionType.ModuloAssign:
                    return "op_Modulus";
                case BinaryExpressionType.And:
                case BinaryExpressionType.AndAssign:
                    return "op_BitwiseAnd";
                case BinaryExpressionType.Or:
                case BinaryExpressionType.OrAssign:
                    return "op_BitwiseOr";
                case BinaryExpressionType.ExclusiveOr:
                case BinaryExpressionType.ExclusiveOrAssign:
                    return "op_ExclusiveOr";
                case BinaryExpressionType.LeftShift:
                case BinaryExpressionType.LeftShiftAssign:
                    return "op_LeftShift";
                case BinaryExpressionType.RightShift:
                case BinaryExpressionType.RightShiftAssign:
                    return "op_RightShift";
                case BinaryExpressionType.Equal:
                    return "op_Equality";
                case BinaryExpressionType.GreaterThan:
                    return "op_GreaterThan";
                case BinaryExpressionType.GreaterThanOrEqual:
                    return "op_GreaterThanOrEqual";
                case BinaryExpressionType.LessThan:
                    return "op_LessThan";
                case BinaryExpressionType.LessThanOrEqual:
                    return "op_LessThanOrEqual";
                case BinaryExpressionType.NotEqual:
                    return "op_Inequality";
                default:
                    throw new AstException($"不支持{leftType}与{rightType}的{expressionType}运算！");
            }
        }

        private static MethodInfo FindOperatorMethod(Type leftType, Type rightType, string operatorName)
        {
            var types = new Type[] { leftType, rightType };
            var staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            return leftType.GetMethod(operatorName, staticFlags, null, types, null)
                ?? rightType.GetMethod(operatorName, staticFlags, null, types, null);
        }

        private static void ValidateOperatorReturnType(BinaryExpressionType expressionType, Type leftType, MethodInfo operatorMethod)
        {
            if (expressionType < BinaryExpressionType.OrElse
                && (expressionType & BinaryExpressionType.Add) == 0
                && leftType != operatorMethod.ReturnType)
            {
                throw new AstException($"操作符返回类型不匹配：期望 {leftType}，实际 {operatorMethod.ReturnType}");
            }
        }

        /// <summary>
        /// 尝试处理可空类型的比较操作
        /// </summary>
        private static bool TryHandleNullableComparison(Type leftType, Type rightType, out Type resultType, out NullableComparisonKind nullableComparisonKind, out Type nullableUnderlyingType)
        {
            var leftUnderlyingType = Nullable.GetUnderlyingType(leftType);
            var rightUnderlyingType = Nullable.GetUnderlyingType(rightType);

            // 场景 1: 两个操作数都是可空类型 (T? == T?)
            if (leftUnderlyingType is not null && rightUnderlyingType is not null)
            {
                if (leftUnderlyingType == rightUnderlyingType && IsArithmeticUnderlying(leftUnderlyingType))
                {
                    resultType = typeof(bool);
                    nullableComparisonKind = NullableComparisonKind.BothNullable;
                    nullableUnderlyingType = leftUnderlyingType;
                    return true;
                }
            }
            // 场景 2: 左操作数可空，右操作数不可空 (T? == T)
            else if (leftUnderlyingType is not null && rightUnderlyingType is null)
            {
                if (leftUnderlyingType == rightType && IsArithmeticUnderlying(leftUnderlyingType))
                {
                    resultType = typeof(bool);
                    nullableComparisonKind = NullableComparisonKind.LeftNullable;
                    nullableUnderlyingType = leftUnderlyingType;
                    return true;
                }
            }
            // 场景 3: 左操作数不可空，右操作数可空 (T == T?)
            else if (leftUnderlyingType is null && rightUnderlyingType is not null)
            {
                if (leftType == rightUnderlyingType && IsArithmeticUnderlying(rightUnderlyingType))
                {
                    resultType = typeof(bool);
                    nullableComparisonKind = NullableComparisonKind.RightNullable;
                    nullableUnderlyingType = rightUnderlyingType;
                    return true;
                }
            }

            resultType = null;
            nullableComparisonKind = NullableComparisonKind.None;
            nullableUnderlyingType = null;
            return false;
        }

        /// <summary>
        /// 为 Equal/NotEqual 查找相等性比较方法，按优先级依次尝试：
        /// 1. <see cref="IEquatable{T}"/> 接口的 Equals(T) 方法
        /// 2. 重写的 Equals(object) 方法
        /// 3. 两侧均为引用类型且具有继承关系时回退到 <see cref="object.ReferenceEquals(object, object)"/>
        /// 若均未找到返回 null。
        /// </summary>
        private static MethodInfo FindEqualityMethod(Type leftType, Type rightType)
        {
            // 1. 检查 leftType 是否实现了 IEquatable<rightType>
            var equatableType = typeof(IEquatable<>).MakeGenericType(rightType);

            if (equatableType.IsAssignableFrom(leftType))
            {
                return leftType.GetMethod("Equals",
                    BindingFlags.Public | BindingFlags.Instance,
                    null, new[] { rightType }, null);
            }

            // 2. 检查 leftType 是否重写了 Equals(object)（排除基类 object.Equals）
            var equalsMethod = leftType.GetMethod("Equals",
                BindingFlags.Public | BindingFlags.Instance,
                null, new[] { typeof(object) }, null);

            if (equalsMethod is not null && equalsMethod.ReturnType == typeof(bool)
                && equalsMethod.DeclaringType != typeof(object))
            {
                return equalsMethod;
            }

            // 3. 两侧均为引用类型且具有继承/实现关系时，按 C# 默认语义采用引用相等。
            if (!leftType.IsValueType && !rightType.IsValueType
                && (leftType.IsAssignableFrom(rightType) || rightType.IsAssignableFrom(leftType)))
            {
                return _referenceEqualsMethod;
            }

            return null;
        }

        /// <summary>
        /// 生成两个可空类型比较的 IL 代码 (T? == T?)
        /// </summary>
        private static void EmitNullableComparison(ILGenerator ilg, Expression left, Expression right, Type underlyingType, BinaryExpressionType expressionType)
        {
            var nullableType = typeof(Nullable<>).MakeGenericType(underlyingType);
            var hasValueGetter = nullableType.GetProperty("HasValue").GetMethod;
            var getValueOrDefaultMethod = nullableType.GetMethod("GetValueOrDefault", Type.EmptyTypes);

            var leftLocal = ilg.DeclareLocal(nullableType);
            var rightLocal = ilg.DeclareLocal(nullableType);

            // 存储左右操作数到局部变量
            left.Load(ilg);
            ilg.Emit(OpCodes.Stloc, leftLocal);
            right.Load(ilg);
            ilg.Emit(OpCodes.Stloc, rightLocal);

            // 比较逻辑：(left.GetValueOrDefault() == right.GetValueOrDefault()) && (left.HasValue == right.HasValue)

            // 比较值：left.GetValueOrDefault() == right.GetValueOrDefault()
            ilg.Emit(OpCodes.Ldloca, leftLocal);
            ilg.Emit(OpCodes.Call, getValueOrDefaultMethod);
            ilg.Emit(OpCodes.Ldloca, rightLocal);
            ilg.Emit(OpCodes.Call, getValueOrDefaultMethod);
            ilg.Emit(OpCodes.Ceq);

            // 比较 HasValue：left.HasValue == right.HasValue
            ilg.Emit(OpCodes.Ldloca, leftLocal);
            ilg.Emit(OpCodes.Call, hasValueGetter);
            ilg.Emit(OpCodes.Ldloca, rightLocal);
            ilg.Emit(OpCodes.Call, hasValueGetter);
            ilg.Emit(OpCodes.Ceq);

            // 合并结果：值相等 && HasValue 相等
            ilg.Emit(OpCodes.And);

            // NotEqual 需要取反
            if (expressionType == BinaryExpressionType.NotEqual)
            {
                EmitLogicalNot(ilg);
            }
        }

        /// <summary>
        /// 生成可空类型与非可空类型比较的 IL 代码 (T? == T 或 T == T?)
        /// </summary>
        private static void EmitNullableToNonNullableComparison(ILGenerator ilg, Expression nullableExpr, Expression nonNullableExpr, Type underlyingType, BinaryExpressionType expressionType)
        {
            var nullableType = typeof(Nullable<>).MakeGenericType(underlyingType);
            var hasValueGetter = nullableType.GetProperty("HasValue").GetMethod;
            var getValueOrDefaultMethod = nullableType.GetMethod("GetValueOrDefault", Type.EmptyTypes);

            var nullableLocal = ilg.DeclareLocal(nullableType);

            // 存储可空操作数到局部变量
            nullableExpr.Load(ilg);
            ilg.Emit(OpCodes.Stloc, nullableLocal);

            // 比较逻辑：nullable.HasValue && (nullable.GetValueOrDefault() == nonNullable)

            // 比较值：nullable.GetValueOrDefault() == nonNullable
            ilg.Emit(OpCodes.Ldloca, nullableLocal);
            ilg.Emit(OpCodes.Call, getValueOrDefaultMethod);
            nonNullableExpr.Load(ilg);
            ilg.Emit(OpCodes.Ceq);

            // 检查 HasValue
            ilg.Emit(OpCodes.Ldloca, nullableLocal);
            ilg.Emit(OpCodes.Call, hasValueGetter);

            // 合并结果：值相等 && HasValue
            ilg.Emit(OpCodes.And);

            // NotEqual 需要取反
            if (expressionType == BinaryExpressionType.NotEqual)
            {
                EmitLogicalNot(ilg);
            }
        }

        /// <summary>
        /// 生成逻辑取反的 IL 代码
        /// </summary>
        private static void EmitLogicalNot(ILGenerator ilg)
        {
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ceq);
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">命令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (_expressionType < BinaryExpressionType.OrElse && (_expressionType & BinaryExpressionType.Add) == 0)
            {
                Assign(_left, new BinaryExpression(this))
                    .Load(ilg);
            }
            else
            {
                // 尝试生成可空类型比较的特殊 IL
                if (TryEmitNullableComparison(ilg))
                {
                    return;
                }

                // 加载左右操作数
                LoadOperand(ilg, _left);
                LoadOperand(ilg, _right);

                // 如果有自定义操作符方法，调用它
                if (_operatorMethod is not null)
                {
                    EmitOperatorMethodCall(ilg);
                    return;
                }

                // 生成内置操作的 IL 指令
                EmitBuiltInOperation(ilg);
            }
        }

        /// <summary>
        /// 尝试为可空类型比较生成特殊的 IL 代码
        /// </summary>
        private bool TryEmitNullableComparison(ILGenerator ilg)
        {
            switch (_nullableComparisonKind)
            {
                case NullableComparisonKind.BothNullable:
                    EmitNullableComparison(ilg, _left, _right, _nullableUnderlyingType, _expressionType);
                    return true;

                case NullableComparisonKind.LeftNullable:
                    EmitNullableToNonNullableComparison(ilg, _left, _right, _nullableUnderlyingType, _expressionType);
                    return true;

                case NullableComparisonKind.RightNullable:
                    EmitNullableToNonNullableComparison(ilg, _right, _left, _nullableUnderlyingType, _expressionType);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 加载操作数到栈
        /// </summary>
        private static void LoadOperand(ILGenerator ilg, Expression operand)
        {
            if (operand is ConstantExpression constantExpr && constantExpr.IsNull)
            {
                ilg.Emit(OpCodes.Ldnull);
            }
            else
            {
                operand.Load(ilg);
            }
        }

        /// <summary>
        /// 调用自定义操作符方法
        /// </summary>
        private void EmitOperatorMethodCall(ILGenerator ilg)
        {
            ilg.Emit(_operatorMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, _operatorMethod);

            // 对于 NotEqual，如果调用的是 Equals 或 ReferenceEquals，需要取反结果
            if (_expressionType == BinaryExpressionType.NotEqual
                && (_operatorMethod.Name == "Equals" || _operatorMethod.Name == "ReferenceEquals"))
            {
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Ceq);
            }
        }

        /// <summary>
        /// 生成内置操作的 IL 指令
        /// </summary>
        private void EmitBuiltInOperation(ILGenerator ilg)
        {
            switch (_expressionType)
            {
                case BinaryExpressionType.Add:
                    ilg.Emit(OpCodes.Add);
                    break;
                case BinaryExpressionType.AddChecked:
                    EmitCheckedAdd(ilg);
                    break;
                case BinaryExpressionType.Subtract:
                    ilg.Emit(OpCodes.Sub);
                    break;
                case BinaryExpressionType.SubtractChecked:
                    EmitCheckedSubtract(ilg);
                    break;
                case BinaryExpressionType.Multiply:
                    ilg.Emit(OpCodes.Mul);
                    break;
                case BinaryExpressionType.MultiplyChecked:
                    EmitCheckedMultiply(ilg);
                    break;
                case BinaryExpressionType.Divide:
                    ilg.Emit(IsUnsigned(_left.RuntimeType) ? OpCodes.Div_Un : OpCodes.Div);
                    break;
                case BinaryExpressionType.Modulo:
                    ilg.Emit(IsUnsigned(_left.RuntimeType) ? OpCodes.Rem_Un : OpCodes.Rem);
                    break;
                case BinaryExpressionType.And:
                case BinaryExpressionType.AndAlso:
                    ilg.Emit(OpCodes.And);
                    break;
                case BinaryExpressionType.Or:
                case BinaryExpressionType.OrElse:
                    ilg.Emit(OpCodes.Or);
                    break;
                case BinaryExpressionType.ExclusiveOr:
                    ilg.Emit(OpCodes.Xor);
                    break;
                case BinaryExpressionType.LeftShift:
                    ilg.Emit(OpCodes.Shl);
                    break;
                case BinaryExpressionType.RightShift:
                    ilg.Emit(IsUnsigned(_left.RuntimeType) ? OpCodes.Shr_Un : OpCodes.Shr);
                    break;
                case BinaryExpressionType.Power:
                    EmitPower(ilg);
                    break;
                case BinaryExpressionType.LessThan:
                    ilg.Emit(IsUnsigned(_left.RuntimeType) ? OpCodes.Clt_Un : OpCodes.Clt);
                    break;
                case BinaryExpressionType.Equal:
                    ilg.Emit(OpCodes.Ceq);
                    break;
                case BinaryExpressionType.GreaterThan:
                    ilg.Emit(IsUnsigned(_left.RuntimeType) ? OpCodes.Cgt_Un : OpCodes.Cgt);
                    break;
                case BinaryExpressionType.GreaterThanOrEqual:
                case BinaryExpressionType.LessThanOrEqual:
                case BinaryExpressionType.NotEqual:
                    EmitInverseComparison(ilg);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        private void EmitCheckedAdd(ILGenerator ilg)
        {
            if (IsFloatingPoint(_left.RuntimeType))
            {
                ilg.Emit(OpCodes.Add);
            }
            else if (IsUnsigned(_left.RuntimeType))
            {
                ilg.Emit(OpCodes.Add_Ovf_Un);
            }
            else
            {
                ilg.Emit(OpCodes.Add_Ovf);
            }
        }

        private void EmitCheckedSubtract(ILGenerator ilg)
        {
            if (IsFloatingPoint(_left.RuntimeType))
            {
                ilg.Emit(OpCodes.Sub);
            }
            else if (IsUnsigned(_left.RuntimeType))
            {
                ilg.Emit(OpCodes.Sub_Ovf_Un);
            }
            else
            {
                ilg.Emit(OpCodes.Sub_Ovf);
            }
        }

        private void EmitCheckedMultiply(ILGenerator ilg)
        {
            if (IsFloatingPoint(_left.RuntimeType))
            {
                ilg.Emit(OpCodes.Mul);
            }
            else if (IsUnsigned(_left.RuntimeType))
            {
                ilg.Emit(OpCodes.Mul_Ovf_Un);
            }
            else
            {
                ilg.Emit(OpCodes.Mul_Ovf);
            }
        }

        private void EmitPower(ILGenerator ilg)
        {
#if NETSTANDARD2_1_OR_GREATER
            if (_left.RuntimeType == typeof(double))
            {
                ilg.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Pow), BindingFlags.Static | BindingFlags.Public));
            }
            else
            {
                ilg.Emit(OpCodes.Call, typeof(MathF).GetMethod(nameof(MathF.Pow), BindingFlags.Static | BindingFlags.Public));
            }
#else
            ilg.Emit(OpCodes.Call, typeof(Math).GetMethod(nameof(Math.Pow), BindingFlags.Static | BindingFlags.Public));
#endif
        }

        private void EmitInverseComparison(ILGenerator ilg)
        {
            if (_expressionType == BinaryExpressionType.GreaterThanOrEqual)
            {
                ilg.Emit(IsUnsigned(_left.RuntimeType) ? OpCodes.Clt_Un : OpCodes.Clt);
            }
            else if (_expressionType == BinaryExpressionType.LessThanOrEqual)
            {
                ilg.Emit(IsUnsigned(_left.RuntimeType) ? OpCodes.Cgt_Un : OpCodes.Cgt);
            }
            else
            {
                ilg.Emit(OpCodes.Ceq);
            }
            ilg.Emit(OpCodes.Ldc_I4_0);
            ilg.Emit(OpCodes.Ceq);
        }

        private static bool IsInteger(Type type)
        {
            if (type.IsNullable())
            {
                return false;
            }

            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
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
            if (type.IsNullable())
            {
                return false;
            }

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

        private static bool IsFloatingPoint(Type type)
        {
            if (type.IsNullable())
            {
                return false;
            }

            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsUnsigned(Type type)
        {
            if (type.IsNullable())
            {
                return false;
            }

            switch (Type.GetTypeCode(type.IsEnum ? Enum.GetUnderlyingType(type) : type))
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.Char:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsArithmetic(Type type)
        {
            if (type.IsNullable())
            {
                return false;
            }

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

        private static bool IsArithmeticUnderlying(Type type)
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
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return true;
                default:
                    return false;
            }
        }
    }
}
