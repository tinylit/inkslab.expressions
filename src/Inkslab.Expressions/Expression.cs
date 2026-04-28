using Inkslab.Emitters;
using Inkslab.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Inkslab
{
    /// <summary>
    /// 表达式。
    /// </summary>
    public abstract partial class Expression
    {
        /// <summary>
        /// 构造函数（无返回值）。
        /// </summary>
        protected Expression()
        {
            IsVoid = true;
            RuntimeType = typeof(void);
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回类型。</param>
        protected Expression(Type returnType)
        {
            RuntimeType = returnType ?? throw new ArgumentNullException(nameof(returnType));

            IsVoid = returnType == typeof(void);
        }

        private Expression(AbstractTypeEmitter typeEmitter)
        {
            if (typeEmitter is null)
            {
                throw new ArgumentNullException(nameof(typeEmitter));
            }

            var instanceType = typeEmitter.Value;

            if (instanceType.IsAbstract)
            {
                throw new AstException($"抽象类型({instanceType.Name})不支持“this”关键字！");
            }

            IsContext = true;

            RuntimeType = instanceType;
        }

        /// <summary>
        /// 当前上下文。
        /// </summary>
        /// <param name="typeEmitter">实例类型。</param>
        public static Expression This(AbstractTypeEmitter typeEmitter) => new ThisExpression(typeEmitter);

        /// <summary>
        /// 空表达式数组。
        /// </summary>
        public static readonly Expression[] EmptyAsts = new Expression[0];

        /// <summary>
        /// 是否可写。
        /// </summary>
        public virtual bool CanWrite => false;

        /// <summary>
        /// 无返回值。
        /// </summary>
        public bool IsVoid { get; }

        /// <summary>
        /// 是上下文对象（this）。
        /// </summary>
        public bool IsContext { get; }

        /// <summary>
        /// 类型。
        /// </summary>
        public Type RuntimeType
        {
            get;
        }

        /// <summary>
        /// 标记标签。
        /// </summary>
        /// <param name="label">标签。</param>
        protected internal virtual void MarkLabel(Label label)
        {
        }

        /// <summary>
        /// 将数据存储到方法返回值的变量中。
        /// </summary>
        /// <param name="variable">变量。</param>
        protected internal virtual void StoredLocal(VariableExpression variable)
        {
        }

        /// <summary>
        /// 加载数据。
        /// </summary>
        /// <param name="ilg">指令。</param>
        public abstract void Load(ILGenerator ilg);

        /// <summary>
        /// 检查是否可以进行赋值运算。
        /// </summary>
        /// <param name="left">被赋值的表达式。</param>
        /// <param name="right">值。</param>
        private static void AssignChecked(Expression left, Expression right)
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (!left.CanWrite)
            {
                throw new ArgumentException("左侧表达式不可写!");
            }

            var returnType = left.RuntimeType;

            if (returnType == typeof(void))
            {
                throw new AstException("不能对无返回值类型进行赋值运算!");
            }

            if (right is ThisExpression)
            {
                return;
            }

            var valueType = right.RuntimeType;

            if (valueType == typeof(void))
            {
                throw new AstException("无返回值类型赋值不能用于赋值运算!");
            }

            if (!ConvertChecked(returnType, valueType))
            {
                throw new AstException($"“{valueType}”无法对类型“{returnType}”赋值!");
            }
        }

        internal static bool ConvertChecked(Type returnType, Type valueType)
        {
            if (EmitUtils.IsAssignableFromSignatureTypes(returnType, valueType))
            {
                return true;
            }

            if (valueType.IsByRef || returnType.IsByRef)
            {
                if (valueType.IsByRef)
                {
                    valueType = valueType.GetElementType();
                }

                if (returnType.IsByRef)
                {
                    returnType = returnType.GetElementType();
                }

                if (EmitUtils.IsAssignableFromSignatureTypes(returnType, valueType))
                {
                    return true;
                }
            }

            if (valueType.IsEnum || returnType.IsEnum)
            {
                if (valueType.IsEnum)
                {
                    valueType = Enum.GetUnderlyingType(valueType);
                }

                if (returnType.IsEnum)
                {
                    returnType = Enum.GetUnderlyingType(returnType);
                }

                if (EmitUtils.IsAssignableFromSignatureTypes(returnType, valueType))
                {
                    return true;
                }
            }

            if (returnType.IsNullable())
            {
                if (Nullable.GetUnderlyingType(returnType) == valueType)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected virtual void Assign(ILGenerator ilg, Expression value) => throw new NotSupportedException();

        /// <summary>
        /// 结果检测。
        /// </summary>
        /// <param name="returnType">返回类型。</param>
        protected internal virtual bool DetectionResult(Type returnType) => returnType == RuntimeType;

        #region 表达式模块

        /// <summary>
        /// 赋值。
        /// </summary>
        [DebuggerDisplay("{left} = {right}")]
        private class AssignExpression : Expression
        {
            private readonly Expression _left;
            private readonly Expression _right;

            /// <summary>
            /// 赋值运算。
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <exception cref="ArgumentNullException"></exception>
            /// <exception cref="ArgumentException"></exception>
            public AssignExpression(Expression left, Expression right) : base(right.RuntimeType)
            {
                AssignChecked(left, right);

                this._left = left ?? throw new ArgumentNullException(nameof(left));
                this._right = right ?? throw new ArgumentNullException(nameof(right));
            }

            /// <summary>
            /// 发行。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Load(ILGenerator ilg) => _left.Assign(ilg, Convert(_right, RuntimeType));
        }

        /// <summary>
        /// 当前上下文。
        /// </summary>
        [DebuggerDisplay("this")]
        private class ThisExpression : Expression
        {
            public ThisExpression(AbstractTypeEmitter typeEmitter) : base(typeEmitter)
            {
            }

            /// <inheritdoc/>
            public override void Load(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldarg_0);
            }
        }

        #endregion
    }
}
