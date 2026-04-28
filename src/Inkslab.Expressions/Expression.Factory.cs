using Inkslab.Emitters;
using Inkslab.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inkslab
{
    /// <summary>
    /// <see cref="Expression"/> 的静态工厂方法集合。
    /// 拆分自原 Expression.cs 以提升可读性和 IDE 跳转效率，不改变任何 API 表面。
    /// </summary>
    public abstract partial class Expression
    {
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
        public static NewExpression New(Type instanceType) => new NewExpression(instanceType);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        /// <returns>创建实例表达式。</returns>
        public static NewExpression New(ConstructorInfo constructor) => new NewExpression(constructor);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="instanceType">实例类型。</param>
        /// <param name="parameters">参数。</param>
        /// <returns>创建实例表达式。</returns>
        public static NewExpression New(Type instanceType, params Expression[] parameters) => new NewExpression(instanceType, parameters);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="constructor">构造函数。</param>
        /// <param name="parameters">参数。</param>
        /// <returns>创建实例表达式。</returns>
        public static NewExpression New(ConstructorInfo constructor, params Expression[] parameters) => new NewExpression(constructor, parameters);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="constructorEmitter">构造函数。</param>
        /// <returns>创建实例表达式。</returns>
        public static Expression New(ConstructorEmitter constructorEmitter) => new NewInstanceEmitter(constructorEmitter);

        /// <summary>
        /// 创建实例。
        /// </summary>
        /// <param name="constructorEmitter">构造函数。</param>
        /// <param name="parameters">参数。</param>
        /// <returns>创建实例表达式。</returns>
        public static Expression New(ConstructorEmitter constructorEmitter, params Expression[] parameters) => new NewInstanceEmitter(constructorEmitter, parameters);

        /// <summary>
        /// 绑定成员。
        /// </summary>
        /// <param name="member">成员。</param>
        /// <param name="expression">表达式。</param>
        public static MemberAssignment Bind(MemberInfo member, Expression expression)
        {
            if (member is null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            ValidateSettableFieldOrPropertyMember(member, out Type memberType);

            if (!memberType.IsAssignableFrom(expression.RuntimeType))
            {
                throw new NotSupportedException($"表达式无法对类型为“{memberType.Name}”的成员赋值！");
            }

            return new MemberAssignment(member, expression);
        }

        private static void ValidateSettableFieldOrPropertyMember(MemberInfo member, out Type memberType)
        {
            switch (member)
            {
                case PropertyInfo propertyInfo:

                    if (!propertyInfo.CanWrite)
                    {
                        throw new NotSupportedException($"“{propertyInfo.DeclaringType.Name}.{propertyInfo.Name}”属性不可写！");
                    }

                    memberType = propertyInfo.PropertyType;

                    break;
                case FieldInfo fieldInfo:

                    memberType = fieldInfo.FieldType;

                    break;
                default:
                    throw new NotSupportedException($"成员类型“{member.GetType().Name}”不支持绑定，仅支持字段与可写属性！");
            }
        }

        /// <summary>
        /// 初始化成员。
        /// </summary>
        /// <param name="newExpression">创建实列表达式。</param>
        /// <param name="bindings">初始化成员。</param>
        public static MemberInitExpression MemberInit(NewExpression newExpression, params MemberAssignment[] bindings) => MemberInit(newExpression, (IEnumerable<MemberAssignment>)bindings);

        /// <summary>
        /// 初始化成员。
        /// </summary>
        /// <param name="newExpression">创建实列表达式。</param>
        /// <param name="bindings">初始化成员。</param>
        public static MemberInitExpression MemberInit(NewExpression newExpression, IEnumerable<MemberAssignment> bindings)
        {
            if (newExpression is null)
            {
                throw new ArgumentNullException(nameof(newExpression));
            }

            var assignments = new List<MemberAssignment>(bindings ?? Enumerable.Empty<MemberAssignment>());

            ValidateMemberInitArgs(newExpression.RuntimeType, assignments);

            return new MemberInitExpression(newExpression, assignments);
        }

        private static void ValidateMemberInitArgs(Type type, List<MemberAssignment> bindings)
        {
            for (int i = 0, n = bindings.Count; i < n; i++)
            {
                MemberAssignment b = bindings[i] ?? throw new ArgumentNullException();

                if (!b.Member.DeclaringType.IsAssignableFrom(type))
                {
                    throw new MissingMemberException(type.Name, b.Member.Name);
                }
            }
        }

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
        public static BinaryExpression Add(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Add, right);

        /// <summary>加(检查溢出)。</summary>
        public static BinaryExpression AddChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AddChecked, right);

        /// <summary>加等于。</summary>
        public static BinaryExpression AddAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AddAssign, right);

        /// <summary>加等于(检查溢出)。</summary>
        public static BinaryExpression AddAssignChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AddAssignChecked, right);

        /// <summary>减。</summary>
        public static BinaryExpression Subtract(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Subtract, right);

        /// <summary>减(检查溢出)。</summary>
        public static BinaryExpression SubtractChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.SubtractChecked, right);

        /// <summary>减等于。</summary>
        public static BinaryExpression SubtractAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.SubtractAssign, right);

        /// <summary>减等于(检查溢出)。</summary>
        public static BinaryExpression SubtractAssignChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.SubtractAssignChecked, right);

        /// <summary>乘。</summary>
        public static BinaryExpression Multiply(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Multiply, right);

        /// <summary>乘（检查溢出）。</summary>
        public static BinaryExpression MultiplyChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.MultiplyChecked, right);

        /// <summary>乘等于。</summary>
        public static BinaryExpression MultiplyAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.MultiplyAssign, right);

        /// <summary>乘等于（检查溢出）。</summary>
        public static BinaryExpression MultiplyAssignChecked(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.MultiplyAssignChecked, right);

        /// <summary>除。</summary>
        public static BinaryExpression Divide(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Divide, right);

        /// <summary>除等于。</summary>
        public static BinaryExpression DivideAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.DivideAssign, right);

        /// <summary>取模。</summary>
        public static BinaryExpression Modulo(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Modulo, right);

        /// <summary>取模等于。</summary>
        public static BinaryExpression ModuloAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.ModuloAssign, right);

        /// <summary>小于。</summary>
        public static BinaryExpression LessThan(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.LessThan, right);

        /// <summary>小于等于。</summary>
        public static BinaryExpression LessThanOrEqual(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.LessThanOrEqual, right);

        /// <summary>位运算：或。</summary>
        public static BinaryExpression Or(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Or, right);

        /// <summary>位运算：或等于。</summary>
        public static BinaryExpression OrAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.OrAssign, right);

        /// <summary>位运算：且。</summary>
        public static BinaryExpression And(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.And, right);

        /// <summary>位运算：且等于。</summary>
        public static BinaryExpression AndAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AndAssign, right);

        /// <summary>位运算：异或。</summary>
        public static BinaryExpression ExclusiveOr(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.ExclusiveOr, right);

        /// <summary>位运算：异或等于。</summary>
        public static BinaryExpression ExclusiveOrAssign(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.ExclusiveOrAssign, right);

        /// <summary>等于。</summary>
        public static BinaryExpression Equal(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.Equal, right);

        /// <summary>大于等于。</summary>
        public static BinaryExpression GreaterThanOrEqual(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.GreaterThanOrEqual, right);

        /// <summary>大于。</summary>
        public static BinaryExpression GreaterThan(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.GreaterThan, right);

        /// <summary>不等于。</summary>
        public static BinaryExpression NotEqual(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.NotEqual, right);

        /// <summary>或。</summary>
        public static BinaryExpression OrElse(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.OrElse, right);

        /// <summary>且。</summary>
        public static BinaryExpression AndAlso(Expression left, Expression right) => new BinaryExpression(left, BinaryExpressionType.AndAlso, right);

        /// <summary>按位补运算或逻辑反运算。</summary>
        public static UnaryExpression Not(Expression body) => new UnaryExpression(body, UnaryExpressionType.Not);

        /// <summary>是否为假。</summary>
        public static UnaryExpression IsFalse(Expression body) => new UnaryExpression(body, UnaryExpressionType.IsFalse);

        /// <summary>增量(i + 1)。</summary>
        public static UnaryExpression Increment(Expression body) => new UnaryExpression(body, UnaryExpressionType.Increment);

        /// <summary>减量(i - 1)。</summary>
        public static UnaryExpression Decrement(Expression body) => new UnaryExpression(body, UnaryExpressionType.Decrement);

        /// <summary>递增(i += 1)。</summary>
        public static UnaryExpression IncrementAssign(Expression body) => new UnaryExpression(body, UnaryExpressionType.IncrementAssign);

        /// <summary>递减(i -= 1)。</summary>
        public static UnaryExpression DecrementAssign(Expression body) => new UnaryExpression(body, UnaryExpressionType.DecrementAssign);

        /// <summary>正负反转。</summary>
        public static UnaryExpression Negate(Expression body) => new UnaryExpression(body, UnaryExpressionType.Negate);

        /// <summary>流程。</summary>
        public static SwitchExpression Switch(Expression switchValue) => new SwitchExpression(switchValue);

        /// <summary>流程。</summary>
        public static SwitchExpression Switch(Expression switchValue, Expression defaultAst) => new SwitchExpression(switchValue, defaultAst);

        /// <summary>条件判断。</summary>
        public static IfThenExpression IfThen(Expression test, Expression ifTrue) => new IfThenExpression(test, ifTrue);

        /// <summary>条件判断。</summary>
        public static IfThenElseExpression IfThenElse(Expression test, Expression ifTrue, Expression ifFalse) => new IfThenElseExpression(test, ifTrue, ifFalse);

        /// <summary>三目运算。</summary>
        public static ConditionExpression Condition(Expression test, Expression ifTrue, Expression ifFalse) => new ConditionExpression(test, ifTrue, ifFalse);

        /// <summary>三目运算。</summary>
        public static ConditionExpression Condition(Expression test, Expression ifTrue, Expression ifFalse, Type returnType) => new ConditionExpression(test, ifTrue, ifFalse, returnType);

        /// <summary>调用方法。</summary>
        public static Expression Call(MethodInfo methodInfo) => new MethodCallExpression(methodInfo);

        /// <summary>调用方法。</summary>
        public static Expression Call(MethodInfo methodInfo, params Expression[] arguments) => new MethodCallExpression(methodInfo, arguments);

        /// <summary>调用方法。</summary>
        public static Expression Call(Expression instanceAst, MethodInfo methodInfo) => new MethodCallExpression(instanceAst, methodInfo);

        /// <summary>调用方法。</summary>
        public static Expression Call(Expression instanceAst, MethodInfo methodInfo, params Expression[] arguments) => new MethodCallExpression(instanceAst, methodInfo, arguments);

        /// <summary>调用方法。</summary>
        public static Expression Call(MethodEmitter methodEmitter) => new MethodCallEmitter(methodEmitter);

        /// <summary>调用方法。</summary>
        public static Expression Call(MethodEmitter methodEmitter, params Expression[] arguments) => new MethodCallEmitter(methodEmitter, arguments);

        /// <summary>
        /// 调用方法（若方法 <paramref name="methodInfo"/> 不为抽象方法，则直接调用方法，而不是被重写的方法；否则，调用重写方法。）。
        /// </summary>
        public static Expression DeclaringCall(Expression instanceAst, MethodInfo methodInfo) => new MethodCallExpression(instanceAst, methodInfo) { VirtualCall = methodInfo.DeclaringType.IsInterface || (methodInfo.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract };

        /// <summary>
        /// 调用方法（若方法 <paramref name="methodInfo"/> 不为抽象方法，则直接调用方法，而不是被重写的方法；否则，调用重写方法。）。
        /// </summary>
        public static Expression DeclaringCall(Expression instanceAst, MethodInfo methodInfo, params Expression[] arguments) => new MethodCallExpression(instanceAst, methodInfo, arguments) { VirtualCall = methodInfo.DeclaringType.IsInterface || (methodInfo.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract };

        /// <summary>调用方法。<see cref="MethodBase.Invoke(object, object[])"/></summary>
        public static Expression Invoke(MethodInfo methodInfo, Expression arguments) => new InvocationExpression(methodInfo, arguments);

        /// <summary>调用方法。<see cref="MethodBase.Invoke(object, object[])"/></summary>
        public static Expression Invoke(Expression instanceAst, MethodInfo methodInfo, Expression arguments) => new InvocationExpression(instanceAst, methodInfo, arguments);

        /// <summary>调用静态方法。<see cref="MethodBase.Invoke(object, object[])"/></summary>
        public static Expression Invoke(MethodEmitter methodEmitter, Expression arguments) => new InvocationEmitter(methodEmitter, arguments);

        /// <summary>调用方法。<see cref="MethodBase.Invoke(object, object[])"/></summary>
        public static Expression Invoke(Expression instanceAst, MethodEmitter methodEmitter, Expression arguments) => new InvocationEmitter(instanceAst, methodEmitter, arguments);

        /// <summary>调用静态方法。<see cref="MethodBase.Invoke(object, object[])"/></summary>
        public static Expression Invoke(Expression methodAst, Expression arguments) => new InvocationExpression(methodAst, arguments);

        /// <summary>调用方法。<see cref="MethodBase.Invoke(object, object[])"/></summary>
        public static Expression Invoke(Expression instanceAst, Expression methodAst, Expression arguments) => new InvocationExpression(instanceAst, methodAst, arguments);

        /// <summary>循环代码块。</summary>
        public static LoopExpression Loop() => new LoopExpression();

        /// <summary>
        /// 元素遍历循环代码块（合并 <c>for</c> 与 <c>foreach</c> 语义）。
        /// 优先使用 <c>for</c> 索引循环（一维数组、或具备 <c>int Count</c>/<c>int Length</c> 与 <c>this[int]</c>
        /// 的索引集合，且元素类型与 <paramref name="item"/> 完全一致）；否则使用 <c>foreach</c> 枚举器循环：
        /// 优先选择元素类型匹配的 <see cref="System.Collections.Generic.IEnumerable{T}"/>，仅当源类型只实现非泛型
        /// <see cref="System.Collections.IEnumerable"/> 时才退回并对当前元素进行类型强转，强转无法进行时抛出
        /// <see cref="AstException"/>。
        /// </summary>
        /// <param name="item">循环变量。</param>
        /// <param name="source">迭代源表达式。</param>
        public static ForEachExpression ForEach(VariableExpression item, Expression source) => new ForEachExpression(item, source);

        /// <summary>代码块。</summary>
        public static BlockExpression Block() => new BlockExpression();

        /// <summary>抛出异常。</summary>
        public static ThrowExpression Throw(Type exceptionType) => new ThrowExpression(exceptionType);

        /// <summary>抛出异常。</summary>
        public static ThrowExpression Throw(Type exceptionType, string errorMsg) => new ThrowExpression(exceptionType, errorMsg);

        /// <summary>抛出异常。</summary>
        public static ThrowExpression Throw(Expression expression) => new ThrowExpression(expression);

        /// <summary>异常处理。</summary>
        public static TryExpression Try() => new TryExpression();

        /// <summary>异常处理。</summary>
        public static TryExpression Try(Expression finallyAst) => new TryExpression(finallyAst);

        /// <summary>字段。</summary>
        public static FieldExpression Field(FieldInfo field) => new FieldExpression(field);

        /// <summary>字段。</summary>
        public static FieldExpression Field(Expression instanceAst, FieldInfo field) => new FieldExpression(instanceAst, field);

        /// <summary>属性。</summary>
        public static PropertyExpression Property(PropertyInfo property) => new PropertyExpression(property);

        /// <summary>属性。</summary>
        public static PropertyExpression Property(Expression instanceAst, PropertyInfo property) => new PropertyExpression(instanceAst, property);

        /// <summary>参数。</summary>
        public static ParameterExpression Paramter(ParameterInfo parameter) => new ParameterExpression(parameter);

        /// <summary>变量。</summary>
        public static VariableExpression Variable(Type variableType) => new VariableExpression(variableType);

        /// <summary>跳转标签。</summary>
        public static Label Label() => new Label(LabelKind.Goto);

        /// <summary>标记标签。</summary>
        public static LabelExpression Label(Label label) => new LabelExpression(label);

        /// <summary>跳转到标签。</summary>
        public static GotoExpression Goto(Label label) => new GotoExpression(label);

        /// <summary>跳出封闭式循环。</summary>
        public static BreakExpression Break() => new BreakExpression();

        /// <summary>继续封闭式循环。</summary>
        public static ContinueExpression Continue() => new ContinueExpression();

        /// <summary>结束方法（无返回值）。</summary>
        public static ReturnExpression Return() => new ReturnExpression();

        /// <summary>结束方法（返回数据）。</summary>
        public static ReturnExpression Return(Expression body) => new ReturnExpression(body);
    }
}
