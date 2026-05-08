using System;
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
        private readonly Expression _test;
        private readonly Expression _ifTrue;
        private readonly Expression _ifFalse;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真的代码块。</param>
        /// <param name="ifFalse">为假的代码块。</param>
        /// <param name="returnType">结果类型。</param>
        public ConditionExpression(Expression test, Expression ifTrue, Expression ifFalse, Type returnType) : base(returnType)
        {
            _test = test;
            _ifTrue = returnType == ifTrue.RuntimeType ? ifTrue : new ConvertExpression(ifTrue, returnType);
            _ifFalse = returnType == ifFalse.RuntimeType ? ifFalse : new ConvertExpression(ifFalse, returnType);
        }

        /// <summary>
        /// 生成。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            var label = ilg.DefineLabel();
            var leave = ilg.DefineLabel();
            var variable = ilg.DeclareLocal(RuntimeType);

            _test.Load(ilg);

            ilg.Emit(OpCodes.Brfalse_S, label);

            ilg.Emit(OpCodes.Nop);

            _ifTrue.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            ilg.Emit(OpCodes.Leave_S, leave);

            ilg.MarkLabel(label);

            _ifFalse.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            ilg.MarkLabel(leave);

            ilg.Emit(OpCodes.Ldloc, variable);
        }
    }
}
