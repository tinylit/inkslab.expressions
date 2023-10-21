﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 是否为指定类型。
    /// </summary>
    [DebuggerDisplay("{body} is {isType.Name}")]
    public class TypeIsExpression : Expression
    {
        private static bool IsNullable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        private enum AnalyzeTypeIsResult
        {
            KnownFalse,
            KnownTrue,
            KnownAssignable, // need null check only
            Unknown,         // need full runtime check
        }

        private readonly Expression body;
        private readonly Type isType;


        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="body">成员。</param>
        /// <param name="isType">类型。</param>
        internal TypeIsExpression(Expression body, Type isType) : base(typeof(bool))
        {
            this.body = body ?? throw new ArgumentNullException(nameof(body));
            this.isType = isType ?? throw new ArgumentNullException(nameof(isType));

            if (body.IsVoid)
            {
                throw new AstException("表达式“is”无效！");
            }
        }

        /// <summary>
        /// 取值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            var type = body.RuntimeType;

            AnalyzeTypeIsResult result = AnalyzeTypeIs(type, isType);

            if (result == AnalyzeTypeIsResult.KnownTrue ||
                result == AnalyzeTypeIsResult.KnownFalse)
            {
                if (result == AnalyzeTypeIsResult.KnownTrue)
                {
                    ilg.Emit(OpCodes.Ldc_I4_1);
                }
                else
                {
                    ilg.Emit(OpCodes.Ldc_I4_0);
                }

                return;
            }

            if (result == AnalyzeTypeIsResult.KnownAssignable)
            {
                if (IsNullable(type))
                {
                    body.Load(ilg);

                    MethodInfo mi = type.GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);

                    ilg.Emit(OpCodes.Call, mi);

                    return;
                }

                body.Load(ilg);

                ilg.Emit(OpCodes.Ldnull);
                ilg.Emit(OpCodes.Ceq);
                ilg.Emit(OpCodes.Ldc_I4_0);
                ilg.Emit(OpCodes.Ceq);

                return;
            }

            body.Load(ilg);

            if (type.IsValueType)
            {
                ilg.Emit(OpCodes.Box, type);
            }

            ilg.Emit(OpCodes.Isinst, isType);
            ilg.Emit(OpCodes.Ldnull);
            ilg.Emit(OpCodes.Cgt_Un);
        }

        private static AnalyzeTypeIsResult AnalyzeTypeIs(Type operandType, Type testType)
        {
            if (operandType == typeof(void))
            {
                return AnalyzeTypeIsResult.KnownFalse;
            }

            if (operandType == testType)
            {
                return AnalyzeTypeIsResult.KnownTrue;
            }

            Type nnOperandType = IsNullable(operandType) ? Nullable.GetUnderlyingType(operandType) : operandType;
            Type nnTestType = IsNullable(testType) ? Nullable.GetUnderlyingType(testType) : testType;

            if (nnTestType.IsAssignableFrom(nnOperandType))
            {
                if (operandType.IsValueType && !IsNullable(operandType))
                {
                    return AnalyzeTypeIsResult.KnownTrue;
                }

                return AnalyzeTypeIsResult.KnownAssignable;
            }

            return AnalyzeTypeIsResult.Unknown;
        }
    }
}
