using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Inkslab.Expressions
{
    /// <summary>
    /// 流程。
    /// </summary>
    [DebuggerDisplay("{DebuggerView}")]
    public class SwitchExpression : Expression
    {
        private readonly Expression _defaultAst;
        private readonly Expression _switchValue;
        private readonly List<SwitchCaseExpression> _switchCases;

        private readonly Type _switchValueType;
        private readonly MySwitchValueKind _switchValueKind;

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
                    .Append(_switchValue)
                    .Append(')')
                    .Append('{');

                if (_switchValueKind == MySwitchValueKind.Equality)
                {
                    sb.AppendLine()
                        .Append("case condition: /*TODO:somethings...*/");
                }
                else
                {
                    foreach (var caseHandler in _switchCases)
                    {
                        sb.AppendLine()
                            .Append(caseHandler);
                    }
                }

                if (_defaultAst is not null)
                {
                    sb.AppendLine()
                        .Append("default:");

                    if (IsVoid)
                    {
                        sb.Append(_defaultAst)
                            .Append("; break;");
                    }
                    else
                    {
                        sb.Append("return ").Append(_defaultAst);
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

            void MarkLabel(Label label);

            void StoredLocal(VariableExpression variable);
        }

        private class MyVariableExpression : Expression
        {
            private readonly LocalBuilder _local;

            public MyVariableExpression(LocalBuilder local) : base(local.LocalType)
            {
                this._local = local;
            }

            public override void Load(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldloc, _local);
            }
        }

        private abstract class SwitchCaseExpression : BlockExpression, ICaseHandler
        {
            public abstract void EmitEqual(ILGenerator ilg);

            public abstract OpCode Equal_S { get; }

            public virtual void Emit(ILGenerator ilg, MyVariableExpression variable) => Load(ilg);

            ICaseHandler ICaseHandler.Append(Expression code)
            {
                Append(code);

                return this;
            }
        }

        private class SwitchCaseArithmeticExpression : SwitchCaseExpression
        {
            private readonly ConstantExpression _constant;

            public SwitchCaseArithmeticExpression(ConstantExpression constant)
            {
                this._constant = constant;
            }

            public override void EmitEqual(ILGenerator ilg) => _constant.Load(ilg);

            public override OpCode Equal_S => OpCodes.Beq_S;

            public override string ToString()
            {
                if (IsVoid)
                {
                    return $"case {_constant}: /*TODO:somethings...*/ break;";
                }

                return $"case {_constant}: return /*TODO:somethings...*/;";
            }
        }

        private class SwitchCaseRuntimeTypeExpression : SwitchCaseExpression
        {
            private readonly VariableExpression _variableAst;

            public SwitchCaseRuntimeTypeExpression(VariableExpression variableAst)
            {
                this._variableAst = variableAst;
            }

            public override void EmitEqual(ILGenerator ilg)
            {
                if (_variableAst.RuntimeType.IsNullable())
                {
                    ilg.Emit(OpCodes.Isinst, Nullable.GetUnderlyingType(_variableAst.RuntimeType));
                }
                else
                {
                    ilg.Emit(OpCodes.Isinst, _variableAst.RuntimeType);
                }
            }

            public override OpCode Equal_S => _variableAst.RuntimeType.IsValueType ? OpCodes.Brfalse_S : OpCodes.Brtrue_S;

            public override void Emit(ILGenerator ilg, MyVariableExpression variable)
            {
                Assign(_variableAst, Convert(variable, _variableAst.RuntimeType))
                    .Load(ilg);

                Load(ilg);
            }

            public override string ToString()
            {
                if (IsVoid)
                {
                    return $"case {_variableAst}: /*TODO:somethings...*/ break;";
                }

                return $"case {_variableAst}: return /*TODO:somethings...*/;";
            }
        }

        private class SwitchCaseEqualityAst : SwitchCaseExpression
        {
            private readonly ConstantExpression _constant;
            private readonly MethodInfo _comparison;

            public SwitchCaseEqualityAst(ConstantExpression constant, MethodInfo comparison)
            {
                this._constant = constant;
                this._comparison = comparison;
            }

            public override void EmitEqual(ILGenerator ilg)
            {
                _constant.Load(ilg);

                if (_comparison.IsStatic || _comparison.DeclaringType.IsValueType)
                {
                    ilg.Emit(OpCodes.Call, _comparison);
                }
                else
                {
                    ilg.Emit(OpCodes.Callvirt, _comparison);
                }
            }

            public override OpCode Equal_S => OpCodes.Brtrue_S;
        }

        /// <summary>
        /// 流程（无返回值）。
        /// </summary>
        internal SwitchExpression(Expression switchValue)
        {
            if (switchValue is null)
            {
                throw new ArgumentNullException(nameof(switchValue));
            }

            _switchValueType = switchValue.RuntimeType;

            if (_switchValueType == typeof(object))
            {
                _switchValueType = typeof(Type);

                _switchValueKind = MySwitchValueKind.RuntimeType;
            }
            else if (IsArithmetic(_switchValueType))
            {
                _switchValueKind = MySwitchValueKind.Arithmetic;
            }
            else
            {
                _switchValueKind = MySwitchValueKind.Equality;
            }

            _switchCases = new List<SwitchCaseExpression>();

            this._switchValue = switchValue;
        }

        /// <summary>
        /// 流程（无返回值）。
        /// </summary>
        internal SwitchExpression(Expression switchValue, Expression defaultAst) : this(switchValue)
        {
            this._defaultAst = defaultAst ?? throw new ArgumentNullException(nameof(defaultAst));
        }

        /// <inheritdoc/>
        protected internal override void MarkLabel(Label label)
        {
            if (label is null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            _switchValue.MarkLabel(label);

            if (label.Kind != LabelKind.Break)
            {
                foreach (var item in _switchCases)
                {
                    item.MarkLabel(label);
                }

                _defaultAst?.MarkLabel(label);
            }
        }
        /// <inheritdoc/>
        protected internal override void StoredLocal(VariableExpression variable)
        {
            if (variable is null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            _switchValue.StoredLocal(variable);

            foreach (var switchCase in _switchCases)
            {
                switchCase.StoredLocal(variable);
            }

            _defaultAst?.StoredLocal(variable);
        }
        /// <inheritdoc/>
        protected internal override bool DetectionResult(Type returnType)
        {
            foreach (var switchCase in _switchCases)
            {
                if (!switchCase.DetectionResult(returnType))
                {
                    return false;
                }
            }

            return _defaultAst is null || _defaultAst.DetectionResult(returnType);
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

            SwitchCaseExpression switchCase;

            switch (_switchValueKind)
            {
                case MySwitchValueKind.Arithmetic when IsArithmetic(constant.RuntimeType):
                    switchCase = new SwitchCaseArithmeticExpression(constant);
                    break;
                case MySwitchValueKind.RuntimeType:
                    throw new AstException("当前流程控制为类型转换，请使用“{Case(VariableAst variable)}”方法处理！");
                case MySwitchValueKind.Equality:
                    var types = new Type[2] { _switchValueType, constant.RuntimeType };

                    MethodInfo comparison = _switchValueType.GetMethod("op_Equality", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);

                    if (comparison is null)
                    {
                        if (EmitUtils.AreEquivalent(_switchValueType, constant.RuntimeType))
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
                        if (_switchValueType.IsAssignableFrom(typeof(IEquatable<>).MakeGenericType(constant.RuntimeType)))
                        {
                            comparison = _switchValueType.GetMethod("Equals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { constant.RuntimeType }, null);
                        }
                    }

label_break:
                    if (comparison is null)
                    {
                        throw new InvalidOperationException($"未找到“{constant.RuntimeType}”和“{_switchValueType}”有效的比较函数!");
                    }

                    switchCase = new SwitchCaseEqualityAst(constant, comparison);

                    break;
                default:
                    throw new NotSupportedException();
            }

            _switchCases.Add(switchCase);

            return switchCase;
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

            SwitchCaseExpression switchCase = _switchValueKind switch
            {
                MySwitchValueKind.RuntimeType => new SwitchCaseRuntimeTypeExpression(variable),
                MySwitchValueKind.Arithmetic or MySwitchValueKind.Equality => throw new AstException("当前流程控制为值比较转换，请使用“{Case(ConstantAst constant)}”方法处理！"),
                _ => throw new NotSupportedException(),
            };
            _switchCases.Add(switchCase);

            return switchCase;
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public override void Load(ILGenerator ilg)
        {
            if (_switchCases.Count == 0)
            {
                if (_defaultAst is null)
                {
                    throw new AstException("表达式残缺，未设置“case”代码块和“default”代码块至少设置其一！");
                }

                _defaultAst.Load(ilg);
            }
            else
            {
                var label = new Label(LabelKind.Break);

                foreach (var item in _switchCases)
                {
                    item.MarkLabel(label);
                }

                _defaultAst?.MarkLabel(label);

                Emit(ilg);

                label.MarkLabel(ilg);

                ilg.Emit(OpCodes.Nop);
            }
        }

        /// <summary>
        /// 发行。
        /// </summary>
        /// <param name="ilg">指令。</param>
        protected virtual void Emit(ILGenerator ilg)
        {
            LocalBuilder variable = ilg.DeclareLocal(_switchValue.RuntimeType);

            _switchValue.Load(ilg);

            ilg.Emit(OpCodes.Stloc, variable);

            int i = 0, len = _switchCases.Count;

            var labels = new System.Reflection.Emit.Label[len];

            for (; i < len; i++)
            {
                labels[i] = ilg.DefineLabel();
            }

            for (i = 0; i < len; i++)
            {
                var switchCase = _switchCases[i];

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
