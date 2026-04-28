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
        private readonly Expression _left;
        private readonly BinaryExpressionType _expressionType;
        private readonly Expression _right;
        private readonly MethodInfo _operatorMethod;
        private BinaryExpression(BinaryExpression binaryAst) : base(binaryAst.RuntimeType)
        {
            _left = binaryAst._left;
            _expressionType = binaryAst._expressionType - 1;
            _right = binaryAst._right;
            _operatorMethod = binaryAst._operatorMethod;
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="expressionType">计算方式。</param>
        /// <param name="right">右表达式。</param>
        internal BinaryExpression(Expression left, BinaryExpressionType expressionType, Expression right) : base(AnalysisType(left, expressionType, right, out MethodInfo operatorMethod))
        {
            _left = left;
            _expressionType = expressionType;
            _right = right;
            _operatorMethod = operatorMethod;
        }

        private static Type AnalysisType(Expression left, BinaryExpressionType expressionType, Expression right, out MethodInfo operatorMethod)
        {
            operatorMethod = null;

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
                    return AnalysisTypeByCustom(left, expressionType, right, ref operatorMethod);
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
                    return AnalysisTypeByCustom(left, expressionType, right, ref operatorMethod);
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
                    return AnalysisTypeByCustom(left, expressionType, right, ref operatorMethod);
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

        private static Type AnalysisTypeByCustom(Expression left, BinaryExpressionType expressionType, Expression right, ref MethodInfo operatorMethod)
        {
            string operatorName;

            switch (expressionType)
            {
                case BinaryExpressionType.Add:
                case BinaryExpressionType.AddChecked:
                case BinaryExpressionType.AddAssign:
                case BinaryExpressionType.AddAssignChecked:
                    operatorName = "op_Addition";
                    break;
                case BinaryExpressionType.Subtract:
                case BinaryExpressionType.SubtractAssign:
                case BinaryExpressionType.SubtractChecked:
                case BinaryExpressionType.SubtractAssignChecked:
                    operatorName = "op_Subtraction";
                    break;
                case BinaryExpressionType.Multiply:
                case BinaryExpressionType.MultiplyAssign:
                case BinaryExpressionType.MultiplyChecked:
                case BinaryExpressionType.MultiplyAssignChecked:
                    operatorName = "op_Multiply";
                    break;
                case BinaryExpressionType.Divide:
                case BinaryExpressionType.DivideAssign:
                    operatorName = "op_Division";
                    break;
                case BinaryExpressionType.Modulo:
                case BinaryExpressionType.ModuloAssign:
                    operatorName = "op_Modulus";
                    break;
                case BinaryExpressionType.And:
                case BinaryExpressionType.AndAssign:
                    operatorName = "op_BitwiseAnd";
                    break;
                case BinaryExpressionType.Or:
                case BinaryExpressionType.OrAssign:
                    operatorName = "op_BitwiseOr";
                    break;
                case BinaryExpressionType.ExclusiveOr:
                case BinaryExpressionType.ExclusiveOrAssign:
                    operatorName = "op_ExclusiveOr";
                    break;
                case BinaryExpressionType.LeftShift:
                case BinaryExpressionType.LeftShiftAssign:
                    operatorName = "op_LeftShift";
                    break;
                case BinaryExpressionType.RightShift:
                case BinaryExpressionType.RightShiftAssign:
                    operatorName = "op_RightShift";
                    break;
                case BinaryExpressionType.Equal:
                    operatorName = "op_Equality";
                    break;
                case BinaryExpressionType.GreaterThan:
                    operatorName = "op_GreaterThan";
                    break;
                case BinaryExpressionType.GreaterThanOrEqual:
                    operatorName = "op_GreaterThanOrEqual";
                    break;
                case BinaryExpressionType.LessThan:
                    operatorName = "op_LessThan";
                    break;
                case BinaryExpressionType.LessThanOrEqual:
                    operatorName = "op_LessThanOrEqual";
                    break;
                case BinaryExpressionType.NotEqual:
                    operatorName = "op_Inequality";
                    break;
                case BinaryExpressionType.OrElse:
                case BinaryExpressionType.AndAlso:
                case BinaryExpressionType.Power:
                case BinaryExpressionType.PowerAssign:
                default:
                    throw new AstException($"不支持{left.RuntimeType}与{right.RuntimeType}的{expressionType}运算！");
            }

            var leftType = left.RuntimeType;
            var rightType = right.RuntimeType;

            var types = new Type[] { leftType, rightType };

            BindingFlags staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            operatorMethod = leftType.GetMethod(operatorName, staticFlags, null, types, null) ?? rightType.GetMethod(operatorName, staticFlags, null, types, null);

            if (operatorMethod is null || expressionType < BinaryExpressionType.OrElse && (expressionType & BinaryExpressionType.Add) == 0 && leftType != operatorMethod.ReturnType)
            {
                throw new AstException($"不支持{left.RuntimeType}与{right.RuntimeType}的{expressionType}运算！");
            }

            return operatorMethod.ReturnType;
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
                if (_left is ConstantExpression constantAst && constantAst.IsNull)
                {
                    ilg.Emit(OpCodes.Ldnull);
                }
                else
                {
                    _left.Load(ilg);
                }

                if (_right is ConstantExpression constantAst2 && constantAst2.IsNull)
                {
                    ilg.Emit(OpCodes.Ldnull);
                }
                else
                {
                    _right.Load(ilg);
                }

                if (_operatorMethod is not null)
                {
                    ilg.Emit(OpCodes.Call, _operatorMethod);

                    return;
                }

                switch (_expressionType)
                {
                    case BinaryExpressionType.Add:
                        ilg.Emit(OpCodes.Add);
                        break;
                    case BinaryExpressionType.AddChecked:
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
                        break;
                    case BinaryExpressionType.Subtract:
                        ilg.Emit(OpCodes.Sub);
                        break;
                    case BinaryExpressionType.SubtractChecked:
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
                        break;
                    case BinaryExpressionType.Multiply:
                        ilg.Emit(OpCodes.Mul);
                        break;
                    case BinaryExpressionType.MultiplyChecked:
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
                        break;
                    case BinaryExpressionType.Divide:
                        if (IsUnsigned(_left.RuntimeType))
                        {
                            ilg.Emit(OpCodes.Div_Un);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Div);
                        }
                        break;
                    case BinaryExpressionType.Modulo:
                        if (IsUnsigned(_left.RuntimeType))
                        {
                            ilg.Emit(OpCodes.Rem_Un);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Rem);
                        }
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
                        if (IsUnsigned(_left.RuntimeType))
                        {
                            ilg.Emit(OpCodes.Shr_Un);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Shr);
                        }
                        break;
                    case BinaryExpressionType.Power:
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
                        break;
                    case BinaryExpressionType.LessThan:
                        if (IsUnsigned(_left.RuntimeType))
                        {
                            ilg.Emit(OpCodes.Clt_Un);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Clt);
                        }
                        break;
                    case BinaryExpressionType.Equal:
                        ilg.Emit(OpCodes.Ceq);
                        break;
                    case BinaryExpressionType.GreaterThan:
                        if (IsUnsigned(_left.RuntimeType))
                        {
                            ilg.Emit(OpCodes.Cgt_Un);
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Cgt);
                        }
                        break;
                    case BinaryExpressionType.GreaterThanOrEqual:
                    case BinaryExpressionType.LessThanOrEqual:
                    case BinaryExpressionType.NotEqual:
                        if (_expressionType == BinaryExpressionType.GreaterThanOrEqual)
                        {
                            if (IsUnsigned(_left.RuntimeType))
                            {
                                ilg.Emit(OpCodes.Clt_Un);
                            }
                            else
                            {
                                ilg.Emit(OpCodes.Clt);
                            }
                        }
                        else if (_expressionType == BinaryExpressionType.LessThanOrEqual)
                        {
                            if (IsUnsigned(_left.RuntimeType))
                            {
                                ilg.Emit(OpCodes.Cgt_Un);
                            }
                            else
                            {
                                ilg.Emit(OpCodes.Cgt);
                            }
                        }
                        else
                        {
                            ilg.Emit(OpCodes.Ceq);
                        }
                        ilg.Emit(OpCodes.Ldc_I4_0);
                        ilg.Emit(OpCodes.Ceq);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
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
    }
}
