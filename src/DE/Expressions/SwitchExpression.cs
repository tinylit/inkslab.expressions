using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Delta.Expressions
{
    /// <summary>
    /// 流程。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class SwitchExpression : Expression
    {
        private readonly Expression defaultAst;
        private readonly Expression switchValue;
        private readonly Label breakLabel;
        private readonly List<IPrivateCaseHandler> switchCases;

        private readonly Type switchValueType;
        private readonly MySwitchValueKind switchValueKind;

        private enum MySwitchValueKind
        {
            Arithmetic,
            RuntimeType,
            Equality
        }

        [DebuggerHidden]
        private string DebuggerView
        {
            get
            {
                var sb = new StringBuilder();

                sb.Append("switch(")
                    .Append(switchValue)
                    .Append(')')
                    .Append('{');

                if (switchValueKind == MySwitchValueKind.Equality)
                {
                    sb.AppendLine()
                        .Append("case condition: /*TODO:somethings...*/");
                }
                else
                {
                    foreach (var caseHandler in switchCases)
                    {
                        sb.AppendLine()
                            .Append(caseHandler);
                    }
                }

                if (defaultAst is not null)
                {
                    sb.AppendLine()
                        .Append("default:");

                    if (RuntimeType == typeof(void))
                    {
                        sb.Append(defaultAst)
                            .Append("; break;");
                    }
                    else
                    {
                        sb.Append("return ").Append(defaultAst);
                    }
                }

                return sb.AppendLine()
                        .Append('}')
                        .ToString();
            }
        }

        /// <summary>
        /// 案例处理。
        /// </summary>
        public interface ICaseHandler
        {
            /// <summary>
            /// 添加表达式。
            /// </summary>
            /// <param name="code">表达式。</param>
            /// <returns></returns>
            ICaseHandler Append(Expression code);
        }

        private interface IPrivateCaseHandler : ICaseHandler
        {
            OpCode Equal_S { get; }

            void EmitEqual(ILGenerator ilg);

            void Emit(ILGenerator ilg, MyVariableExpression variable);
        }

        private class MyVariableExpression : Expression
        {
            private readonly LocalBuilder local;

            public MyVariableExpression(LocalBuilder local) : base(local.LocalType)
            {
                this.local = local;
            }

            public override void Load(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldloc, local);
            }
        }

        private class SwitchCaseArithmeticExpression : BlockExpression, IPrivateCaseHandler
        {
            private readonly ConstantExpression constant;

            public SwitchCaseArithmeticExpression(ConstantExpression constant, Type returnType) : base(returnType)
            {
                this.constant = constant;
            }

            public void EmitEqual(ILGenerator ilg)
            {
                constant.Load(ilg);
            }

            public OpCode Equal_S => OpCodes.Beq_S;

            ICaseHandler ICaseHandler.Append(Expression code)
            {
                Append(code);

                return this;
            }

            void IPrivateCaseHandler.Emit(ILGenerator ilg, MyVariableExpression variable) => Load(ilg);

            public override string ToString()
            {
                if (RuntimeType == typeof(void))
                {
                    return $"case {constant}: /*TODO:somethings...*/ break;";
                }

                return $"case {constant}: return /*TODO:somethings...*/;";
            }
        }

        private class SwitchCaseRuntimeTypeExpression : BlockExpression, IPrivateCaseHandler
        {
            private readonly VariableExpression variableAst;

            public SwitchCaseRuntimeTypeExpression(VariableExpression variableAst, Type returnType) : base(returnType)
            {
                this.variableAst = variableAst;
            }

            public void EmitEqual(ILGenerator ilg)
            {
                if (variableAst.RuntimeType.IsNullable())
                {
                    ilg.Emit(OpCodes.Isinst, Nullable.GetUnderlyingType(variableAst.RuntimeType));
                }
                else
                {
                    ilg.Emit(OpCodes.Isinst, variableAst.RuntimeType);
                }
            }

            public OpCode Equal_S => variableAst.RuntimeType.IsValueType ? OpCodes.Brfalse_S : OpCodes.Brtrue_S;

            ICaseHandler ICaseHandler.Append(Expression code)
            {
                Append(code);

                return this;
            }

            void IPrivateCaseHandler.Emit(ILGenerator ilg, MyVariableExpression variable)
            {
                Assign(variableAst, Convert(variable, variableAst.RuntimeType))
                    .Load(ilg);

                Load(ilg);
            }

            public override string ToString()
            {
                if (RuntimeType == typeof(void))
                {
                    return $"case {variableAst}: /*TODO:somethings...*/ break;";
                }

                return $"case {variableAst}: return /*TODO:somethings...*/;";
            }
        }

        private class SwitchCaseEqualityAst : BlockExpression, IPrivateCaseHandler
        {
            private readonly ConstantExpression constant;
            private readonly MethodInfo comparison;

            public SwitchCaseEqualityAst(ConstantExpression constant, MethodInfo comparison, Type returnType) : base(returnType)
            {
                this.constant = constant;
                this.comparison = comparison;
            }

            public void EmitEqual(ILGenerator ilg)
            {
                constant.Load(ilg);

                if (comparison.IsStatic || comparison.DeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Call, comparison);
                }
                else
                {
                    ilg.Emit(OpCodes.Callvirt, comparison);
                }
            }

            public OpCode Equal_S => OpCodes.Brtrue_S;

            ICaseHandler ICaseHandler.Append(Expression code)
            {
                Append(code);

                return this;
            }

            void IPrivateCaseHandler.Emit(ILGenerator ilg, MyVariableExpression variable) => Load(ilg);
        }

        private class SwitchCase
        {
            public SwitchCase(ConstantExpression value, Expression body)
            {
                Value = value;
                Body = body;
            }

            public ConstantExpression Value { get; }
            public Expression Body { get; }
        }

        internal SwitchExpression(Expression switchValue, Type returnType, Label breakLabel) : base(returnType)
        {
            if (switchValue is null)
            {
                throw new ArgumentNullException(nameof(switchValue));
            }

            switchValueType = switchValue.RuntimeType;

            if (switchValueType == typeof(object))
            {
                switchValueType = typeof(Type);

                switchValueKind = MySwitchValueKind.RuntimeType;
            }
            else if (IsArithmetic(switchValueType))
            {
                switchValueKind = MySwitchValueKind.Arithmetic;
            }
            else
            {
                switchValueKind = MySwitchValueKind.Equality;
            }

            switchCases = new List<IPrivateCaseHandler>();

            this.switchValue = switchValue;
            this.breakLabel = breakLabel;
        }

        /// <summary>
        /// 流程（无返回值）。
        /// </summary>
        internal SwitchExpression(Expression switchValue, Label breakLabel) : this(switchValue, typeof(void), breakLabel)
        {

        }

        /// <summary>
        /// 流程（无返回值）。
        /// </summary>
        internal SwitchExpression(Expression switchValue, Expression defaultAst, Label breakLabel) : this(switchValue, breakLabel)
        {
            this.defaultAst = defaultAst ?? throw new ArgumentNullException(nameof(defaultAst));
        }

        /// <summary>
        /// 流程。
        /// </summary>
        internal SwitchExpression(Expression switchValue, Expression defaultAst, Type returnType, Label breakLabel) : this(switchValue, returnType, breakLabel)
        {
            if (defaultAst is null)
            {
                throw new ArgumentNullException(nameof(defaultAst));
            }

            if (returnType == typeof(void) || EmitUtils.EqualSignatureTypes(defaultAst.RuntimeType, returnType) || defaultAst.RuntimeType.IsAssignableFrom(RuntimeType))
            {
                this.defaultAst = defaultAst;
            }
            else
            {
                throw new NotSupportedException($"默认模块“{defaultAst.RuntimeType}”和返回“{returnType}”类型无法默认转换!");
            }
        }

        /// <summary>
        /// 实例。
        /// </summary>
        /// <param name="constant">常量。</param>
        public ICaseHandler Case(ConstantExpression constant)
        {
            if (constant is null)
            {
                throw new ArgumentNullException(nameof(constant));
            }

            IPrivateCaseHandler handler;

            switch (switchValueKind)
            {
                case MySwitchValueKind.Arithmetic when IsArithmetic(constant.RuntimeType):
                    handler = new SwitchCaseArithmeticExpression(constant, RuntimeType);
                    break;
                case MySwitchValueKind.RuntimeType:
                    throw new AstException("当前流程控制为类型转换，请使用“{Case(VariableAst variable)}”方法处理！");
                case MySwitchValueKind.Equality:
                    var types = new Type[2] { switchValueType, constant.RuntimeType };

                    MethodInfo comparison = switchValueType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);

                    if (comparison is null)
                    {
                        if (EmitUtils.AreEquivalent(switchValueType, constant.RuntimeType))
                        {
                            goto label_equals;
                        }
                        else
                        {
                            comparison = constant.RuntimeType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);

                            if (comparison is null)
                            {
                                goto label_equals;
                            }
                        }

                        goto label_break;

label_equals:
                        if (switchValueType.IsAssignableFrom(typeof(IEquatable<>).MakeGenericType(constant.RuntimeType)))
                        {
                            comparison = switchValueType.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { constant.RuntimeType }, null);
                        }
                    }

