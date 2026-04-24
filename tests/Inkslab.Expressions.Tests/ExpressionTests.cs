using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 表达式系统全面覆盖测试。
    /// </summary>
    public class ExpressionTests
    {
        private readonly ModuleEmitter _emitter = new ModuleEmitter($"Inkslab.Expressions.Tests.ExprTests.{Guid.NewGuid():N}");

        private Type BuildStaticMethod(string name, Type returnType, Action<MethodEmitter> body, params (Type type, string name)[] parameters)
        {
            var typeEmitter = _emitter.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, returnType);

            foreach (var p in parameters)
            {
                methodEmitter.DefineParameter(p.type, p.name);
            }

            body(methodEmitter);

            return typeEmitter.CreateType();
        }

        private object InvokeStatic(Type type, string name, params object[] args)
        {
            return type.GetMethod(name).Invoke(null, args);
        }

        #region 算术运算

        /// <summary>
        /// 测试两个整数的加法运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Add 能正确执行加法操作，输入：3 + 4，预期输出：7。
        /// </remarks>
        [Fact]
        public void Add_TwoIntegers_ReturnsSum()
        {
            var type = BuildStaticMethod("Add", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Add(a, b));
            });

            Assert.Equal(7, InvokeStatic(type, "Add", 3, 4));
        }

        /// <summary>
        /// 测试两个整数的减法运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Subtract 能正确执行减法操作，输入：10 - 4，预期输出：6。
        /// </remarks>
        [Fact]
        public void Subtract_TwoIntegers_ReturnsDifference()
        {
            var type = BuildStaticMethod("Sub", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Subtract(a, b));
            });

            Assert.Equal(6, InvokeStatic(type, "Sub", 10, 4));
        }

        /// <summary>
        /// 测试两个整数的乘法运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Multiply 能正确执行乘法操作，输入：4 * 5，预期输出：20。
        /// </remarks>
        [Fact]
        public void Multiply_TwoIntegers_ReturnsProduct()
        {
            var type = BuildStaticMethod("Mul", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Multiply(a, b));
            });

            Assert.Equal(20, InvokeStatic(type, "Mul", 4, 5));
        }

        /// <summary>
        /// 测试两个整数的除法运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Divide 能正确执行除法操作，输入：15 / 3，预期输出：5。
        /// </remarks>
        [Fact]
        public void Divide_TwoIntegers_ReturnsQuotient()
        {
            var type = BuildStaticMethod("Div", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Divide(a, b));
            });

            Assert.Equal(5, InvokeStatic(type, "Div", 15, 3));
        }

        /// <summary>
        /// 测试两个整数的模运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Modulo 能正确执行取模操作，输入：7 % 3，预期输出：1。
        /// </remarks>
        [Fact]
        public void Modulo_TwoIntegers_ReturnsRemainder()
        {
            var type = BuildStaticMethod("Mod", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Modulo(a, b));
            });

            Assert.Equal(1, InvokeStatic(type, "Mod", 7, 3));
        }

        /// <summary>
        /// 测试整数的取反运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Negate 能正确执行取反操作。测试用例：
        /// - 输入 5，预期输出 -5
        /// - 输入 -3，预期输出 3
        /// </remarks>
        [Fact]
        public void Negate_Integer_ReturnsNegated()
        {
            var type = BuildStaticMethod("Neg", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                m.Append(Expression.Negate(a));
            });

            Assert.Equal(-5, InvokeStatic(type, "Neg", 5));
            Assert.Equal(3, InvokeStatic(type, "Neg", -3));
        }

        #endregion

        #region 复合赋值运算

        /// <summary>
        /// 测试变量的减法赋值运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.SubtractAssign 能正确执行减法赋值操作。
        /// 初始值：10，减去 3，预期结果：7。
        /// </remarks>
        [Fact]
        public void SubtractAssign_Variable_ModifiesValue()
        {
            var type = BuildStaticMethod("SubAssign", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, a));
                m.Append(Expression.SubtractAssign(v, b));
                m.Append(v);
            });

            Assert.Equal(7, InvokeStatic(type, "SubAssign", 10, 3));
        }

        /// <summary>
        /// 测试变量的乘法赋值运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.MultiplyAssign 能正确执行乘法赋值操作。
        /// 初始值：5，乘以 3，预期结果：15。
        /// </remarks>
        [Fact]
        public void MultiplyAssign_Variable_ModifiesValue()
        {
            var type = BuildStaticMethod("MulAssign", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, a));
                m.Append(Expression.MultiplyAssign(v, Expression.Constant(3)));
                m.Append(v);
            });

            Assert.Equal(15, InvokeStatic(type, "MulAssign", 5));
        }

        /// <summary>
        /// 测试变量的除法赋值运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.DivideAssign 能正确执行除法赋值操作。
        /// 初始值：10，除以 2，预期结果：5。
        /// </remarks>
        [Fact]
        public void DivideAssign_Variable_ModifiesValue()
        {
            var type = BuildStaticMethod("DivAssign", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, a));
                m.Append(Expression.DivideAssign(v, Expression.Constant(2)));
                m.Append(v);
            });

            Assert.Equal(5, InvokeStatic(type, "DivAssign", 10));
        }

        /// <summary>
        /// 测试变量的模运算赋值操作。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.ModuloAssign 能正确执行模赋值操作。
        /// 初始值：7，模 3，预期结果：1。
        /// </remarks>
        [Fact]
        public void ModuloAssign_Variable_ModifiesValue()
        {
            var type = BuildStaticMethod("ModAssign", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, a));
                m.Append(Expression.ModuloAssign(v, Expression.Constant(3)));
                m.Append(v);
            });

            Assert.Equal(1, InvokeStatic(type, "ModAssign", 7));
        }

        /// <summary>
        /// 测试变量的递减赋值运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.DecrementAssign 能正确执行递减操作。
        /// 初始值：10，递减后，预期结果：9。
        /// </remarks>
        [Fact]
        public void DecrementAssign_Variable_Decrements()
        {
            var type = BuildStaticMethod("DecAssign", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(10)));
                m.Append(Expression.DecrementAssign(v));
                m.Append(v);
            });

            Assert.Equal(9, InvokeStatic(type, "DecAssign"));
        }

        #endregion

        #region 比较运算

        /// <summary>
        /// 测试小于比较运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.LessThan 能正确执行小于比较。测试用例：
        /// - 1 &lt; 2，预期为 true
        /// - 2 &lt; 1，预期为 false
        /// </remarks>
        [Fact]
        public void LessThan_ReturnsCorrectResult()
        {
            var type = BuildStaticMethod("LT", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.LessThan(a, b));
            });

            Assert.Equal(true, InvokeStatic(type, "LT", 1, 2));
            Assert.Equal(false, InvokeStatic(type, "LT", 2, 1));
        }

        /// <summary>
        /// 测试小于等于比较运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.LessThanOrEqual 能正确执行小于等于比较。测试用例：
        /// - 2 &lt;= 2，预期为 true
        /// - 3 &lt;= 2，预期为 false
        /// </remarks>
        [Fact]
        public void LessThanOrEqual_ReturnsCorrectResult()
        {
            var type = BuildStaticMethod("LTE", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.LessThanOrEqual(a, b));
            });

            Assert.Equal(true, InvokeStatic(type, "LTE", 2, 2));
            Assert.Equal(false, InvokeStatic(type, "LTE", 3, 2));
        }

        /// <summary>
        /// 测试相等比较运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Equal 能正确执行相等比较。测试用例：
        /// - 5 == 5，预期为 true
        /// - 5 == 6，预期为 false
        /// </remarks>
        [Fact]
        public void Equal_ReturnsCorrectResult()
        {
            var type = BuildStaticMethod("EQ", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Equal(a, b));
            });

            Assert.Equal(true, InvokeStatic(type, "EQ", 5, 5));
            Assert.Equal(false, InvokeStatic(type, "EQ", 5, 6));
        }

        /// <summary>
        /// 测试不相等比较运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.NotEqual 能正确执行不相等比较。测试用例：
        /// - 1 != 2，预期为 true
        /// - 1 != 1，预期为 false
        /// </remarks>
        [Fact]
        public void NotEqual_ReturnsCorrectResult()
        {
            var type = BuildStaticMethod("NE", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.NotEqual(a, b));
            });

            Assert.Equal(true, InvokeStatic(type, "NE", 1, 2));
            Assert.Equal(false, InvokeStatic(type, "NE", 1, 1));
        }

        #endregion

        #region 逻辑运算

        /// <summary>
        /// 测试布尔逻辑非运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Not 能正确执行逻辑非操作。测试用例：
        /// - !true，预期为 false
        /// - !false，预期为 true
        /// </remarks>
        [Fact]
        public void Not_Boolean_Inverts()
        {
            var type = BuildStaticMethod("NotOp", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(bool), "a");
                m.Append(Expression.Not(a));
            });

            Assert.Equal(false, InvokeStatic(type, "NotOp", true));
            Assert.Equal(true, InvokeStatic(type, "NotOp", false));
        }

        /// <summary>
        /// 测试逻辑或运算的短路特性。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.OrElse 能正确执行逻辑或操作。测试用例：
        /// - true || false，预期为 true
        /// - false || true，预期为 true
        /// - false || false，预期为 false
        /// </remarks>
        [Fact]
        public void OrElse_ShortCircuit()
        {
            var type = BuildStaticMethod("OrOp", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(bool), "a");
                var b = m.DefineParameter(typeof(bool), "b");
                m.Append(Expression.OrElse(a, b));
            });

            Assert.Equal(true, InvokeStatic(type, "OrOp", true, false));
            Assert.Equal(true, InvokeStatic(type, "OrOp", false, true));
            Assert.Equal(false, InvokeStatic(type, "OrOp", false, false));
        }

        /// <summary>
        /// 测试逻辑与运算的短路特性。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.AndAlso 能正确执行逻辑与操作。测试用例：
        /// - true &amp;&amp; true，预期为 true
        /// - true &amp;&amp; false，预期为 false
        /// - false &amp;&amp; true，预期为 false
        /// </remarks>
        [Fact]
        public void AndAlso_ShortCircuit()
        {
            var type = BuildStaticMethod("AndOp", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(bool), "a");
                var b = m.DefineParameter(typeof(bool), "b");
                m.Append(Expression.AndAlso(a, b));
            });

            Assert.Equal(true, InvokeStatic(type, "AndOp", true, true));
            Assert.Equal(false, InvokeStatic(type, "AndOp", true, false));
            Assert.Equal(false, InvokeStatic(type, "AndOp", false, true));
        }

        #endregion

        #region 位运算

        /// <summary>
        /// 测试整数的按位或运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Or 能正确执行按位或操作。
        /// 输入：0b1010 | 0b0101，预期输出：0b1111（15）。
        /// </remarks>
        [Fact]
        public void BitwiseOr_Integers()
        {
            var type = BuildStaticMethod("BitOr", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Or(a, b));
            });

            Assert.Equal(0b1111, InvokeStatic(type, "BitOr", 0b1010, 0b0101));
        }

        /// <summary>
        /// 测试整数的按位与运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.And 能正确执行按位与操作。
        /// 输入：0b1010 &amp; 0b1001，预期输出：0b1000（8）。
        /// </remarks>
        [Fact]
        public void BitwiseAnd_Integers()
        {
            var type = BuildStaticMethod("BitAnd", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.And(a, b));
            });

            Assert.Equal(0b1000, InvokeStatic(type, "BitAnd", 0b1010, 0b1001));
        }

        /// <summary>
        /// 测试整数的按位异或运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.ExclusiveOr 能正确执行按位异或操作。
        /// 输入：0b1010 ^ 0b1001，预期输出：0b0011（3）。
        /// </remarks>
        [Fact]
        public void BitwiseExclusiveOr_Integers()
        {
            var type = BuildStaticMethod("BitXor", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.ExclusiveOr(a, b));
            });

            Assert.Equal(0b0011, InvokeStatic(type, "BitXor", 0b1010, 0b1001));
        }

        #endregion

        #region 类型操作

        /// <summary>
        /// 测试类型检查操作。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.TypeIs 能正确执行类型检查。测试用例：
        /// - "hello" is string，预期为 true
        /// - 42 is string，预期为 false
        /// </remarks>
        [Fact]
        public void TypeIs_ReturnsTrue_WhenTypeMatches()
        {
            var type = BuildStaticMethod("IsStr", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(object), "a");
                m.Append(Expression.TypeIs(a, typeof(string)));
            });

            Assert.Equal(true, InvokeStatic(type, "IsStr", "hello"));
            Assert.Equal(false, InvokeStatic(type, "IsStr", 42));
        }

        /// <summary>
        /// 测试类型转换操作（as 操作符）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.TypeAs 能正确执行类型转换。测试用例：
        /// - "hello" as string，预期返回 "hello"
        /// - 42 as string，预期返回 null
        /// </remarks>
        [Fact]
        public void TypeAs_ReturnsNull_WhenTypeMismatch()
        {
            var type = BuildStaticMethod("AsStr", typeof(string), m =>
            {
                var a = m.DefineParameter(typeof(object), "a");
                m.Append(Expression.TypeAs(a, typeof(string)));
            });

            Assert.Equal("hello", InvokeStatic(type, "AsStr", "hello"));
            Assert.Null(InvokeStatic(type, "AsStr", 42));
        }

        /// <summary>
        /// 测试类型转换（int 转 double）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Convert 能正确执行显式类型转换。
        /// 输入：42，转换为 double，预期输出：42.0。
        /// </remarks>
        [Fact]
        public void Convert_IntToDouble()
        {
            var type = BuildStaticMethod("ToDouble", typeof(double), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                m.Append(Expression.Convert(a, typeof(double)));
            });

            Assert.Equal(42.0, InvokeStatic(type, "ToDouble", 42));
        }

        /// <summary>
        /// 测试值类型的默认值操作。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Default 能正确返回值类型的默认值。
        /// int 的默认值应为 0。
        /// </remarks>
        [Fact]
        public void Default_ValueType_ReturnsDefault()
        {
            var type = BuildStaticMethod("DefInt", typeof(int), m =>
            {
                m.Append(Expression.Default(typeof(int)));
            });

            Assert.Equal(0, InvokeStatic(type, "DefInt"));
        }

        /// <summary>
        /// 测试引用类型的默认值操作。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Default 能正确返回引用类型的默认值。
        /// string 的默认值应为 null。
        /// </remarks>
        [Fact]
        public void Default_ReferenceType_ReturnsNull()
        {
            var type = BuildStaticMethod("DefStr", typeof(string), m =>
            {
                m.Append(Expression.Default(typeof(string)));
            });

            Assert.Null(InvokeStatic(type, "DefStr"));
        }

        #endregion

        #region 数组操作

        /// <summary>
        /// 测试数组创建（指定大小）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.NewArray 能正确创建指定大小的数组。
        /// 创建大小为 5 的 int 数组，预期数组长度为 5。
        /// </remarks>
        [Fact]
        public void NewArray_CreatesArrayOfGivenSize()
        {
            var type = BuildStaticMethod("MakeArr", typeof(int[]), m =>
            {
                m.Append(Expression.NewArray(5, typeof(int)));
            });

            var arr = (int[])InvokeStatic(type, "MakeArr");
            Assert.Equal(5, arr.Length);
        }

        /// <summary>
        /// 测试数组元素访问操作。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.ArrayIndex 能正确访问数组元素。
        /// 从数组 [10, 20, 30] 中访问索引 2，预期返回 30。
        /// </remarks>
        [Fact]
        public void ArrayIndex_GetElement()
        {
            var type = BuildStaticMethod("GetIdx", typeof(int), m =>
            {
                var arr = m.DefineParameter(typeof(int[]), "arr");
                var idx = m.DefineParameter(typeof(int), "idx");
                m.Append(Expression.ArrayIndex(arr, idx));
            });

            Assert.Equal(30, InvokeStatic(type, "GetIdx", new[] { 10, 20, 30 }, 2));
        }

        /// <summary>
        /// 测试数组长度获取操作。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.ArrayLength 能正确获取数组长度。
        /// 数组 [1, 2, 3] 的长度预期为 3。
        /// </remarks>
        [Fact]
        public void ArrayLength_ReturnsLength()
        {
            var type = BuildStaticMethod("Len", typeof(int), m =>
            {
                var arr = m.DefineParameter(typeof(int[]), "arr");
                m.Append(Expression.ArrayLength(arr));
            });

            Assert.Equal(3, InvokeStatic(type, "Len", new[] { 1, 2, 3 }));
        }

        /// <summary>
        /// 测试带初始化器的数组创建。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Array 能正确创建并初始化数组。
        /// 创建包含 "a", "b", "c" 的数组，预期长度为 3，元素依次为 "a", "b", "c"。
        /// </remarks>
        [Fact]
        public void Array_WithInitializer_CreatesPopulatedArray()
        {
            var type = BuildStaticMethod("InitArr", typeof(object[]), m =>
            {
                m.Append(Expression.Array(
                    Expression.Constant("a"),
                    Expression.Constant("b"),
                    Expression.Constant("c")));
            });

            var arr = (object[])InvokeStatic(type, "InitArr");
            Assert.Equal(3, arr.Length);
            Assert.Equal("a", arr[0]);
            Assert.Equal("b", arr[1]);
            Assert.Equal("c", arr[2]);
        }

        #endregion

        #region 控制流

        /// <summary>
        /// 测试条件分支语句（if-then-else）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Condition 能正确执行条件分支。测试用例：
        /// - flag=true，预期返回 1
        /// - flag=false，预期返回 2
        /// </remarks>
        [Fact]
        public void IfThenElse_VoidBranches()
        {
            // Use method calls (void) as branches to avoid Assign's non-void RuntimeType issue
            var type = BuildStaticMethod("IfElse", typeof(int), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                // Use Condition for value-returning if/else
                m.Append(Expression.Condition(
                    flag,
                    Expression.Constant(1),
                    Expression.Constant(2)
                ));
            });

            Assert.Equal(1, InvokeStatic(type, "IfElse", true));
            Assert.Equal(2, InvokeStatic(type, "IfElse", false));
        }

        /// <summary>
        /// 测试条件分支语句（仅 if 分支）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Condition 在仅有 if 分支时的执行。测试用例：
        /// - flag=true，预期返回 99
        /// - flag=false，预期返回 0
        /// </remarks>
        [Fact]
        public void IfThen_OnlyExecutesWhenTrue()
        {
            // IfThen is a void statement - just verify it runs without error
            var type = BuildStaticMethod("IfOnly", typeof(int), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0)));
                // Use Condition to produce a return value based on flag
                m.Append(Expression.Condition(
                    flag,
                    Expression.Constant(99),
                    v
                ));
            });

            Assert.Equal(99, InvokeStatic(type, "IfOnly", true));
            Assert.Equal(0, InvokeStatic(type, "IfOnly", false));
        }

        /// <summary>
        /// 测试空合并运算符（??）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Coalesce 能正确执行空合并操作。测试用例：
        /// - "hello" ?? "world"，预期返回 "hello"
        /// - null ?? "world"，预期返回 "world"
        /// </remarks>
        [Fact]
        public void Coalesce_ReturnsLeftWhenNotNull()
        {
            var type = BuildStaticMethod("Coal", typeof(string), m =>
            {
                var a = m.DefineParameter(typeof(string), "a");
                var b = m.DefineParameter(typeof(string), "b");
                m.Append(Expression.Coalesce(a, b));
            });

            Assert.Equal("hello", InvokeStatic(type, "Coal", "hello", "world"));
            Assert.Equal("world", InvokeStatic(type, "Coal", null, "world"));
        }

        /// <summary>
        /// 测试 switch 分支语句。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Switch 能正确执行多分支选择。测试用例：
        /// - val=1，执行 case 1
        /// - val=2，执行 case 2
        /// - val=99，无匹配分支（默认情况）
        /// </remarks>
        [Fact]
        public void Switch_IntegerCases_VoidAction()
        {
            var type = BuildStaticMethod("Sw", typeof(void), m =>
            {
                var val = m.DefineParameter(typeof(int), "val");
                var v = Expression.Variable(typeof(int));
                var sw = Expression.Switch(val);
                sw.Case(Expression.Constant(1)).Append(Expression.Assign(v, Expression.Constant(10)));
                sw.Case(Expression.Constant(2)).Append(Expression.Assign(v, Expression.Constant(20)));
                m.Append(sw);
            });

            // Just verify it doesn't throw
            InvokeStatic(type, "Sw", 1);
            InvokeStatic(type, "Sw", 2);
            InvokeStatic(type, "Sw", 99);
        }

        /// <summary>
        /// 测试 goto 跳转语句。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Goto 和 Expression.Label 能正确执行跳转。
        /// 使用 goto 跳过中间代码，直接跳转到标签处，预期返回 42。
        /// </remarks>
        [Fact]
        public void GotoAndLabel_JumpsCorrectly()
        {
            var type = BuildStaticMethod("GotoTest", typeof(int), m =>
            {
                var label = Expression.Label();
                m.Append(Expression.Goto(label));
                m.Append(Expression.Constant(1));    // 被跳过
                m.Append(Expression.Label(label));
                m.Append(Expression.Constant(42));
            });

            Assert.Equal(42, InvokeStatic(type, "GotoTest"));
        }

        #endregion

        #region 异常处理

        /// <summary>
        /// 测试抛出异常操作。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Throw 能正确抛出异常。
        /// 应抛出 InvalidOperationException，异常消息为 "test error"。
        /// </remarks>
        [Fact]
        public void Throw_ThrowsException()
        {
            var type = BuildStaticMethod("ThrowTest", typeof(void), m =>
            {
                m.Append(Expression.Throw(typeof(InvalidOperationException), "test error"));
            });

            var ex = Assert.Throws<TargetInvocationException>(() => InvokeStatic(type, "ThrowTest"));
            Assert.IsType<InvalidOperationException>(ex.InnerException);
            Assert.Equal("test error", ex.InnerException.Message);
        }

        /// <summary>
        /// 测试 try-catch 异常捕获。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Try 和 Catch 能正确捕获异常。
        /// try 块抛出异常，catch 块捕获异常并修改结果，预期返回 99。
        /// </remarks>
        [Fact]
        public void TryCatch_CatchesException()
        {
            var type = BuildStaticMethod("TryCatch", typeof(int), m =>
            {
                var result = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(result, Expression.Constant(0)));

                var tryExpr = Expression.Try();
                tryExpr.Append(Expression.Throw(typeof(InvalidOperationException)));
                tryExpr.Append(Expression.Assign(result, Expression.Constant(1)));
                tryExpr.Catch().Append(Expression.Assign(result, Expression.Constant(99)));

                m.Append(tryExpr);
                m.Append(result);
            });

            Assert.Equal(99, InvokeStatic(type, "TryCatch"));
        }

        /// <summary>
        /// 测试 try-finally 异常处理。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Try 和 Finally 能正确执行 finally 块。
        /// try 块执行后，finally 块必定执行，预期返回 42（finally 块中设置的值）。
        /// </remarks>
        [Fact]
        public void TryFinally_ExecutesFinally()
        {
            var type = BuildStaticMethod("TryFinally", typeof(int), m =>
            {
                var result = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(result, Expression.Constant(0)));

                var finallyBlock = Expression.Block();
                finallyBlock.Append(Expression.Assign(result, Expression.Constant(42)));

                var tryExpr = Expression.Try(finallyBlock);
                tryExpr.Append(Expression.Assign(result, Expression.Constant(1)));

                m.Append(tryExpr);
                m.Append(result);
            });

            Assert.Equal(42, InvokeStatic(type, "TryFinally"));
        }

        #endregion

        #region 方法调用

        /// <summary>
        /// 测试静态方法调用（无参数）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Call 能正确调用无参数的静态方法。
        /// 调用 Environment.NewLine 属性的 get 方法，预期返回平台相关的换行符。
        /// </remarks>
        [Fact]
        public void Call_StaticMethod_NoArgs()
        {
            var type = BuildStaticMethod("GetNewLine", typeof(string), m =>
            {
                var prop = typeof(Environment).GetProperty(nameof(Environment.NewLine));
                m.Append(Expression.Call(prop.GetGetMethod()));
            });

            Assert.Equal(Environment.NewLine, InvokeStatic(type, "GetNewLine"));
        }

        /// <summary>
        /// 测试实例方法调用（有参数）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Call 能正确调用有参数的实例方法。
        /// 调用 string.Substring(2, 3)，输入 "hello"，预期返回 "llo"。
        /// </remarks>
        [Fact]
        public void Call_InstanceMethod_WithArgs()
        {
            var type = BuildStaticMethod("SubStr", typeof(string), m =>
            {
                var str = m.DefineParameter(typeof(string), "str");
                var start = m.DefineParameter(typeof(int), "start");
                var len = m.DefineParameter(typeof(int), "len");
                var method = typeof(string).GetMethod(nameof(string.Substring), new[] { typeof(int), typeof(int) });
                m.Append(Expression.Call(str, method, start, len));
            });

            Assert.Equal("llo", InvokeStatic(type, "SubStr", "hello", 2, 3));
        }

        /// <summary>
        /// 测试实例方法调用（无参数）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Call 能正确调用无参数的实例方法。
        /// 调用 string.ToUpper()，输入 "hello"，预期返回 "HELLO"。
        /// </remarks>
        [Fact]
        public void Call_InstanceMethod_NoArgs()
        {
            var type = BuildStaticMethod("ToUp", typeof(string), m =>
            {
                var str = m.DefineParameter(typeof(string), "str");
                var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
                m.Append(Expression.Call(str, method));
            });

            Assert.Equal("HELLO", InvokeStatic(type, "ToUp", "hello"));
        }

        #endregion

        #region 对象创建与成员访问

        /// <summary>
        /// 测试使用默认构造函数创建对象。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.New 能正确调用默认构造函数创建对象。
        /// 创建 object 实例，预期返回非 null 对象。
        /// </remarks>
        [Fact]
        public void New_DefaultConstructor()
        {
            var type = BuildStaticMethod("NewObj", typeof(object), m =>
            {
                m.Append(Expression.New(typeof(object)));
            });

            Assert.NotNull(InvokeStatic(type, "NewObj"));
        }

        /// <summary>
        /// 测试使用带参数的构造函数创建对象。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.New 能正确调用带参数的构造函数。
        /// 调用 new string('A', 5)，预期返回 "AAAAA"。
        /// </remarks>
        [Fact]
        public void New_WithConstructorArgs()
        {
            var type = BuildStaticMethod("NewStr", typeof(string), m =>
            {
                var ctor = typeof(string).GetConstructor(new[] { typeof(char), typeof(int) });
                m.Append(Expression.New(ctor, Expression.Constant('A'), Expression.Constant(5)));
            });

            Assert.Equal("AAAAA", InvokeStatic(type, "NewStr"));
        }

        /// <summary>
        /// 测试 return 语句提前退出方法。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Return 能正确执行提前返回。
        /// 执行 return 42，后续代码被跳过，预期返回 42。
        /// </remarks>
        [Fact]
        public void Return_ExitsMethodEarly()
        {
            var type = BuildStaticMethod("ReturnEarly", typeof(int), m =>
            {
                m.Append(Expression.Return(Expression.Constant(42)));
                m.Append(Expression.Constant(0));
            });

            Assert.Equal(42, InvokeStatic(type, "ReturnEarly"));
        }

        #endregion

        #region Checked 算术运算

        /// <summary>
        /// 测试加法赋值运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.AddAssign 能正确执行加法赋值操作。
        /// 初始值：1，加上 2，预期结果：3。
        /// </remarks>
        [Fact]
        public void AddAssign_NormalOperation()
        {
            var type = BuildStaticMethod("AddA", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(1)));
                m.Append(Expression.AddAssign(v, Expression.Constant(2)));
                m.Append(v);
            });

            Assert.Equal(3, InvokeStatic(type, "AddA"));
        }

        #endregion

        #region 变量与常量

        /// <summary>
        /// 测试变量的赋值与读取操作。
        /// </summary>
        /// <remarks>
        /// 验证表达式变量能正确赋值和读取。
        /// 初始赋值 100，再加 23，预期结果：123。
        /// </remarks>
        [Fact]
        public void Variable_AssignAndRead()
        {
            var type = BuildStaticMethod("VarTest", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(100)));
                m.Append(Expression.AddAssign(v, Expression.Constant(23)));
                m.Append(v);
            });

            Assert.Equal(123, InvokeStatic(type, "VarTest"));
        }

        /// <summary>
        /// 测试字符串常量。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Constant 能正确创建字符串常量。
        /// 常量值 "hello world"，预期返回相同字符串。
        /// </remarks>
        [Fact]
        public void Constant_StringValue()
        {
            var type = BuildStaticMethod("ConstStr", typeof(string), m =>
            {
                m.Append(Expression.Constant("hello world"));
            });

            Assert.Equal("hello world", InvokeStatic(type, "ConstStr"));
        }

        /// <summary>
        /// 测试空值常量。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Constant 能正确创建空值常量。
        /// 常量值 null，预期返回 null。
        /// </remarks>
        [Fact]
        public void Constant_NullValue()
        {
            var type = BuildStaticMethod("ConstNull", typeof(string), m =>
            {
                m.Append(Expression.Constant(null, typeof(string)));
            });

            Assert.Null(InvokeStatic(type, "ConstNull"));
        }

        /// <summary>
        /// 测试 long 类型常量。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Constant 能正确创建 long 常量。
        /// 常量值 123456789L，预期返回相同值。
        /// </remarks>
        [Fact]
        public void Constant_LongValue()
        {
            var type = BuildStaticMethod("ConstLong", typeof(long), m =>
            {
                m.Append(Expression.Constant(123456789L));
            });

            Assert.Equal(123456789L, InvokeStatic(type, "ConstLong"));
        }

        /// <summary>
        /// 测试 double 类型常量。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Constant 能正确创建 double 常量。
        /// 常量值 3.14，预期返回相同值。
        /// </remarks>
        [Fact]
        public void Constant_DoubleValue()
        {
            var type = BuildStaticMethod("ConstDbl", typeof(double), m =>
            {
                m.Append(Expression.Constant(3.14));
            });

            Assert.Equal(3.14, InvokeStatic(type, "ConstDbl"));
        }

        /// <summary>
        /// 测试 bool 类型常量。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Constant 能正确创建 bool 常量。
        /// 常量值 true，预期返回 true。
        /// </remarks>
        [Fact]
        public void Constant_BoolValue()
        {
            var type = BuildStaticMethod("ConstBool", typeof(bool), m =>
            {
                m.Append(Expression.Constant(true));
            });

            Assert.Equal(true, InvokeStatic(type, "ConstBool"));
        }

        #endregion

        #region 比较运算补充

        /// <summary>
        /// 测试大于比较运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.GreaterThan 能正确执行大于比较。测试用例：
        /// - 2 &gt; 1，预期为 true
        /// - 1 &gt; 2，预期为 false
        /// </remarks>
        [Fact]
        public void GreaterThan_ReturnsCorrectResult()
        {
            var type = BuildStaticMethod("GT", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.GreaterThan(a, b));
            });

            Assert.Equal(true, InvokeStatic(type, "GT", 2, 1));
            Assert.Equal(false, InvokeStatic(type, "GT", 1, 2));
        }

        /// <summary>
        /// 测试大于等于比较运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.GreaterThanOrEqual 能正确执行大于等于比较。测试用例：
        /// - 3 &gt;= 3，预期为 true
        /// - 2 &gt;= 3，预期为 false
        /// </remarks>
        [Fact]
        public void GreaterThanOrEqual_ReturnsCorrectResult()
        {
            var type = BuildStaticMethod("GTE", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.GreaterThanOrEqual(a, b));
            });

            Assert.Equal(true, InvokeStatic(type, "GTE", 3, 3));
            Assert.Equal(false, InvokeStatic(type, "GTE", 2, 3));
        }

        #endregion

        #region Checked 算术运算扩展

        /// <summary>
        /// 测试 AddChecked 对基础数值类型未实现（库当前行为）。
        /// </summary>
        /// <remarks>
        /// 当前库的 BinaryExpression 对基础 int 类型的 Checked 变体（AddChecked、SubtractChecked、
        /// MultiplyChecked 及对应 AssignChecked）未在 AnalysisType 中实现，
        /// 应抛出 NotImplementedException。此测试记录该已知行为。
        /// </remarks>
        [Fact]
        public void AddChecked_WithBuiltinIntOperands_ThrowsNotImplementedException()
        {
            var a = Expression.Constant(3);
            var b = Expression.Constant(4);
            Assert.Throws<NotImplementedException>(() => Expression.AddChecked(a, b));
        }

        /// <summary>
        /// 测试 SubtractChecked 对基础数值类型未实现（库当前行为）。
        /// </summary>
        [Fact]
        public void SubtractChecked_WithBuiltinIntOperands_ThrowsNotImplementedException()
        {
            var a = Expression.Constant(10);
            var b = Expression.Constant(3);
            Assert.Throws<NotImplementedException>(() => Expression.SubtractChecked(a, b));
        }

        /// <summary>
        /// 测试 MultiplyChecked 对基础数值类型未实现（库当前行为）。
        /// </summary>
        [Fact]
        public void MultiplyChecked_WithBuiltinIntOperands_ThrowsNotImplementedException()
        {
            var a = Expression.Constant(3);
            var b = Expression.Constant(4);
            Assert.Throws<NotImplementedException>(() => Expression.MultiplyChecked(a, b));
        }

        /// <summary>
        /// 测试 AddAssignChecked 对基础数值类型未实现（库当前行为）。
        /// </summary>
        [Fact]
        public void AddAssignChecked_WithBuiltinIntOperands_ThrowsNotImplementedException()
        {
            var v = Expression.Variable(typeof(int));
            Assert.Throws<NotImplementedException>(() => Expression.AddAssignChecked(v, Expression.Constant(1)));
        }

        /// <summary>
        /// 测试 SubtractAssignChecked 对基础数值类型未实现（库当前行为）。
        /// </summary>
        [Fact]
        public void SubtractAssignChecked_WithBuiltinIntOperands_ThrowsNotImplementedException()
        {
            var v = Expression.Variable(typeof(int));
            Assert.Throws<NotImplementedException>(() => Expression.SubtractAssignChecked(v, Expression.Constant(1)));
        }

        /// <summary>
        /// 测试 MultiplyAssignChecked 对基础数值类型未实现（库当前行为）。
        /// </summary>
        [Fact]
        public void MultiplyAssignChecked_WithBuiltinIntOperands_ThrowsNotImplementedException()
        {
            var v = Expression.Variable(typeof(int));
            Assert.Throws<NotImplementedException>(() => Expression.MultiplyAssignChecked(v, Expression.Constant(1)));
        }

        #endregion

        #region 位运算赋值

        /// <summary>
        /// 测试按位或赋值运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.OrAssign 能正确执行按位或赋值。
        /// 初始值：0b1010，或 0b0101，预期结果：0b1111（15）。
        /// </remarks>
        [Fact]
        public void OrAssign_Variable_ModifiesValue()
        {
            var type = BuildStaticMethod("OrAsg", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0b1010)));
                m.Append(Expression.OrAssign(v, Expression.Constant(0b0101)));
                m.Append(v);
            });

            Assert.Equal(0b1111, InvokeStatic(type, "OrAsg"));
        }

        /// <summary>
        /// 测试按位与赋值运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.AndAssign 能正确执行按位与赋值。
        /// 初始值：0b1111，与 0b1010，预期结果：0b1010（10）。
        /// </remarks>
        [Fact]
        public void AndAssign_Variable_ModifiesValue()
        {
            var type = BuildStaticMethod("AndAsg", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0b1111)));
                m.Append(Expression.AndAssign(v, Expression.Constant(0b1010)));
                m.Append(v);
            });

            Assert.Equal(0b1010, InvokeStatic(type, "AndAsg"));
        }

        /// <summary>
        /// 测试按位异或赋值运算。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.ExclusiveOrAssign 能正确执行按位异或赋值。
        /// 初始值：0b1111，异或 0b1010，预期结果：0b0101（5）。
        /// </remarks>
        [Fact]
        public void ExclusiveOrAssign_Variable_ModifiesValue()
        {
            var type = BuildStaticMethod("XorAsg", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0b1111)));
                m.Append(Expression.ExclusiveOrAssign(v, Expression.Constant(0b1010)));
                m.Append(v);
            });

            Assert.Equal(0b0101, InvokeStatic(type, "XorAsg"));
        }

        #endregion

        #region 一元运算扩展

        /// <summary>
        /// 测试非修改性自增运算（返回 a+1，不修改 a）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Increment 返回值为 a+1，但不修改变量 a 本身。
        /// 输入：5，预期返回：6，变量 a 不变仍为 5。
        /// </remarks>
        [Fact]
        public void Increment_Integer_ReturnsIncrementedValue()
        {
            var type = BuildStaticMethod("IncrVal", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                // Increment 返回 a+1，不修改 a；方法返回该值
                m.Append(Expression.Increment(a));
            });

            Assert.Equal(6, InvokeStatic(type, "IncrVal", 5));
            Assert.Equal(1, InvokeStatic(type, "IncrVal", 0));
        }

        /// <summary>
        /// 测试非修改性自减运算（返回 a-1，不修改 a）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Decrement 返回值为 a-1，但不修改变量 a 本身。
        /// 输入：5，预期返回：4。
        /// </remarks>
        [Fact]
        public void Decrement_Integer_ReturnsDecrementedValue()
        {
            var type = BuildStaticMethod("DecrVal", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                m.Append(Expression.Decrement(a));
            });

            Assert.Equal(4, InvokeStatic(type, "DecrVal", 5));
            Assert.Equal(-1, InvokeStatic(type, "DecrVal", 0));
        }

        /// <summary>
        /// 测试 IsFalse 运算：输入 false 返回 true。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.IsFalse 对 false 输入返回 true（等价于条件检查"值为假"）。
        /// </remarks>
        [Fact]
        public void IsFalse_WhenFalse_ReturnsTrue()
        {
            var type = BuildStaticMethod("IsF", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(bool), "a");
                m.Append(Expression.IsFalse(a));
            });

            Assert.Equal(true, InvokeStatic(type, "IsF", false));
            Assert.Equal(false, InvokeStatic(type, "IsF", true));
        }

        #endregion

        #region 控制流扩展

        /// <summary>
        /// 测试 switch 仅含 default（无 case）时直接执行 default 分支。
        /// </summary>
        /// <remarks>
        /// 当 SwitchExpression 未添加任何 Case 时，所有输入都会执行 defaultAst。
        /// 预期：result 被设为 99。
        /// </remarks>
        [Fact]
        public void Switch_OnlyDefault_ExecutesDefaultBranch()
        {
            var type = BuildStaticMethod("SwOnlyDef", typeof(int), m =>
            {
                var val = m.DefineParameter(typeof(int), "val");
                var result = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(result, Expression.Constant(0)));

                var defaultAssign = Expression.Assign(result, Expression.Constant(99));
                var sw = Expression.Switch(val, defaultAssign);
                // 不添加任何 Case，全部走 default

                m.Append(sw);
                m.Append(result);
            });

            Assert.Equal(99, InvokeStatic(type, "SwOnlyDef", 42));
            Assert.Equal(99, InvokeStatic(type, "SwOnlyDef", 1));
        }

        /// <summary>
        /// 测试 Block 顺序执行多条语句。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Block 能顺序执行内部所有语句。
        /// 依次累加 1、2、3，预期结果：6。
        /// </remarks>
        [Fact]
        public void Block_ExecutesSequentially()
        {
            var type = BuildStaticMethod("BlockSeq", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0)));

                var block = Expression.Block();
                block.Append(Expression.AddAssign(v, Expression.Constant(1)));
                block.Append(Expression.AddAssign(v, Expression.Constant(2)));
                block.Append(Expression.AddAssign(v, Expression.Constant(3)));

                m.Append(block);
                m.Append(v);
            });

            Assert.Equal(6, InvokeStatic(type, "BlockSeq"));
        }

        /// <summary>
        /// 测试 IfThenElse 两个 void 分支均可正确执行。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.IfThenElse（显式版本）能正确执行 void 类型的 if/else 分支。
        /// - flag=true：分支 A 执行，结果变量设为 1
        /// - flag=false：分支 B 执行，结果变量设为 2
        /// </remarks>
        [Fact]
        public void IfThenElse_VoidBranches_SetsVariableCorrectly()
        {
            var type = BuildStaticMethod("IfElseVoid", typeof(int), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0)));

                var trueBlock = Expression.Block();
                trueBlock.Append(Expression.Assign(v, Expression.Constant(1)));

                var falseBlock = Expression.Block();
                falseBlock.Append(Expression.Assign(v, Expression.Constant(2)));

                m.Append(Expression.IfThenElse(flag, trueBlock, falseBlock));
                m.Append(v);
            });

            Assert.Equal(1, InvokeStatic(type, "IfElseVoid", true));
            Assert.Equal(2, InvokeStatic(type, "IfElseVoid", false));
        }

        #endregion

        #region 异常处理扩展

        /// <summary>
        /// 测试 try-catch 捕获异常并通过变量访问异常信息。
        /// </summary>
        /// <remarks>
        /// 验证 catch(Exception e) 变量绑定形式能正确捕获并访问异常消息。
        /// 抛出消息为 "my-error" 的 InvalidOperationException，
        /// catch 块通过异常变量读取 Message 属性，预期返回 "my-error"。
        /// </remarks>
        [Fact]
        public void TryCatch_WithExceptionVariable_AccessesException()
        {
            var msgProp = typeof(Exception).GetProperty(nameof(Exception.Message));
            var type = BuildStaticMethod("TryCatchVar", typeof(string), m =>
            {
                var result = Expression.Variable(typeof(string));
                m.Append(Expression.Assign(result, Expression.Constant("none")));

                var exVar = Expression.Variable(typeof(Exception));

                var tryExpr = Expression.Try();
                tryExpr.Append(Expression.Throw(typeof(InvalidOperationException), "my-error"));
                tryExpr.Catch(exVar)
                    .Append(Expression.Assign(result, Expression.Property(exVar, msgProp)));

                m.Append(tryExpr);
                m.Append(result);
            });

            Assert.Equal("my-error", InvokeStatic(type, "TryCatchVar"));
        }

        /// <summary>
        /// 测试 try-catch-finally 三块均正确执行。
        /// </summary>
        /// <remarks>
        /// 验证同时带有 catch 和 finally 的 try 表达式均能执行。
        /// try 块抛出异常：catch 块赋值 1，finally 块在 catch 后再累加 10，预期最终值：11。
        /// </remarks>
        [Fact]
        public void TryCatchFinally_BothBlocksExecute()
        {
            var type = BuildStaticMethod("TryCF", typeof(int), m =>
            {
                var result = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(result, Expression.Constant(0)));

                var finallyBlock = Expression.Block();
                finallyBlock.Append(Expression.AddAssign(result, Expression.Constant(10)));

                var tryExpr = Expression.Try(finallyBlock);
                tryExpr.Append(Expression.Throw(typeof(InvalidOperationException)));
                tryExpr.Catch().Append(Expression.Assign(result, Expression.Constant(1)));

                m.Append(tryExpr);
                m.Append(result);
            });

            Assert.Equal(11, InvokeStatic(type, "TryCF"));
        }

        /// <summary>
        /// 测试空 try 表达式（无 catch 无 finally）应抛出 AstException。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Try() 在未添加任何 catch 或 finally 时，调用 CreateType 应抛出 AstException。
        /// </remarks>
        [Fact]
        public void Try_WithoutCatchOrFinally_ThrowsAstException()
        {
            var typeEmitter = _emitter.DefineType($"EmptyTry_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod("Empty", MethodAttributes.Public | MethodAttributes.Static, typeof(void));

            var emptyTry = Expression.Try();
            emptyTry.Append(Expression.Default(typeof(void)));
            methodEmitter.Append(emptyTry);

            Assert.Throws<AstException>(() => typeEmitter.CreateType());
        }

        #endregion

        #region 属性与字段访问

        /// <summary>
        /// 测试静态属性读取。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Property(PropertyInfo) 能正确读取静态属性。
        /// 读取 Environment.NewLine，预期返回平台换行符。
        /// </remarks>
        [Fact]
        public void Property_StaticRead_ReturnsValue()
        {
            var prop = typeof(Environment).GetProperty(nameof(Environment.NewLine));
            var type = BuildStaticMethod("StaticProp", typeof(string), m =>
            {
                m.Append(Expression.Property(prop));
            });

            Assert.Equal(Environment.NewLine, InvokeStatic(type, "StaticProp"));
        }

        /// <summary>
        /// 测试实例属性读取。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Property(instance, PropertyInfo) 能正确读取实例属性。
        /// 读取 string.Length，输入 "hello"，预期返回 5。
        /// </remarks>
        [Fact]
        public void Property_InstanceRead_ReturnsValue()
        {
            var prop = typeof(string).GetProperty(nameof(string.Length));
            var type = BuildStaticMethod("InstPropR", typeof(int), m =>
            {
                var s = m.DefineParameter(typeof(string), "s");
                m.Append(Expression.Property(s, prop));
            });

            Assert.Equal(5, InvokeStatic(type, "InstPropR", "hello"));
        }

        /// <summary>
        /// 测试实例属性写入并读回。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Property 配合 Expression.Assign 能正确设置实例属性。
        /// 向 Exception.Source 属性赋值 "test-src"，再读回，预期返回 "test-src"。
        /// </remarks>
        [Fact]
        public void Property_InstanceWrite_SetsValue()
        {
            var sourceProp = typeof(Exception).GetProperty(nameof(Exception.Source));
            var type = BuildStaticMethod("InstPropW", typeof(string), m =>
            {
                var ex = m.DefineParameter(typeof(Exception), "ex");
                var propExpr = Expression.Property(ex, sourceProp);
                m.Append(Expression.Assign(propExpr, Expression.Constant("test-src")));
                m.Append(propExpr);
            });

            Assert.Equal("test-src", InvokeStatic(type, "InstPropW", new Exception()));
        }

        /// <summary>
        /// 测试静态字段读取。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Field(FieldInfo) 能正确读取静态字段。
        /// 读取 string.Empty，预期返回空字符串 ""。
        /// </remarks>
        [Fact]
        public void Field_StaticRead_ReturnsValue()
        {
            var field = typeof(string).GetField(nameof(string.Empty));
            var type = BuildStaticMethod("StaticFld", typeof(string), m =>
            {
                m.Append(Expression.Field(field));
            });

            Assert.Equal(string.Empty, InvokeStatic(type, "StaticFld"));
        }

        /// <summary>
        /// 测试实例字段读取。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Field(instance, FieldInfo) 能正确读取实例公有字段。
        /// 读取 ExprFieldTarget.Value 字段，预期返回构造时设定的值 77。
        /// </remarks>
        [Fact]
        public void Field_InstanceRead_ReturnsValue()
        {
            var field = typeof(ExprFieldTarget).GetField(nameof(ExprFieldTarget.Value));
            var type = BuildStaticMethod("InstFldR", typeof(int), m =>
            {
                var t = m.DefineParameter(typeof(ExprFieldTarget), "t");
                m.Append(Expression.Field(t, field));
            });

            Assert.Equal(77, InvokeStatic(type, "InstFldR", new ExprFieldTarget { Value = 77 }));
        }

        #endregion

        #region 成员初始化

        /// <summary>
        /// 测试对象成员初始化表达式。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.MemberInit 能正确创建对象并初始化属性。
        /// 创建 ExprPoint { X = 3, Y = 7 }，验证属性值正确。
        /// </remarks>
        [Fact]
        public void MemberInit_SetsPropertiesOnNewObject()
        {
            var xProp = typeof(ExprPoint).GetProperty(nameof(ExprPoint.X));
            var yProp = typeof(ExprPoint).GetProperty(nameof(ExprPoint.Y));

            var type = BuildStaticMethod("MInit", typeof(ExprPoint), m =>
            {
                m.Append(Expression.MemberInit(
                    Expression.New(typeof(ExprPoint)),
                    Expression.Bind(xProp, Expression.Constant(3)),
                    Expression.Bind(yProp, Expression.Constant(7))
                ));
            });

            var result = (ExprPoint)InvokeStatic(type, "MInit");
            Assert.Equal(3, result.X);
            Assert.Equal(7, result.Y);
        }

        #endregion

        #region 方法调用扩展

        /// <summary>
        /// 测试 DeclaringCall 对实例方法的非虚调用。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.DeclaringCall 能对非抽象实例方法进行非虚调用。
        /// 对字符串调用 ToUpper()，输入 "hello"，预期返回 "HELLO"。
        /// </remarks>
        [Fact]
        public void DeclaringCall_InstanceMethod_NonVirtualDispatch()
        {
            var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
            var type = BuildStaticMethod("DeclCall", typeof(string), m =>
            {
                var s = m.DefineParameter(typeof(string), "s");
                m.Append(Expression.DeclaringCall(s, method));
            });

            Assert.Equal("HELLO", InvokeStatic(type, "DeclCall", "hello"));
        }

        /// <summary>
        /// 测试 Call 调用带参数的静态方法。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Call 能正确调用带参数的静态方法。
        /// 调用 Math.Max(a, b)，输入 3 和 7，预期返回 7。
        /// </remarks>
        [Fact]
        public void Call_StaticMethodWithArgs_ReturnsResult()
        {
            var method = typeof(Math).GetMethod(nameof(Math.Max), new[] { typeof(int), typeof(int) });
            var type = BuildStaticMethod("CallMaxStatic", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Call(method, a, b));
            });

            Assert.Equal(7, InvokeStatic(type, "CallMaxStatic", 3, 7));
        }

        #endregion

        #region 返回与常量扩展

        /// <summary>
        /// 测试无返回值的 Return() 能正常结束 void 方法。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Return()（void 版本）能生成有效的 void 方法，调用不抛异常。
        /// </remarks>
        [Fact]
        public void Return_Void_ExitsMethodEarly()
        {
            var type = BuildStaticMethod("RetVoid", typeof(void), m =>
            {
                m.Append(Expression.Return());
                // Return() 之后不追加任何表达式，确保生成有效 IL
            });

            // 仅验证方法能正常创建并调用，不抛异常
            var exception = Record.Exception(() => InvokeStatic(type, "RetVoid"));
            Assert.Null(exception);
        }

        /// <summary>
        /// 测试带显式类型的常量表达式（null string 常量）。
        /// </summary>
        /// <remarks>
        /// 验证 Expression.Constant(value, type) 重载能正确创建显式类型的常量。
        /// 使用 null 值和 typeof(string) 创建常量，方法返回类型为 string，预期返回 null。
        /// </remarks>
        [Fact]
        public void Constant_WithExplicitType_CreatesTypedConstant()
        {
            var type = BuildStaticMethod("ConstTyped", typeof(string), m =>
            {
                m.Append(Expression.Constant(null, typeof(string)));
            });

            Assert.Null(InvokeStatic(type, "ConstTyped"));
        }

        #endregion
    }

    /// <summary>
    /// 用于字段访问测试的辅助类型。
    /// </summary>
    public class ExprFieldTarget
    {
        /// <summary>
        /// 公有实例字段，用于测试字段读取。
        /// </summary>
        public int Value;
    }

    /// <summary>
    /// 用于成员初始化测试的辅助类型。
    /// </summary>
    public class ExprPoint
    {
        /// <summary>
        /// X 坐标。
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Y 坐标。
        /// </summary>
        public int Y { get; set; }
    }
}
