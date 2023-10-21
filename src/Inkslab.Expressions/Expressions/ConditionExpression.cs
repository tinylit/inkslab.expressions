﻿using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 判断。
    /// </summary>
    [DebuggerDisplay("{test} ? {ifTrue} : {ifFalse}")]
    public class ConditionExpression : Expression
    {
        private readonly Expression test;
        private readonly Expression ifTrue;
        private readonly Expression ifFalse;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        /// <param name="ifFalse">为假的代码块。</param>
        internal ConditionExpression(Expression test, Expression ifTrue, Expression ifFalse) : this(test, ifTrue, ifFalse, AnalysisReturnType(ifTrue, ifFalse))
        {

        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        /// <param name="ifFalse">为假的代码块。</param>
        /// <param name="returnType">结果类型。</param>
        public ConditionExpression(Expression test, Expression ifTrue, Expression ifFalse, Type returnType) : base(returnType)
        {
            this.test = test ?? throw new ArgumentNullException(nameof(test));

            if (test.RuntimeType == typeof(bool))
            {
                this.ifTrue = ifTrue ?? throw new ArgumentNullException(nameof(ifTrue));
                this.ifFalse = ifFalse ?? throw new ArgumentNullException(nameof(ifFalse));
            }
            else
            {
                throw new ArgumentException("不是有效的条件语句!", nameof(test));
            }

            if (returnType == typeof(void))
            {
                return;
            }

            if (returnType == ifTrue.RuntimeType)
            {

            }
            else if (EmitUtils.IsAssignableFromSignatureTypes(returnType, ifTrue.RuntimeType))
            {
                this.ifTrue = new ConvertExpression(ifTrue, returnType);
            }
            else
            {
                throw new ArgumentException($"表达式类型“{ifTrue.RuntimeType}”不能默认转换为“{returnType}”!", nameof(ifTrue));
            }

            if (returnType == ifFalse.RuntimeType)
            {

            }
            else if (EmitUtils.IsAssignableFromSignatureTypes(returnType, ifTrue.RuntimeType))
            {
                this.ifFalse = new ConvertExpression(ifFalse, returnType);
            }
            else
            {
                throw new ArgumentException($"表达式类型“{ifFalse.RuntimeType}”不能默认转换为“{returnType}”!", nameof(ifFalse));
            }

        }

        private static Type AnalysisReturnType(Expression ifTrue, Expression ifFalse)
        {
            if (ifTrue is null)
            {
                throw new ArgumentNullException(nameof(ifTrue));
            }

            if (ifFalse is null)
            {
                throw new ArgumentNullException(nameof(ifFalse));
            }

            if (ifTrue.IsVoid || ifFalse.IsVoid)
            {
                return typeof(void);
            }

            if (ifTrue.RuntimeType == ifFalse.RuntimeType)
            {
                return ifTrue.RuntimeType;
            }

            if (EmitUtils.IsAssignableFromSignatureTypes(ifTrue.RuntimeType, ifFalse.RuntimeType))
            {
                return ifTrue.RuntimeType;
            }

            if (ifTrue.RuntimeType.IsSubclassOf(ifFalse.RuntimeType))
            {
                return ifFalse.RuntimeType;
            }

            return typeof(void);
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (IsVoid)
            {
                EmitVoid(ilg);
            }
            else
            {
                Emit(ilg);
            }
        }

        private void Emit(ILGenerator ilg)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();
            var variable = ilg.DeclareLocal(RuntimeType);

            test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ilg.Emit(OpCodes.Nop);

            ifTrue.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            ilg.Emit(OpCodes.Leave_S, leave);

            ilg.MarkLabel(label);

            ifFalse.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            ilg.MarkLabel(leave);

            ilg.Emit(OpCodes.Ldloc, variable);
        }

        private void EmitVoid(ILGenerator ilg)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();

            test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ilg.Emit(OpCodes.Nop);

            ifTrue.Load(ilg);

            if (ifFalse.RuntimeType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(label);

            ilg.Emit(OpCodes.Leave_S, leave);

            ilg.MarkLabel(label);

            ifFalse.Load(ilg);

            if (ifTrue.RuntimeType != typeof(void))
            {
                ilg.Emit(OpCodes.Pop);
            }

            ilg.MarkLabel(leave);
        }
    }
}
