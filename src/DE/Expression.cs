﻿using Delta.Expressions;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Delta
{
    /// <summary>
    /// 表达式。
    /// </summary>
    public abstract class Expression
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="returnType">返回类型。</param>
        protected Expression(Type returnType) => RuntimeType = returnType ?? throw new ArgumentNullException(nameof(returnType));

        /// <summary>
        /// 当前上下文。
        /// </summary>
        public static Expression This => ThisExpression.Instance;

        /// <summary>
        /// 清除堆栈顶部数据。
        /// </summary>
        public static Expression Nop => NopExpression.Instance;

        /// <summary>
        /// 空表达式数组。
        /// </summary>
        public static readonly Expression[] EmptyAsts = new Expression[0];

        /// <summary>
        /// 是否可写。
        /// </summary>
        public virtual bool CanWrite => false;

        /// <summary>
        /// 类型。
        /// </summary>
        public Type RuntimeType { get; private set; }

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
        private static bool AssignChecked(Expression left, Expression right)
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
                return false;
            }

            var returnType = left.RuntimeType;

            if (returnType == typeof(void))
            {
                throw new AstException("不能对无返回值类型进行赋值运算!");
            }

            if (right is ThisExpression)
            {
                return true;
            }

            var valueType = right.RuntimeType;

            if (valueType == typeof(void))
            {
                throw new AstException("无返回值类型赋值不能用于赋值运算!");
            }

            if (valueType == returnType || returnType.IsAssignableFrom(valueType))
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

                if (valueType == returnType || returnType.IsAssignableFrom(valueType))
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

                if (valueType == returnType || returnType.IsAssignableFrom(valueType))
                {
                    return true;
                }
            }

            if (returnType.IsNullable())
            {
                return Enum.GetUnderlyingType(returnType) == valueType;
            }

            throw new AstException($"“{right.RuntimeType}”无法对类型“{left.RuntimeType}”赋值!");
        }

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="ilg">指令。</param>
        /// <param name="value">值。</param>
        protected virtual void Assign(ILGenerator ilg, Expression value) => throw new NotSupportedException();

        #region 表达式模块

        /// <summary>
        /// 赋值。
        /// </summary>
        [DebuggerDisplay("{left} = {right}")]
        private class AssignExpression : Expression
        {
            private readonly Expression left;
            private readonly Expression right;

            /// <summary>
            /// 赋值运算。
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <exception cref="ArgumentNullException"></exception>
            /// <exception cref="ArgumentException"></exception>
            public AssignExpression(Expression left, Expression right) : base(right.RuntimeType)
            {
                this.left = left ?? throw new ArgumentNullException(nameof(left));

                if (AssignChecked(left, right))
                {
                    this.right = right ?? throw new ArgumentNullException(nameof(right));
                }
                else
                {
                    throw new ArgumentException("表达式左侧只读!");
                }
            }

            /// <summary>
            /// 发行。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Load(ILGenerator ilg) => left.Assign(ilg, Convert(right, RuntimeType));
        }

        /// <summary>
        /// 成员本身。
        /// </summary>
        [DebuggerDisplay("this")]
        private class ThisExpression : Expression
        {
            /// <summary>
            /// 单例。
            /// </summary>
            public static ThisExpression Instance = new ThisExpression();

            /// <summary>
            /// 构造函数。
            /// </summary>
            private ThisExpression() : base(typeof(object))
            {
            }

            /// <summary>
            /// 加载。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Load(ILGenerator ilg)
            {
                ilg.Emit(OpCodes.Ldarg_0);
            }
        }

        private class NopExpression : Expression
        {
            /// <summary>
            /// 单例。
            /// </summary>
            public static NopExpression Instance = new NopExpression();

            /// <summary>
            /// 构造函数。
            /// </summary>
            private NopExpression() : base(typeof(void))
            {
            }

            /// <summary>
            /// 加载。
            /// </summary>
            /// <param name="ilg">指令。</param>
            public override void Load(ILGenerator ilg) => ilg.Emit(OpCodes.Nop);
        }
        #endregion

        /// <summary>
        /// 类型转换。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <param name="convertToType">转换类型。</param>
        /// <returns>类型转换表达式。</returns>
        public static ConvertExpression Convert(Expression body, Type convertToType) => new ConvertExpression(body, convertToType);

        /// <summary>
        /// 默认值。
        /// </summary>
        /// <param name="defaultType">默认值。</param>
        /// <returns>默认值表达式。</returns>
        public static DefaultExpression Default(Type defaultType) => new DefaultExpression(defaultType);

        /// <summary>
        /// 常量。
        /// </summary>
        /// <param name="value">值。</param>
        /// <returns>常量表达式。</returns>
        public static ConstantExpression Constant(object value) => new ConstantExpression(value);

        /// <summary>
        /// 常量。
        /// </summary>
        /// <param name="value">值。</param>
        /// <param name="constantType">常量类型。</param>
        /// <returns>常量表达式。</returns>
        public static ConstantExpression Constant(object value, Type constantType) => new ConstantExpression(value, constantType);

        /// <summary>
        /// 类型是。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <param name="bodyIsType">类型。</param>
        /// <returns>类型判断表达式。</returns>
        public static TypeIsExpression TypeIs(Expression body, Type bodyIsType) => new TypeIsExpression(body, bodyIsType);

        /// <summary>
        /// 类型转为。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <param name="bodyAsType">类型。</param>
        /// <returns>类型判断与转换表达式。</returns>
        public static TypeAsExpression TypeAs(Expression body, Type bodyAsType) => new TypeAsExpression(body, bodyAsType);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <returns>创建实例表达式。</returns>
        public static NewInstanceExpression New(Type instanceType) => new NewInstanceExpression(instanceType);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        /// <returns>创建实例表达式。</returns>
        public static NewInstanceExpression New(ConstructorInfo constructor) => new NewInstanceExpression(constructor);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <param name="parameters">参数。</param>
        /// <returns>创建实例表达式。</returns>
        public static NewInstanceExpression New(Type instanceType, params Expression[] parameters) => new NewInstanceExpression(instanceType, parameters);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        /// <param name="parameters">参数。</param>
        /// <returns>创建实例表达式。</returns>
        public static NewInstanceExpression New(ConstructorInfo constructor, params Expression[] parameters) => new NewInstanceExpression(constructor, parameters);

        /// <summary>
        /// 创建 object[]。
        /// </summary>
        /// <param name="size">数组大小。</param>
        /// <returns>创建数组表达式。</returns>
        public static NewArrayExpression NewArray(int size) => new NewArrayExpression(size);

        /// <summary>
        /// 创建 <paramref name="elementType"/>[]。
        /// </summary>
        /// <param name="size">数组大小。</param>
        /// <param name="elementType">数组元素类型。</param>
        /// <returns>创建数组表达式。</returns>
        public static NewArrayExpression NewArray(int size, Type elementType) => new NewArrayExpression(size, elementType);

        /// <summary>
        /// 创建 object[]。
        /// </summary>
        /// <param name="arguments">元素。</param>
        /// <returns>创建并初始化数组元素表达式。</returns>
        public static ArrayExpression Array(params Expression[] arguments) => new ArrayExpression(arguments);

        /// <summary>
        /// 创建 <paramref name="elementType"/>[]。
        /// </summary>
        /// <param name="elementType">元素类型。</param>
        /// <param name="arguments">元素。</param>
        /// <returns>创建并初始化数组元素表达式。</returns>
        public static ArrayExpression Array(Type elementType, params Expression[] arguments) => new ArrayExpression(arguments, elementType);

        /// <summary>
        /// 数组索引。
        /// </summary>
        /// <param name="array">数组。</param>
        /// <param name="index">索引。</param>
        /// <returns>数组索引表达式。</returns>
        public static ArrayIndexExpression ArrayIndex(Expression array, int index) => new ArrayIndexExpression(array, index);

        /// <summary>
        /// 数组索引。
        /// </summary>
        /// <param name="array">数组。</param>
        /// <param name="index">索引。</param>
        /// <returns>数组索引表达式。</returns>
        public static ArrayIndexExpression ArrayIndex(Expression array, Expression index) => new ArrayIndexExpression(array, index);

        /// <summary>
        /// 数组长度。
        /// </summary>
        /// <param name="array">数组。</param>
        /// <returns>获得数组长度表达式。</returns>
        public static ArrayLengthExpression ArrayLength(Expression array) => new ArrayLengthExpression(array);

        /// <summary>
        /// 赋值。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>赋值运算表达式。</returns>
        public static Expression Assign(Expression left, Expression right) => new AssignExpression(left, right);

        /// <summary>
        /// 空合并运算符。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>空合并运算表达式。</returns>
        public static CoalesceExpression Coalesce(Expression left, Expression right) => new CoalesceExpression(left, right);

        /// <summary>
        /// 加。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression Add(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Add, right);

        /// <summary>
        /// 加(检查溢出)。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression AddChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AddChecked, right);

        /// <summary>
        /// 加等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression AddAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AddAssign, right);

        /// <summary>
        /// 加等于(检查溢出)。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression AddAssignChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AddAssignChecked, right);

        /// <summary>
        /// 减。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression Subtract(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Subtract, right);

        /// <summary>
        /// 减(检查溢出)。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression SubtractChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.SubtractChecked, right);

        /// <summary>
        /// 减等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression SubtractAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.SubtractAssign, right);

        /// <summary>
        /// 减等于(检查溢出)。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression SubtractAssignChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.SubtractAssignChecked, right);

        /// <summary>
        /// 乘。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression Multiply(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Multiply, right);

        /// <summary>
        /// 乘（检查溢出）。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression MultiplyChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.MultiplyChecked, right);

        /// <summary>
        /// 乘等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression MultiplyAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.MultiplyAssign, right);

        /// <summary>
        /// 乘等于（检查溢出）。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression MultiplyAssignChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.MultiplyAssignChecked, right);

        /// <summary>
        /// 除。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression Divide(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Divide, right);

        /// <summary>
        /// 除等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression DivideAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.DivideAssign, right);

        /// <summary>
        /// 取模。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression Modulo(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Modulo, right);

        /// <summary>
        /// 取模等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression ModuloAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.ModuloAssign, right);

        /// <summary>
        /// 小于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression LessThan(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.LessThan, right);

        /// <summary>
        /// 小于等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression LessThanOrEqual(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.LessThanOrEqual, right);

        /// <summary>
        /// 位运算：或。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression Or(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Or, right);

        /// <summary>
        /// 位运算：或等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression OrAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.OrAssign, right);

        /// <summary>
        /// 位运算：且。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression And(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.And, right);

        /// <summary>
        /// 位运算：且等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression AndAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AndAssign, right);

        /// <summary>
        /// 位运算：异或。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression ExclusiveOr(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.ExclusiveOr, right);

        /// <summary>
        /// 位运算：异或等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression ExclusiveOrAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.ExclusiveOrAssign, right);

        /// <summary>
        /// 等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression Equal(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Equal, right);

        /// <summary>
        /// 大于等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression GreaterThanOrEqual(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.GreaterThanOrEqual, right);

        /// <summary>
        /// 大于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression GreaterThan(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.GreaterThan, right);

        /// <summary>
        /// 不等于。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression NotEqual(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.NotEqual, right);

        /// <summary>
        /// 或。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression OrElse(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.OrElse, right);

        /// <summary>
        /// 且。
        /// </summary>
        /// <param name="left">左表达式。</param>
        /// <param name="right">右表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static BinaryExpression AndAlso(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AndAlso, right);

        /// <summary>
        /// 按位补运算或逻辑反运算。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static UnaryExpression Not(Expression body) => new UnaryExpression(body, UnaryExpressionType.Not);

        /// <summary>
        /// 是否为假。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns>二元运算表达式。</returns>
        public static UnaryExpression IsFalse(Expression body) => new UnaryExpression(body, UnaryExpressionType.IsFalse);

        /// <summary>
        /// 增量(i + 1)。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns>一元运算表达式。</returns>
        public static UnaryExpression Increment(Expression body) => new UnaryExpression(body, UnaryExpressionType.Increment);

        /// <summary>
        /// 减量(i - 1)。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns>一元运算表达式。</returns>
        public static UnaryExpression Decrement(Expression body) => new UnaryExpression(body, UnaryExpressionType.Decrement);

        /// <summary>
        /// 递增(i += 1)。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns>一元运算表达式。</returns>
        public static UnaryExpression IncrementAssign(Expression body) => new UnaryExpression(body, UnaryExpressionType.IncrementAssign);

        /// <summary>
        /// 递减(i -= 1)。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns>一元运算表达式。</returns>
        public static UnaryExpression DecrementAssign(Expression body) => new UnaryExpression(body, UnaryExpressionType.DecrementAssign);

        /// <summary>
        /// 正负反转。
        /// </summary>
        /// <param name="body">表达式。</param>
        /// <returns>一元运算表达式。</returns>
        public static UnaryExpression Negate(Expression body) => new UnaryExpression(body, UnaryExpressionType.Negate);

        /// <summary>
        /// 流程（无返回值）。
        /// </summary>
        /// <param name="switchValue">判断依据。</param>
        /// <param name="breakLabel">跳出流程。</param>
        /// <returns> 流程控制表达式。</returns>
        public static SwitchExpression Switch(Expression switchValue, Label breakLabel) => new SwitchExpression(switchValue, breakLabel);

        /// <summary>
        /// 流程(无返回值)。
        /// </summary>
        /// <param name="switchValue">判断依据。</param>
        /// <param name="defaultAst">默认流程。</param>
        /// <param name="breakLabel">跳出流程。</param>
        /// <returns> 流程控制表达式。</returns>
        public static SwitchExpression Switch(Expression switchValue, Expression defaultAst, Label breakLabel) => new SwitchExpression(switchValue, defaultAst, breakLabel);

        /// <summary>
        /// 流程(返回“<paramref name="returnType"/>”类型)。
        /// </summary>
        /// <param name="switchValue">判断依据。</param>
        /// <param name="defaultAst">默认流程。</param>
        /// <param name="returnType">流程返回值。</param>
        /// <param name="breakLabel">跳出流程。</param>
        /// <returns> switch 表达式。</returns>
        public static SwitchExpression Switch(Expression switchValue, Expression defaultAst, Type returnType, Label breakLabel) => new SwitchExpression(switchValue, defaultAst, returnType, breakLabel);

        /// <summary>
        /// 条件判断。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真时，执行的代码。</param>
        /// <returns>流程表达式。</returns>
        public static IfThenExpression IfThen(Expression test, Expression ifTrue) => new IfThenExpression(test, ifTrue);

        /// <summary>
        /// 条件判断。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真时，执行的代码。</param>
        /// <param name="ifFalse">为假时，执行的代码。</param>
        /// <returns>流程表达式。</returns>
        public static IfThenElseExpression IfThenElse(Expression test, Expression ifTrue, Expression ifFalse) => new IfThenElseExpression(test, ifTrue, ifFalse);

        /// <summary>
        /// 三目运算。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真时，执行的代码。</param>
        /// <param name="ifFalse">为假时，执行的代码。</param>
        /// <returns>流程表达式。</returns>
        public static ConditionExpression Condition(Expression test, Expression ifTrue, Expression ifFalse) => new ConditionExpression(test, ifTrue, ifFalse);

        /// <summary>
        /// 三目运算。
        /// </summary>
        /// <param name="test">条件。</param>
        /// <param name="ifTrue">为真时，执行的代码。</param>
        /// <param name="ifFalse">为假时，执行的代码。</param>
        /// <param name="returnType">返回类型。</param>
        /// <returns>流程表达式。</returns>
        public static ConditionExpression Condition(Expression test, Expression ifTrue, Expression ifFalse, Type returnType) => new ConditionExpression(test, ifTrue, ifFalse, returnType);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <returns>方法调用表达式。</returns>
        public static MethodCallExpression Call(MethodInfo methodInfo) => new MethodCallExpression(methodInfo);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">方法参数。</param>
        /// <returns>方法调用表达式。</returns>
        public static MethodCallExpression Call(MethodInfo methodInfo, params Expression[] arguments) => new MethodCallExpression(methodInfo, arguments);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">方法。</param>
        /// <returns>方法调用表达式。</returns>
        public static MethodCallExpression Call(Expression instanceAst, MethodInfo methodInfo) => new MethodCallExpression(instanceAst, methodInfo);

        /// <summary>
        /// 调用方法。
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">方法参数。</param>
        /// <returns>方法调用表达式。</returns>
        public static MethodCallExpression Call(Expression instanceAst, MethodInfo methodInfo, params Expression[] arguments) => new MethodCallExpression(instanceAst, methodInfo, arguments);

        /// <summary>
        /// 调用方法：返回“<paramref name="methodInfo"/>.ReturnType”。。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns>方法调用表达式。</returns>
        public static InvocationExpression Invoke(MethodInfo methodInfo, Expression arguments) => new InvocationExpression(methodInfo, arguments);

        /// <summary>
        /// 调用方法：返回“<paramref name="methodInfo"/>.ReturnType”。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodInfo">方法。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns>方法调用表达式。</returns>
        public static InvocationExpression Invoke(Expression instanceAst, MethodInfo methodInfo, Expression arguments) => new InvocationExpression(instanceAst, methodInfo, arguments);

        /// <summary>
        /// 调用静态方法。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="methodAst">方法表达式。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns>方法调用表达式。</returns>
        public static InvocationExpression Invoke(Expression methodAst, Expression arguments) => new InvocationExpression(methodAst, arguments);

        /// <summary>
        /// 调用方法。<see cref="MethodBase.Invoke(object, object[])"/>
        /// </summary>
        /// <param name="instanceAst">实例。</param>
        /// <param name="methodAst">方法表达式。</param>
        /// <param name="arguments">参数<see cref="object"/>[]。</param>
        /// <returns>方法调用表达式。</returns>
        public static InvocationExpression Invoke(Expression instanceAst, Expression methodAst, Expression arguments) => new InvocationExpression(instanceAst, methodAst, arguments);

        /// <summary>
        /// 循环代码块。
        /// </summary>
        /// <param name="returnType">表达式类型。</param>
        /// <param name="breakLabel">跳出循环标记。</param>
        /// <param name="continueLabel">继续循环标记。</param>
        /// <returns>循环表达式。</returns>
        public static LoopExpression Loop(Type returnType, Label breakLabel, Label continueLabel) => new LoopExpression(returnType, breakLabel, continueLabel);

        /// <summary>
        /// 代码块。
        /// </summary>
        /// <param name="returnType">返回值。</param>
        /// <returns>代码片段。</returns>
        public static BlockExpression Block(Type returnType) => new BlockExpression(returnType);

        /// <summary>
        /// 抛出异常。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        /// <returns>抛异常表达式。</returns>
        public static ThrowExpression Throw(Type exceptionType) => new ThrowExpression(exceptionType);

        /// <summary>
        /// 抛出异常。
        /// </summary>
        /// <param name="exceptionType">异常类型。</param>
        /// <param name="errorMsg">异常消息。</param>
        /// <returns>抛异常表达式。</returns>
        public static ThrowExpression Throw(Type exceptionType, string errorMsg) => new ThrowExpression(exceptionType, errorMsg);

        /// <summary>
        /// 抛出异常。
        /// </summary>
        /// <param name="expression">异常表达式。</param>
        /// <returns>抛异常表达式。</returns>
        public static ThrowExpression Throw(Expression expression) => new ThrowExpression(expression);

        /// <summary>
        /// 异常处理。
        /// </summary>
        /// <param name="returnType">返回值。</param>
        /// <returns>流程控制表达式。</returns>
        public static TryExpression Try(Type returnType) => new TryExpression(returnType);

        /// <summary>
        /// 异常处理。
        /// </summary>
        /// <param name="returnType">返回值。</param>
        /// <param name="finallyAst">一定会执行的代码。</param>
        /// <returns>流程控制表达式。</returns>
        public static TryExpression Try(Type returnType, Expression finallyAst) => new TryExpression(returnType, finallyAst);

        /// <summary>
        /// 字段。
        /// </summary>
        /// <param name="field">字段。</param>
        /// <returns>字段表达式。</returns>
        public static FieldExpression Field(FieldInfo field) => new FieldExpression(field);

        /// <summary>
        /// 属性。
        /// </summary>
        /// <param name="property">属性。</param>
        /// <returns>属性表达式。</returns>
        public static PropertyExpression Property(PropertyInfo property) => new PropertyExpression(property);

        /// <summary>
        /// 参数。
        /// </summary>
        /// <param name="parameter">参数。</param>
        /// <returns>参数表达式。</returns>
        public static ParameterExpression Paramter(ParameterInfo parameter) => new ParameterExpression(parameter);

        /// <summary>
        /// 变量。
        /// </summary>
        /// <param name="variableType">变量类型。</param>
        /// <returns>变量表达式。</returns>
        public static VariableExpression Variable(Type variableType) => new VariableExpression(variableType);

        /// <summary>
        /// 标签。
        /// </summary>
        /// <returns>标签。</returns>
        public static Label Label() => new Label();

        /// <summary>
        /// 标记标签。
        /// </summary>
        /// <param name="label">标签。</param>
        /// <returns>标记标签表达式。</returns>
        public static LabelExpression Label(Label label) => new LabelExpression(label);

        /// <summary>
        /// 跳转到标签。
        /// </summary>
        /// <param name="label">标签。</param>
        /// <returns>跳转到标签表达式。</returns>
        public static GotoExpression Goto(Label label) => new GotoExpression(label);
    }
}
