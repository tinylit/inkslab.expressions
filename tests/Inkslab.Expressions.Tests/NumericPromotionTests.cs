using System;
using System.Reflection;
using Inkslab.Emitters;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 测试 C# 二元数值提升：算术与比较中，左右数值类型不同时按 spec 提升到公共类型。
    /// 也覆盖 byte/sbyte 与更宽类型混用、可空异底层提升。
    /// </summary>
    public class NumericPromotionTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"NumProm_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, params object[] args)
            => t.GetMethod("Run")!.Invoke(null, args);

        // ========== 算术：异类型 ==========

        [Fact]
        public void Add_IntAndLong_PromotesToLong()
        {
            var type = BuildStatic("AddIL", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.Add(a, b));
            });
            Assert.Equal(3_000_000_001L, (long)Invoke(type, 1, 3_000_000_000L));
        }

        [Fact]
        public void Add_IntAndDouble_PromotesToDouble()
        {
            var type = BuildStatic("AddID", typeof(double), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(double), "b");
                m.Append(Expression.Add(a, b));
            });
            Assert.Equal(4.5, (double)Invoke(type, 2, 2.5));
        }

        [Fact]
        public void Multiply_FloatAndDouble_PromotesToDouble()
        {
            var type = BuildStatic("MulFD", typeof(double), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(double), "b");
                m.Append(Expression.Multiply(a, b));
            });
            Assert.Equal(7.5, (double)Invoke(type, 1.5f, 5.0));
        }

        [Fact]
        public void Add_ByteAndByte_PromotesToInt()
        {
            // C# 中 byte + byte 也提升为 int
            var type = BuildStatic("AddBB", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(byte), "a");
                var b = m.DefineParameter(typeof(byte), "b");
                m.Append(Expression.Add(a, b));
            });
            Assert.Equal(7, (int)Invoke(type, (byte)3, (byte)4));
        }

        [Fact]
        public void Add_ShortAndInt_PromotesToInt()
        {
            var type = BuildStatic("AddSI", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(short), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Add(a, b));
            });
            Assert.Equal(15, (int)Invoke(type, (short)5, 10));
        }

        [Fact]
        public void Add_UintAndInt_PromotesToLong()
        {
            // uint + int → long（避免符号信息丢失）
            var type = BuildStatic("AddUI", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(uint), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Add(a, b));
            });
            Assert.Equal(150L, (long)Invoke(type, (uint)100, 50));
        }

        [Fact]
        public void Subtract_LongAndInt_PromotesToLong()
        {
            var type = BuildStatic("SubLI", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(long), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Subtract(a, b));
            });
            Assert.Equal(99L, (long)Invoke(type, 100L, 1));
        }

        // ========== 比较：异类型 ==========

        [Fact]
        public void Equal_IntAndLong_PromotesAndCompares()
        {
            var type = BuildStatic("EqIL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, 100, 100L));
            Assert.False((bool)Invoke(type, 100, 101L));
        }

        [Fact]
        public void LessThan_ShortAndInt()
        {
            var type = BuildStatic("LtSI", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(short), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.LessThan(a, b));
            });
            Assert.True((bool)Invoke(type, (short)5, 10));
            Assert.False((bool)Invoke(type, (short)10, 5));
        }

        [Fact]
        public void GreaterThan_DoubleAndInt()
        {
            var type = BuildStatic("GtDI", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(double), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.GreaterThan(a, b));
            });
            Assert.True((bool)Invoke(type, 3.5, 3));
            Assert.False((bool)Invoke(type, 2.5, 3));
        }

        [Fact]
        public void NotEqual_FloatAndDouble()
        {
            var type = BuildStatic("NEqFD", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(double), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            // 1.5f → 1.5 严格等
            Assert.False((bool)Invoke(type, 1.5f, 1.5));
            Assert.True((bool)Invoke(type, 1.5f, 2.0));
        }

        [Fact]
        public void Equal_ByteAndInt()
        {
            var type = BuildStatic("EqBI", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(byte), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, (byte)42, 42));
            Assert.False((bool)Invoke(type, (byte)42, 43));
        }

        // ========== 可空异底层数值 ==========

        [Fact]
        public void Equal_NullableIntAndLong_PromotesUnderlying()
        {
            var type = BuildStatic("EqNILng", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, 100, 100L));
            Assert.False((bool)Invoke(type, 100, 101L));
            Assert.False((bool)Invoke(type, null, 100L));
        }

        [Fact]
        public void Equal_NullableIntAndNullableLong()
        {
            var type = BuildStatic("EqNIL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int?), "a");
                var b = m.DefineParameter(typeof(long?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, 42, 42L));
            Assert.False((bool)Invoke(type, 42, 43L));
            Assert.True((bool)Invoke(type, null, null));
            Assert.False((bool)Invoke(type, 42, null));
            Assert.False((bool)Invoke(type, null, 42L));
        }

        [Fact]
        public void Equal_NullableShortAndNullableInt()
        {
            var type = BuildStatic("EqNSI", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(short?), "a");
                var b = m.DefineParameter(typeof(int?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, (short)5, 5));
            Assert.False((bool)Invoke(type, (short)5, 6));
            Assert.True((bool)Invoke(type, null, null));
        }

        [Fact]
        public void NotEqual_NullableDoubleAndInt()
        {
            var type = BuildStatic("NEqNDI", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(double?), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.False((bool)Invoke(type, 3.0, 3));
            Assert.True((bool)Invoke(type, 3.5, 3));
            Assert.True((bool)Invoke(type, null, 3));
        }

        // ========== ulong 与有符号整数：C# 编译错误，应回退到失败 ==========

        [Fact]
        public void Add_UlongAndLong_NotPromoted_ThrowsAstException()
        {
            // ulong + long 在 C# 中是编译错误 —— 我们也应拒绝
            var ex = Assert.Throws<AstException>(() =>
            {
                BuildStatic("AddULL", typeof(ulong), m =>
                {
                    var a = m.DefineParameter(typeof(ulong), "a");
                    var b = m.DefineParameter(typeof(long), "b");
                    m.Append(Expression.Add(a, b));
                });
            });
            Assert.Contains("不支持", ex.Message);
        }

        [Fact]
        public void Add_UlongAndUint_PromotesToUlong()
        {
            var type = BuildStatic("AddULU", typeof(ulong), m =>
            {
                var a = m.DefineParameter(typeof(ulong), "a");
                var b = m.DefineParameter(typeof(uint), "b");
                m.Append(Expression.Add(a, b));
            });
            Assert.Equal(150UL, (ulong)Invoke(type, 100UL, (uint)50));
        }

        // ========== 位运算：异类型 ==========

        [Fact]
        public void Or_ByteAndInt_PromotesToInt()
        {
            var type = BuildStatic("OrBI", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(byte), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Or(a, b));
            });
            Assert.Equal(0x0F | 0xF0, (int)Invoke(type, (byte)0x0F, 0xF0));
        }

        [Fact]
        public void And_ShortAndLong_PromotesToLong()
        {
            var type = BuildStatic("AndSL", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(short), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.And(a, b));
            });
            Assert.Equal(0x00FFL & 0x0F0FL, (long)Invoke(type, (short)0x00FF, 0x0F0FL));
        }

        [Fact]
        public void ExclusiveOr_IntAndUint_PromotesToLong()
        {
            // int ^ uint → long（uint 与有符号 int 混用）
            var type = BuildStatic("XorIU", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(uint), "b");
                m.Append(Expression.ExclusiveOr(a, b));
            });
            Assert.Equal(0xAAL ^ 0x55L, (long)Invoke(type, 0xAA, (uint)0x55));
        }

        [Fact]
        public void Or_ByteAndByte_FastPathReturnsByte()
        {
            // 同类型快速路径：byte | byte → byte（IsIntegerOrBool 接受 byte，跳过提升）。
            // 这是位运算分支的固有行为，与 byte + byte → int（算术分支无 byte 快速路径）不同。
            var type = BuildStatic("OrBB", typeof(byte), m =>
            {
                var a = m.DefineParameter(typeof(byte), "a");
                var b = m.DefineParameter(typeof(byte), "b");
                m.Append(Expression.Or(a, b));
            });
            Assert.Equal((byte)0xFF, (byte)Invoke(type, (byte)0x0F, (byte)0xF0));
        }

        [Fact]
        public void Or_BoolAndInt_NotPromoted_ThrowsAstException()
        {
            // bool 不参与数值提升
            var ex = Assert.Throws<AstException>(() =>
            {
                BuildStatic("OrBoolI", typeof(int), m =>
                {
                    var a = m.DefineParameter(typeof(bool), "a");
                    var b = m.DefineParameter(typeof(int), "b");
                    m.Append(Expression.Or(a, b));
                });
            });
            Assert.Contains("不支持", ex.Message);
        }

        [Fact]
        public void And_FloatAndInt_NotPromoted_ThrowsAstException()
        {
            // 浮点不允许位运算
            var ex = Assert.Throws<AstException>(() =>
            {
                BuildStatic("AndFI", typeof(int), m =>
                {
                    var a = m.DefineParameter(typeof(float), "a");
                    var b = m.DefineParameter(typeof(int), "b");
                    m.Append(Expression.And(a, b));
                });
            });
            Assert.Contains("不支持", ex.Message);
        }

        // ========== 复合赋值：相同类型仍可工作（回归覆盖） ==========

        [Fact]
        public void AddAssign_IntAndInt_StillWorks()
        {
            var type = BuildStatic("AddAII", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(10)));
                m.Append(Expression.AddAssign(v, Expression.Constant(5)));
                m.Append(v);
            });
            Assert.Equal(15, (int)Invoke(type));
        }

        [Fact]
        public void OrAssign_IntAndInt_StillWorks()
        {
            var type = BuildStatic("OrAII", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0x0F)));
                m.Append(Expression.OrAssign(v, Expression.Constant(0xF0)));
                m.Append(v);
            });
            Assert.Equal(0xFF, (int)Invoke(type));
        }

        // ========== 复合赋值：异类型不应应用提升（保持左侧可写） ==========

        [Fact]
        public void AddAssign_IntAndLong_RejectsCleanly()
        {
            // int += long 在 C# 中是编译错误（结果 long 无法隐式回写 int）。
            // 框架应清晰拒绝（AstException），而非崩溃。
            var ex = Assert.Throws<AstException>(() =>
            {
                BuildStatic("AddAIL", typeof(int), m =>
                {
                    var v = Expression.Variable(typeof(int));
                    m.Append(Expression.Assign(v, Expression.Constant(10)));
                    m.Append(Expression.AddAssign(v, Expression.Constant(5L)));
                    m.Append(v);
                });
            });
            Assert.Contains("不支持", ex.Message);
        }

        [Fact]
        public void OrAssign_IntAndLong_RejectsCleanly()
        {
            var ex = Assert.Throws<AstException>(() =>
            {
                BuildStatic("OrAIL", typeof(int), m =>
                {
                    var v = Expression.Variable(typeof(int));
                    m.Append(Expression.Assign(v, Expression.Constant(0)));
                    m.Append(Expression.OrAssign(v, Expression.Constant(1L)));
                    m.Append(v);
                });
            });
            Assert.Contains("不支持", ex.Message);
        }

        // ========== 除法/取模提升 ==========

        [Fact]
        public void Divide_LongAndInt_PromotesToLong()
        {
            var type = BuildStatic("DivLI", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(long), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Divide(a, b));
            });
            Assert.Equal(50L, (long)Invoke(type, 100L, 2));
        }

        [Fact]
        public void Divide_IntAndDouble_PromotesToDouble()
        {
            var type = BuildStatic("DivID", typeof(double), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(double), "b");
                m.Append(Expression.Divide(a, b));
            });
            Assert.Equal(2.5, (double)Invoke(type, 5, 2.0));
        }

        [Fact]
        public void Modulo_LongAndInt_PromotesToLong()
        {
            var type = BuildStatic("ModLI", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(long), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Modulo(a, b));
            });
            Assert.Equal(1L, (long)Invoke(type, 10L, 3));
        }

        [Fact]
        public void Modulo_DoubleAndInt_PromotesToDouble()
        {
            var type = BuildStatic("ModDI", typeof(double), m =>
            {
                var a = m.DefineParameter(typeof(double), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Modulo(a, b));
            });
            Assert.Equal(0.5, (double)Invoke(type, 10.5, 2));
        }

        // ========== 关系比较：更多组合 ==========

        [Fact]
        public void GreaterThanOrEqual_ByteAndInt()
        {
            var type = BuildStatic("GeBI", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(byte), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.GreaterThanOrEqual(a, b));
            });
            Assert.True((bool)Invoke(type, (byte)5, 5));
            Assert.True((bool)Invoke(type, (byte)10, 5));
            Assert.False((bool)Invoke(type, (byte)3, 5));
        }

        [Fact]
        public void LessThanOrEqual_ShortAndLong()
        {
            var type = BuildStatic("LeSL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(short), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.LessThanOrEqual(a, b));
            });
            Assert.True((bool)Invoke(type, (short)5, 5L));
            Assert.True((bool)Invoke(type, (short)3, 10L));
            Assert.False((bool)Invoke(type, (short)10, 5L));
        }

        [Fact]
        public void LessThan_UintAndLong_PromotesToLong()
        {
            var type = BuildStatic("LtUL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(uint), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.LessThan(a, b));
            });
            Assert.True((bool)Invoke(type, (uint)100, 200L));
            Assert.False((bool)Invoke(type, (uint)200, 100L));
        }

        [Fact]
        public void Equal_SbyteAndShort()
        {
            var type = BuildStatic("EqSbS", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(sbyte), "a");
                var b = m.DefineParameter(typeof(short), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, (sbyte)42, (short)42));
            Assert.False((bool)Invoke(type, (sbyte)42, (short)43));
            Assert.True((bool)Invoke(type, (sbyte)-1, (short)-1));
        }

        // ========== 算术：补充更多组合 ==========

        [Fact]
        public void Subtract_ShortAndInt_NegativeResult()
        {
            // short(5) - int(10) → -5（验证带符号语义）
            var type = BuildStatic("SubSI", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(short), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Subtract(a, b));
            });
            Assert.Equal(-5, (int)Invoke(type, (short)5, 10));
        }

        [Fact]
        public void Multiply_ByteAndLong_PromotesToLong()
        {
            var type = BuildStatic("MulBL", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(byte), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.Multiply(a, b));
            });
            Assert.Equal(1_000_000L, (long)Invoke(type, (byte)10, 100_000L));
        }

        [Fact]
        public void Add_DoubleAndLong_PromotesToDouble()
        {
            var type = BuildStatic("AddDL", typeof(double), m =>
            {
                var a = m.DefineParameter(typeof(double), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.Add(a, b));
            });
            Assert.Equal(1.5 + 100, (double)Invoke(type, 1.5, 100L));
        }

        [Fact]
        public void Add_SbyteAndUshort_PromotesToInt()
        {
            // sbyte + ushort 都是窄类型，按规则提升到 int
            var type = BuildStatic("AddSbUs", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(sbyte), "a");
                var b = m.DefineParameter(typeof(ushort), "b");
                m.Append(Expression.Add(a, b));
            });
            Assert.Equal(105, (int)Invoke(type, (sbyte)5, (ushort)100));
        }

        // ========== 可空：补充更多组合 ==========

        [Fact]
        public void Equal_NullableByteAndShort_PromotesUnderlying()
        {
            var type = BuildStatic("EqNBS", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(byte?), "a");
                var b = m.DefineParameter(typeof(short), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, (byte)42, (short)42));
            Assert.False((bool)Invoke(type, (byte)42, (short)43));
            Assert.False((bool)Invoke(type, null, (short)42));
        }

        [Fact]
        public void NotEqual_IntAndNullableByte()
        {
            var type = BuildStatic("NEqINB", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(byte?), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.False((bool)Invoke(type, 5, (byte)5));
            Assert.True((bool)Invoke(type, 5, (byte)6));
            Assert.True((bool)Invoke(type, 5, null));
        }

        [Fact]
        public void Equal_NullableSbyteAndNullableUshort_PromotesToInt()
        {
            var type = BuildStatic("EqNSU", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(sbyte?), "a");
                var b = m.DefineParameter(typeof(ushort?), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(type, (sbyte)42, (ushort)42));
            Assert.False((bool)Invoke(type, (sbyte)42, (ushort)43));
            Assert.True((bool)Invoke(type, null, null));
            Assert.False((bool)Invoke(type, (sbyte)42, null));
        }

        // ========== 位运算：补充更多组合 ==========

        [Fact]
        public void And_ByteAndLong_PromotesToLong()
        {
            var type = BuildStatic("AndBL", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(byte), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.And(a, b));
            });
            Assert.Equal(0xFFL & 0x0F0FL, (long)Invoke(type, (byte)0xFF, 0x0F0FL));
        }

        [Fact]
        public void ExclusiveOr_ShortAndInt_PromotesToInt()
        {
            var type = BuildStatic("XorSI", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(short), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.ExclusiveOr(a, b));
            });
            Assert.Equal(0x00FF ^ 0x0F0F, (int)Invoke(type, (short)0x00FF, 0x0F0F));
        }

        [Fact]
        public void Or_SbyteAndInt_PromotesToInt()
        {
            var type = BuildStatic("OrSbI", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(sbyte), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Or(a, b));
            });
            Assert.Equal(0x0F | 0x10, (int)Invoke(type, (sbyte)0x0F, 0x10));
        }

        [Fact]
        public void And_UintAndLong_PromotesToLong()
        {
            var type = BuildStatic("AndUL", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(uint), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.And(a, b));
            });
            Assert.Equal(0xFFFFL & 0x0F0FL, (long)Invoke(type, (uint)0xFFFF, 0x0F0FL));
        }

        [Fact]
        public void ExclusiveOr_ByteAndUshort_PromotesToInt()
        {
            var type = BuildStatic("XorBU", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(byte), "a");
                var b = m.DefineParameter(typeof(ushort), "b");
                m.Append(Expression.ExclusiveOr(a, b));
            });
            Assert.Equal(0xFF ^ 0xFFFF, (int)Invoke(type, (byte)0xFF, (ushort)0xFFFF));
        }

        // ========== 复合赋值同类型回归（覆盖各种 *Assign 变体） ==========

        [Fact]
        public void SubtractAssign_IntAndInt_StillWorks()
        {
            var type = BuildStatic("SAII", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(10)));
                m.Append(Expression.SubtractAssign(v, Expression.Constant(3)));
                m.Append(v);
            });
            Assert.Equal(7, (int)Invoke(type));
        }

        [Fact]
        public void MultiplyAssign_DoubleAndDouble_StillWorks()
        {
            var type = BuildStatic("MADD", typeof(double), m =>
            {
                var v = Expression.Variable(typeof(double));
                m.Append(Expression.Assign(v, Expression.Constant(2.5)));
                m.Append(Expression.MultiplyAssign(v, Expression.Constant(4.0)));
                m.Append(v);
            });
            Assert.Equal(10.0, (double)Invoke(type));
        }

        [Fact]
        public void DivideAssign_LongAndLong_StillWorks()
        {
            var type = BuildStatic("DALL", typeof(long), m =>
            {
                var v = Expression.Variable(typeof(long));
                m.Append(Expression.Assign(v, Expression.Constant(100L)));
                m.Append(Expression.DivideAssign(v, Expression.Constant(4L)));
                m.Append(v);
            });
            Assert.Equal(25L, (long)Invoke(type));
        }

        [Fact]
        public void ModuloAssign_IntAndInt_StillWorks()
        {
            var type = BuildStatic("MoAII", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(10)));
                m.Append(Expression.ModuloAssign(v, Expression.Constant(3)));
                m.Append(v);
            });
            Assert.Equal(1, (int)Invoke(type));
        }

        [Fact]
        public void AndAssign_IntAndInt_StillWorks()
        {
            var type = BuildStatic("AnAII", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0xFF)));
                m.Append(Expression.AndAssign(v, Expression.Constant(0x0F)));
                m.Append(v);
            });
            Assert.Equal(0x0F, (int)Invoke(type));
        }

        [Fact]
        public void ExclusiveOrAssign_IntAndInt_StillWorks()
        {
            var type = BuildStatic("XAII", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0xAA)));
                m.Append(Expression.ExclusiveOrAssign(v, Expression.Constant(0xFF)));
                m.Append(v);
            });
            Assert.Equal(0xAA ^ 0xFF, (int)Invoke(type));
        }

        // ========== Power / LeftShift / RightShift 同类型路径（新工厂回归） ==========

        [Fact]
        public void Power_DoubleAndDouble_Works()
        {
            var type = BuildStatic("PowDD", typeof(double), m =>
            {
                var a = m.DefineParameter(typeof(double), "a");
                var b = m.DefineParameter(typeof(double), "b");
                m.Append(Expression.Power(a, b));
            });
            Assert.Equal(8.0, (double)Invoke(type, 2.0, 3.0));
        }

        [Fact]
        public void LeftShift_IntAndInt_Works()
        {
            var type = BuildStatic("LShII", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.LeftShift(a, b));
            });
            Assert.Equal(1 << 4, (int)Invoke(type, 1, 4));
        }

        [Fact]
        public void RightShift_LongAndInt_Works()
        {
            // RightShift 左侧整数、右侧 int 是被支持的同类型路径
            var type = BuildStatic("RShLI", typeof(long), m =>
            {
                var a = m.DefineParameter(typeof(long), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.RightShift(a, b));
            });
            Assert.Equal(0xFF00L >> 4, (long)Invoke(type, 0xFF00L, 4));
        }

        [Fact]
        public void RightShift_UintAndInt_Works_UsesUnsignedShift()
        {
            // 无符号类型使用 Shr_Un，最高位填 0
            var type = BuildStatic("RShUI", typeof(uint), m =>
            {
                var a = m.DefineParameter(typeof(uint), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.RightShift(a, b));
            });
            Assert.Equal(0x80000000u >> 4, (uint)Invoke(type, 0x80000000u, 4));
        }

        [Fact]
        public void LeftShiftAssign_IntAndInt_Works()
        {
            var type = BuildStatic("LSAII", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(1)));
                m.Append(Expression.LeftShiftAssign(v, Expression.Constant(8)));
                m.Append(v);
            });
            Assert.Equal(256, (int)Invoke(type));
        }

        [Fact]
        public void RightShiftAssign_LongAndInt_Works()
        {
            var type = BuildStatic("RSALI", typeof(long), m =>
            {
                var v = Expression.Variable(typeof(long));
                m.Append(Expression.Assign(v, Expression.Constant(0xFF00L)));
                m.Append(Expression.RightShiftAssign(v, Expression.Constant(4)));
                m.Append(v);
            });
            Assert.Equal(0x0FF0L, (long)Invoke(type));
        }

        [Fact]
        public void PowerAssign_DoubleAndDouble_Works()
        {
            var type = BuildStatic("PADD", typeof(double), m =>
            {
                var v = Expression.Variable(typeof(double));
                m.Append(Expression.Assign(v, Expression.Constant(2.0)));
                m.Append(Expression.PowerAssign(v, Expression.Constant(3.0)));
                m.Append(v);
            });
            Assert.Equal(8.0, (double)Invoke(type));
        }

        // ========== 复合赋值：异类型但右侧能隐式提升到左侧（C# 合法） ==========

        [Fact]
        public void AddAssign_LongAndInt_Works()
        {
            // C# 中 long += int 合法（int 隐式提升到 long）
            var type = BuildStatic("AALI", typeof(long), m =>
            {
                var v = Expression.Variable(typeof(long));
                m.Append(Expression.Assign(v, Expression.Constant(100L)));
                m.Append(Expression.AddAssign(v, Expression.Constant(5)));
                m.Append(v);
            });
            Assert.Equal(105L, (long)Invoke(type));
        }

        [Fact]
        public void AddAssign_IntAndByte_Works()
        {
            var type = BuildStatic("AAIB", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(10)));
                m.Append(Expression.AddAssign(v, Expression.Constant((byte)5)));
                m.Append(v);
            });
            Assert.Equal(15, (int)Invoke(type));
        }

        [Fact]
        public void AddAssign_DoubleAndInt_Works()
        {
            var type = BuildStatic("AADI", typeof(double), m =>
            {
                var v = Expression.Variable(typeof(double));
                m.Append(Expression.Assign(v, Expression.Constant(1.5)));
                m.Append(Expression.AddAssign(v, Expression.Constant(2)));
                m.Append(v);
            });
            Assert.Equal(3.5, (double)Invoke(type));
        }

        [Fact]
        public void AddAssign_LongAndUint_Works()
        {
            // long += uint：uint 隐式转 long ✓
            var type = BuildStatic("AALU", typeof(long), m =>
            {
                var v = Expression.Variable(typeof(long));
                m.Append(Expression.Assign(v, Expression.Constant(100L)));
                m.Append(Expression.AddAssign(v, Expression.Constant((uint)50)));
                m.Append(v);
            });
            Assert.Equal(150L, (long)Invoke(type));
        }

        [Fact]
        public void SubtractAssign_DoubleAndFloat_Works()
        {
            var type = BuildStatic("SADF", typeof(double), m =>
            {
                var v = Expression.Variable(typeof(double));
                m.Append(Expression.Assign(v, Expression.Constant(10.0)));
                m.Append(Expression.SubtractAssign(v, Expression.Constant(2.5f)));
                m.Append(v);
            });
            Assert.Equal(7.5, (double)Invoke(type));
        }

        [Fact]
        public void MultiplyAssign_LongAndInt_Works()
        {
            var type = BuildStatic("MALI", typeof(long), m =>
            {
                var v = Expression.Variable(typeof(long));
                m.Append(Expression.Assign(v, Expression.Constant(7L)));
                m.Append(Expression.MultiplyAssign(v, Expression.Constant(6)));
                m.Append(v);
            });
            Assert.Equal(42L, (long)Invoke(type));
        }

        [Fact]
        public void DivideAssign_DoubleAndInt_Works()
        {
            var type = BuildStatic("DADI", typeof(double), m =>
            {
                var v = Expression.Variable(typeof(double));
                m.Append(Expression.Assign(v, Expression.Constant(10.0)));
                m.Append(Expression.DivideAssign(v, Expression.Constant(4)));
                m.Append(v);
            });
            Assert.Equal(2.5, (double)Invoke(type));
        }

        [Fact]
        public void ModuloAssign_LongAndInt_Works()
        {
            var type = BuildStatic("MoALI", typeof(long), m =>
            {
                var v = Expression.Variable(typeof(long));
                m.Append(Expression.Assign(v, Expression.Constant(10L)));
                m.Append(Expression.ModuloAssign(v, Expression.Constant(3)));
                m.Append(v);
            });
            Assert.Equal(1L, (long)Invoke(type));
        }

        [Fact]
        public void AndAssign_LongAndInt_Works()
        {
            // long &= int：int 隐式转 long ✓
            var type = BuildStatic("AnALI", typeof(long), m =>
            {
                var v = Expression.Variable(typeof(long));
                m.Append(Expression.Assign(v, Expression.Constant(0xFFFFL)));
                m.Append(Expression.AndAssign(v, Expression.Constant(0x0F0F)));
                m.Append(v);
            });
            Assert.Equal(0x0F0FL, (long)Invoke(type));
        }

        [Fact]
        public void OrAssign_LongAndInt_Works()
        {
            var type = BuildStatic("OALI", typeof(long), m =>
            {
                var v = Expression.Variable(typeof(long));
                m.Append(Expression.Assign(v, Expression.Constant(0xF000L)));
                m.Append(Expression.OrAssign(v, Expression.Constant(0x0F)));
                m.Append(v);
            });
            Assert.Equal(0xF00FL, (long)Invoke(type));
        }

        [Fact]
        public void ExclusiveOrAssign_IntAndByte_Works()
        {
            var type = BuildStatic("XAIB", typeof(int), m =>
            {
                var v = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(v, Expression.Constant(0xAA)));
                m.Append(Expression.ExclusiveOrAssign(v, Expression.Constant((byte)0xFF)));
                m.Append(v);
            });
            Assert.Equal(0xAA ^ 0xFF, (int)Invoke(type));
        }
    }
}