label_break:

                    if (comparison is null)
                    {
                        throw new InvalidOperationException($"未找到“{constant.RuntimeType}”和“{switchValueType}”有效的比较函数!");
                    }

                    handler = new SwitchCaseEqualityAst(constant, comparison, RuntimeType);

                    break;
                default:
                    throw new NotSupportedException();
            }

            switchCases.Add(handler);

            return handler;
        }

        /// <summary>
        /// 实例（转换成功会自动为变量赋值）。
        /// </summary>
        /// <param name="variable">变量。</param>
        public ICaseHandler Case(VariableExpression variable)
        {
            if (variable is null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            IPrivateCaseHandler handler;

            switch (switchValueKind)
            {
                case MySwitchValueKind.RuntimeType:
                    handler = new SwitchCaseRuntimeTypeExpression(variable, RuntimeType);
                    break;
                case MySwitchValueKind.Arithmetic:
                case MySwitchValueKind.Equality:
                    throw new AstException("当前流程控制为值比较转换，请使用“{Case(ConstantAst constant)}”方法处理！");
                default:
                    throw new NotSupportedException();
            }

            switchCases.Add(handler);

            return handler;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (switchCases.Count == 0)
            {
                if (defaultAst is null)
                {
                    throw new AstException("表达式残缺，未设置“case”代码块和“default”代码块至少设置其一！");
                }

                defaultAst.Load(ilg);
            }
            else if (RuntimeType == typeof(void))
            {
                Emit(ilg);

                breakLabel.MarkLabel(ilg);

                ilg.Emit(OpCodes.Nop);
            }
            else
            {
                var local = ilg.DeclareLocal(RuntimeType);

                Emit(ilg);

                breakLabel.MarkLabel(ilg);

                ilg.Emit(OpCodes.Ldloc, local);
            }
        }

        /// <summary>
        /// 发行（有返回值）。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected virtual void Emit(ILGenerator ilg)
        {
            LocalBuilder variable = ilg.DeclareLocal(switchValue.RuntimeType);

            switchValue.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            int i = 0, len = switchCases.Count;

            var labels = new System.Reflection.Emit.Label[len];

            for (; i < len; i++)
            {
                labels[i] = ilg.DefineLabel();
            }

            for (i = 0; i < len; i++)
            {
                var switchCase = switchCases[i];

                ilg.Emit(OpCodes.Ldloc, variable);

                switchCase.EmitEqual(ilg);

                var caseLabel = ilg.DefineLabel();

                ilg.Emit(switchCase.Equal_S, caseLabel);

                ilg.Emit(OpCodes.Br_S, labels[i]);

                ilg.MarkLabel(caseLabel);

                switchCase.Emit(ilg, new MyVariableExpression(variable));

                ilg.MarkLabel(labels[i]);
            }
        }

        private static bool IsArithmetic(Type type)
        {
            if (type.IsEnum || type.IsNullable())
            {
                return false;
            }

            switch (Type.GetTypeCode(type))
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
