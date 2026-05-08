using System;
using System.Reflection;
using Inkslab.Emitters;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 强行验证以下场景明确不支持，必须抛 <see cref="AstException"/>：
    /// 1. 移位运算（<c>&lt;&lt;</c>、<c>&gt;&gt;</c>）右侧必须严格为 <see cref="int"/>，不接受其他整数类型；左侧必须为整数；
    /// 2. <see cref="Expression.Power"/> 仅支持同类型 <see cref="double"/>（NETSTANDARD2_1+ 还支持 <see cref="float"/>）；
    /// 3. 复合赋值不应用数值提升（保持左侧可写）—— 异类型必须抛；
    /// 4. 数值提升不覆盖 enum + 整数算术/位运算（仅比较走 <c>IsEnumIntegerCompatible</c>）。
    /// </summary>
    public class UnsupportedOperationsTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"Unsup_{Guid.NewGuid():N}");

        public enum Color { A, B, C }

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        // ========== 1. 移位运算右侧必须严格为 int ==========

        [Theory]
        [InlineData(typeof(long))]
        [InlineData(typeof(short))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(ulong))]
        public void LeftShift_RightSideNotInt_Throws(Type rightType)
        {
            var ex = Assert.Throws<AstException>(() =>
            {
                BuildStatic($"LSh_{rightType.Name}", typeof(int), m =>
                {
                    var a = m.DefineParameter(typeof(int), "a");
                    var b = m.DefineParameter(rightType, "b");
                    m.Append(Expression.LeftShift(a, b));
                });
            });
            Assert.Contains("int", ex.Message);
        }

        [Theory]
        [InlineData(typeof(long))]
        [InlineData(typeof(short))]
        [InlineData(typeof(byte))]
        public void RightShift_RightSideNotInt_Throws(Type rightType)
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic($"RSh_{rightType.Name}", typeof(int), m =>
                {
                    var a = m.DefineParameter(typeof(int), "a");
                    var b = m.DefineParameter(rightType, "b");
                    m.Append(Expression.RightShift(a, b));
                });
            });
        }

        [Fact]
        public void LeftShift_LeftSideNonInteger_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("LShDb", typeof(double), m =>
                {
                    var a = m.DefineParameter(typeof(double), "a");
                    var b = m.DefineParameter(typeof(int), "b");
                    m.Append(Expression.LeftShift(a, b));
                });
            });
        }

        [Fact]
        public void LeftShift_LeftSideBool_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("LShBl", typeof(bool), m =>
                {
                    var a = m.DefineParameter(typeof(bool), "a");
                    var b = m.DefineParameter(typeof(int), "b");
                    m.Append(Expression.LeftShift(a, b));
                });
            });
        }

        [Fact]
        public void RightShift_LeftSideFloat_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("RShFl", typeof(float), m =>
                {
                    var a = m.DefineParameter(typeof(float), "a");
                    var b = m.DefineParameter(typeof(int), "b");
                    m.Append(Expression.RightShift(a, b));
                });
            });
        }

        // ========== 2. Power 仅支持同类型 double / float ==========

        [Fact]
        public void Power_IntAndInt_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("PowII", typeof(int), m =>
                {
                    var a = m.DefineParameter(typeof(int), "a");
                    var b = m.DefineParameter(typeof(int), "b");
                    m.Append(Expression.Power(a, b));
                });
            });
        }

        [Fact]
        public void Power_IntAndDouble_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("PowID", typeof(double), m =>
                {
                    var a = m.DefineParameter(typeof(int), "a");
                    var b = m.DefineParameter(typeof(double), "b");
                    m.Append(Expression.Power(a, b));
                });
            });
        }

        [Fact]
        public void Power_DoubleAndInt_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("PowDI", typeof(double), m =>
                {
                    var a = m.DefineParameter(typeof(double), "a");
                    var b = m.DefineParameter(typeof(int), "b");
                    m.Append(Expression.Power(a, b));
                });
            });
        }

        [Fact]
        public void Power_FloatAndDouble_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("PowFD", typeof(double), m =>
                {
                    var a = m.DefineParameter(typeof(float), "a");
                    var b = m.DefineParameter(typeof(double), "b");
                    m.Append(Expression.Power(a, b));
                });
            });
        }

        [Fact]
        public void Power_LongAndLong_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("PowLL", typeof(long), m =>
                {
                    var a = m.DefineParameter(typeof(long), "a");
                    var b = m.DefineParameter(typeof(long), "b");
                    m.Append(Expression.Power(a, b));
                });
            });
        }

        [Fact]
        public void Power_DecimalAndDecimal_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("PowDD", typeof(decimal), m =>
                {
                    var a = m.DefineParameter(typeof(decimal), "a");
                    var b = m.DefineParameter(typeof(decimal), "b");
                    m.Append(Expression.Power(a, b));
                });
            });
        }

        // ========== 3. 复合赋值：右侧能隐式提升到左侧时合法（仅展示反例；正向用例见 NumericPromotionTests） ==========

        [Fact]
        public void AddAssign_IntAndLong_Throws()
        {
            // int += long: long 无法隐式转 int → C# 也是编译错误
            var ex = Assert.Throws<AstException>(() =>
            {
                BuildStatic("AAIL", typeof(int), m =>
                {
                    var v = Expression.Variable(typeof(int));
                    m.Append(Expression.Assign(v, Expression.Constant(0)));
                    m.Append(Expression.AddAssign(v, Expression.Constant(1L)));
                    m.Append(v);
                });
            });
            Assert.Contains("不支持", ex.Message);
        }

        [Fact]
        public void AddAssign_ByteAndInt_Throws()
        {
            // C# 中 byte += int 是编译错误（int 无法隐式转 byte）。本框架同样拒绝。
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("AABI", typeof(byte), m =>
                {
                    var v = Expression.Variable(typeof(byte));
                    m.Append(Expression.Assign(v, Expression.Constant((byte)0)));
                    m.Append(Expression.AddAssign(v, Expression.Constant(1)));
                    m.Append(v);
                });
            });
        }

        [Fact]
        public void AddAssign_UintAndInt_Throws()
        {
            // C# 中 uint += int 是编译错误（int 无法隐式转 uint）。本框架同样拒绝。
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("AAUI", typeof(uint), m =>
                {
                    var v = Expression.Variable(typeof(uint));
                    m.Append(Expression.Assign(v, Expression.Constant((uint)0)));
                    m.Append(Expression.AddAssign(v, Expression.Constant(1)));
                    m.Append(v);
                });
            });
        }

        [Fact]
        public void SubtractAssign_FloatAndDouble_Throws()
        {
            // float -= double: double 无法隐式转 float → 编译错误
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("SAFD", typeof(float), m =>
                {
                    var v = Expression.Variable(typeof(float));
                    m.Append(Expression.Assign(v, Expression.Constant(0f)));
                    m.Append(Expression.SubtractAssign(v, Expression.Constant(1.0)));
                    m.Append(v);
                });
            });
        }

        [Fact]
        public void ExclusiveOrAssign_ShortAndInt_Throws()
        {
            // short ^= int: int 无法隐式转 short → 编译错误
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("XAS", typeof(short), m =>
                {
                    var v = Expression.Variable(typeof(short));
                    m.Append(Expression.Assign(v, Expression.Constant((short)0)));
                    m.Append(Expression.ExclusiveOrAssign(v, Expression.Constant(1)));
                    m.Append(v);
                });
            });
        }

        // ========== 4. enum 算术 / 位运算不应用提升 ==========

        [Fact]
        public void Add_EnumAndUnderlying_Throws()
        {
            // C# 中 MyEnum + int 合法（结果是 MyEnum）；本框架仅在比较中支持 enum vs underlying。
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("EnAdd", typeof(Color), m =>
                {
                    var a = m.DefineParameter(typeof(Color), "a");
                    var b = m.DefineParameter(typeof(int), "b");
                    m.Append(Expression.Add(a, b));
                });
            });
        }

        [Fact]
        public void Subtract_UnderlyingAndEnum_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("EnSub", typeof(Color), m =>
                {
                    var a = m.DefineParameter(typeof(int), "a");
                    var b = m.DefineParameter(typeof(Color), "b");
                    m.Append(Expression.Subtract(a, b));
                });
            });
        }

        [Fact]
        public void Or_EnumAndUnderlying_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("EnOr", typeof(Color), m =>
                {
                    var a = m.DefineParameter(typeof(Color), "a");
                    var b = m.DefineParameter(typeof(int), "b");
                    m.Append(Expression.Or(a, b));
                });
            });
        }

        [Fact]
        public void And_EnumAndUnderlying_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("EnAnd", typeof(Color), m =>
                {
                    var a = m.DefineParameter(typeof(int), "a");
                    var b = m.DefineParameter(typeof(Color), "b");
                    m.Append(Expression.And(a, b));
                });
            });
        }
    }
}
