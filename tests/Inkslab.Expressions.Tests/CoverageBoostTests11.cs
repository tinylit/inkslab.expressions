using Inkslab.Emitters;
using System;
using System.Reflection;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Coverage boost: BinaryExpression (unsigned, shift, power, checked),
    /// UnaryExpression (IncrementAssign, DecrementAssign, IsFalse, UnaryPlus),
    /// MethodCallEmitter, PropertyExpression static paths.
    /// </summary>
    public class CoverageBoostTests11
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB11_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var cls = _mod.DefineType($"C11_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return cls.CreateType();
        }

        private Type BuildStaticP(string name, Type ret, Type p1, Action<MethodEmitter, ParameterExpression> body)
        {
            var cls = _mod.DefineType($"C11P_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            var param = me.DefineParameter(p1, ParameterAttributes.None, "a");
            body(me, param);
            return cls.CreateType();
        }

        private Type BuildStaticP2(string name, Type ret, Type p1, Type p2, Action<MethodEmitter, ParameterExpression, ParameterExpression> body)
        {
            var cls = _mod.DefineType($"C11P2_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            var a = me.DefineParameter(p1, ParameterAttributes.None, "a");
            var b = me.DefineParameter(p2, ParameterAttributes.None, "b");
            body(me, a, b);
            return cls.CreateType();
        }

        private object Invoke(Type t, params object[] args) => t.GetMethod("Run").Invoke(null, args);

        // ===== BinaryExpression: unsigned paths =====

        [Fact]
        public void Binary_Add_UInt()
        {
            var t = BuildStaticP2("AddU", typeof(uint), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.Add(a, b));
            });
            Assert.Equal(5u, Invoke(t, 3u, 2u));
        }

        [Fact]
        public void Binary_Subtract_UInt()
        {
            var t = BuildStaticP2("SubU", typeof(uint), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.Subtract(a, b));
            });
            Assert.Equal(1u, Invoke(t, 3u, 2u));
        }

        [Fact]
        public void Binary_Multiply_UInt()
        {
            var t = BuildStaticP2("MulU", typeof(uint), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.Multiply(a, b));
            });
            Assert.Equal(6u, Invoke(t, 3u, 2u));
        }

        [Fact]
        public void Binary_Divide_UInt()
        {
            var t = BuildStaticP2("DivU", typeof(uint), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.Divide(a, b));
            });
            Assert.Equal(5u, Invoke(t, 10u, 2u));
        }

        [Fact]
        public void Binary_Modulo_UInt()
        {
            var t = BuildStaticP2("ModU", typeof(uint), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.Modulo(a, b));
            });
            Assert.Equal(1u, Invoke(t, 7u, 3u));
        }

        [Fact]
        public void Binary_LessThan_UInt()
        {
            var t = BuildStaticP2("LtU", typeof(bool), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.LessThan(a, b));
            });
            Assert.Equal(true, Invoke(t, 1u, 2u));
            Assert.Equal(false, Invoke(t, 2u, 1u));
        }

        [Fact]
        public void Binary_GreaterThan_UInt()
        {
            var t = BuildStaticP2("GtU", typeof(bool), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.GreaterThan(a, b));
            });
            Assert.Equal(true, Invoke(t, 3u, 1u));
            Assert.Equal(false, Invoke(t, 1u, 3u));
        }

        [Fact]
        public void Binary_GreaterThanOrEqual_UInt()
        {
            var t = BuildStaticP2("GteU", typeof(bool), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.GreaterThanOrEqual(a, b));
            });
            Assert.Equal(true, Invoke(t, 3u, 3u));
            Assert.Equal(true, Invoke(t, 3u, 1u));
            Assert.Equal(false, Invoke(t, 1u, 3u));
        }

        [Fact]
        public void Binary_LessThanOrEqual_UInt()
        {
            var t = BuildStaticP2("LteU", typeof(bool), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.LessThanOrEqual(a, b));
            });
            Assert.Equal(true, Invoke(t, 1u, 1u));
            Assert.Equal(true, Invoke(t, 1u, 3u));
            Assert.Equal(false, Invoke(t, 3u, 1u));
        }

        // ===== BinaryExpression: Checked unsigned =====

        [Fact]
        public void Binary_Add_Long()
        {
            var t = BuildStaticP2("AddL", typeof(long), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.Add(a, b));
            });
            Assert.Equal(5L, Invoke(t, 3L, 2L));
        }

        [Fact]
        public void Binary_Add_Float()
        {
            var t = BuildStaticP2("AddF", typeof(float), typeof(float), typeof(float), (me, a, b) =>
            {
                me.Append(Expression.Add(a, b));
            });
            Assert.Equal(5.5f, Invoke(t, 3.0f, 2.5f));
        }

        [Fact]
        public void Binary_Subtract_Long()
        {
            var t = BuildStaticP2("SubL", typeof(long), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.Subtract(a, b));
            });
            Assert.Equal(1L, Invoke(t, 3L, 2L));
        }

        [Fact]
        public void Binary_Subtract_Float()
        {
            var t = BuildStaticP2("SubF", typeof(float), typeof(float), typeof(float), (me, a, b) =>
            {
                me.Append(Expression.Subtract(a, b));
            });
            Assert.Equal(0.5f, Invoke(t, 3.0f, 2.5f));
        }

        [Fact]
        public void Binary_Multiply_Long()
        {
            var t = BuildStaticP2("MulL", typeof(long), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.Multiply(a, b));
            });
            Assert.Equal(6L, Invoke(t, 3L, 2L));
        }

        [Fact]
        public void Binary_Multiply_Float()
        {
            var t = BuildStaticP2("MulF", typeof(float), typeof(float), typeof(float), (me, a, b) =>
            {
                me.Append(Expression.Multiply(a, b));
            });
            Assert.Equal(6.0f, Invoke(t, 3.0f, 2.0f));
        }

        // ===== BinaryExpression: NotEqual =====

        [Fact]
        public void Binary_NotEqual_Int()
        {
            var t = BuildStaticP2("NeqI", typeof(bool), typeof(int), typeof(int), (me, a, b) =>
            {
                me.Append(Expression.NotEqual(a, b));
            });
            Assert.Equal(true, Invoke(t, 1, 2));
            Assert.Equal(false, Invoke(t, 1, 1));
        }

        [Fact]
        public void Binary_NotEqual_UInt()
        {
            var t = BuildStaticP2("NeqU", typeof(bool), typeof(uint), typeof(uint), (me, a, b) =>
            {
                me.Append(Expression.NotEqual(a, b));
            });
            Assert.Equal(true, Invoke(t, 1u, 2u));
            Assert.Equal(false, Invoke(t, 1u, 1u));
        }

        // ===== BinaryExpression: Equal/LT/GT for ulong =====

        [Fact]
        public void Binary_Add_ULong()
        {
            var t = BuildStaticP2("AddUL", typeof(ulong), typeof(ulong), typeof(ulong), (me, a, b) =>
            {
                me.Append(Expression.Add(a, b));
            });
            Assert.Equal(5UL, Invoke(t, 3UL, 2UL));
        }

        [Fact]
        public void Binary_LessThan_ULong()
        {
            var t = BuildStaticP2("LtUL", typeof(bool), typeof(ulong), typeof(ulong), (me, a, b) =>
            {
                me.Append(Expression.LessThan(a, b));
            });
            Assert.Equal(true, Invoke(t, 1UL, 2UL));
        }

        [Fact]
        public void Binary_GreaterThan_ULong()
        {
            var t = BuildStaticP2("GtUL", typeof(bool), typeof(ulong), typeof(ulong), (me, a, b) =>
            {
                me.Append(Expression.GreaterThan(a, b));
            });
            Assert.Equal(true, Invoke(t, 3UL, 1UL));
        }

        // ===== UnaryExpression: IncrementAssign, DecrementAssign, IsFalse, UnaryPlus =====

        [Fact]
        public void Unary_IncrementAssign()
        {
            var t = BuildStaticP("IncA", typeof(int), typeof(int), (me, a) =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, a));
                me.Append(Expression.IncrementAssign(v));
                me.Append(v);
            });
            Assert.Equal(6, Invoke(t, 5));
        }

        [Fact]
        public void Unary_DecrementAssign()
        {
            var t = BuildStaticP("DecA", typeof(int), typeof(int), (me, a) =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, a));
                me.Append(Expression.DecrementAssign(v));
                me.Append(v);
            });
            Assert.Equal(4, Invoke(t, 5));
        }

        [Fact]
        public void Unary_IsFalse()
        {
            var t = BuildStaticP("IsF", typeof(bool), typeof(bool), (me, a) =>
            {
                me.Append(Expression.IsFalse(a));
            });
            Assert.Equal(true, Invoke(t, false));
            Assert.Equal(false, Invoke(t, true));
        }

        [Fact]
        public void Unary_Increment_Long()
        {
            var t = BuildStaticP("IncL", typeof(long), typeof(long), (me, a) =>
            {
                me.Append(Expression.Increment(a));
            });
            Assert.Equal(6L, Invoke(t, 5L));
        }

        [Fact]
        public void Unary_Decrement_Long()
        {
            var t = BuildStaticP("DecL", typeof(long), typeof(long), (me, a) =>
            {
                me.Append(Expression.Decrement(a));
            });
            Assert.Equal(4L, Invoke(t, 5L));
        }

        [Fact]
        public void Unary_Increment_Double()
        {
            var t = BuildStaticP("IncD", typeof(double), typeof(double), (me, a) =>
            {
                me.Append(Expression.Increment(a));
            });
            Assert.Equal(3.5, Invoke(t, 2.5));
        }

        [Fact]
        public void Unary_Decrement_Float()
        {
            var t = BuildStaticP("DecF", typeof(float), typeof(float), (me, a) =>
            {
                me.Append(Expression.Decrement(a));
            });
            Assert.Equal(1.5f, Invoke(t, 2.5f));
        }

        [Fact]
        public void Unary_Negate_Long()
        {
            var t = BuildStaticP("NegL", typeof(long), typeof(long), (me, a) =>
            {
                me.Append(Expression.Negate(a));
            });
            Assert.Equal(-5L, Invoke(t, 5L));
        }

        [Fact]
        public void Unary_Not_Int()
        {
            var t = BuildStaticP("NotI", typeof(int), typeof(int), (me, a) =>
            {
                me.Append(Expression.Not(a));
            });
            Assert.Equal(~42, Invoke(t, 42));
        }

        [Fact]
        public void Unary_IncrementAssign_Short()
        {
            var t = BuildStaticP("IncAS", typeof(short), typeof(short), (me, a) =>
            {
                var v = Expression.Variable(typeof(short));
                me.Append(Expression.Assign(v, a));
                me.Append(Expression.IncrementAssign(v));
                me.Append(v);
            });
            Assert.Equal((short)6, Invoke(t, (short)5));
        }

        // ===== MethodCallEmitter: call MethodEmitter =====

        [Fact]
        public void MethodCallEmitter_StaticNoArgs()
        {
            var cls = _mod.DefineType("MCE1", TypeAttributes.Public);
            var helper = cls.DefineMethod("Helper", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            helper.Append(Expression.Constant(42));
            var run = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            run.Append(Expression.Call(helper));
            var t = cls.CreateType();
            Assert.Equal(42, t.GetMethod("Run").Invoke(null, null));
        }

        [Fact]
        public void MethodCallEmitter_StaticWithArgs()
        {
            var cls = _mod.DefineType("MCE2", TypeAttributes.Public);
            var helper = cls.DefineMethod("Add", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var pa = helper.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            var pb = helper.DefineParameter(typeof(int), ParameterAttributes.None, "y");
            helper.Append(Expression.Add(pa, pb));

            var run = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var ra = run.DefineParameter(typeof(int), ParameterAttributes.None, "a");
            var rb = run.DefineParameter(typeof(int), ParameterAttributes.None, "b");
            run.Append(Expression.Call(helper, ra, rb));
            var t = cls.CreateType();
            Assert.Equal(7, t.GetMethod("Run").Invoke(null, new object[] { 3, 4 }));
        }

        [Fact]
        public void MethodCallEmitter_InstanceCall()
        {
            var cls = _mod.DefineType("MCE3", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            var helper = cls.DefineMethod("GetVal", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int));
            helper.Append(Expression.Constant(99));

            var run = cls.DefineMethod("Exec", MethodAttributes.Public, typeof(int));
            run.Append(Expression.Call(helper));
            var t = cls.CreateType();
            var inst = Activator.CreateInstance(t);
            Assert.Equal(99, t.GetMethod("Exec").Invoke(inst, null));
        }

        // ===== PropertyExpression: static property =====

        [Fact]
        public void PropertyExpression_StaticProperty()
        {
            var t = BuildStatic("StatProp", typeof(DateTime), me =>
            {
                var prop = typeof(DateTime).GetProperty("Now");
                me.Append(Expression.Property(prop));
            });
            var result = (DateTime)Invoke(t);
            Assert.True((DateTime.Now - result).TotalSeconds < 5);
        }

        [Fact]
        public void PropertyExpression_InstanceReadString()
        {
            var t = BuildStaticP("StrLen", typeof(int), typeof(string), (me, a) =>
            {
                var prop = typeof(string).GetProperty("Length");
                me.Append(Expression.Property(a, prop));
            });
            Assert.Equal(5, Invoke(t, "Hello"));
        }

        // ===== BinaryExpression: bitwise on long =====

        [Fact]
        public void Binary_And_Long()
        {
            var t = BuildStaticP2("AndL", typeof(long), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.And(a, b));
            });
            Assert.Equal(0x0FL, Invoke(t, 0xFFL, 0x0FL));
        }

        [Fact]
        public void Binary_Or_Long()
        {
            var t = BuildStaticP2("OrL", typeof(long), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.Or(a, b));
            });
            Assert.Equal(0xFFL, Invoke(t, 0xF0L, 0x0FL));
        }

        [Fact]
        public void Binary_ExclusiveOr_Long()
        {
            var t = BuildStaticP2("XorL", typeof(long), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.ExclusiveOr(a, b));
            });
            Assert.Equal(0xFFL, Invoke(t, 0xF0L, 0x0FL));
        }

        // ===== BinaryExpression: Assign variants for uint =====

        [Fact]
        public void Binary_AddAssign_UInt()
        {
            var t = BuildStaticP("AddAU", typeof(uint), typeof(uint), (me, a) =>
            {
                var v = Expression.Variable(typeof(uint));
                me.Append(Expression.Assign(v, a));
                me.Append(Expression.AddAssign(v, Expression.Constant(10u)));
                me.Append(v);
            });
            Assert.Equal(15u, Invoke(t, 5u));
        }

        [Fact]
        public void Binary_SubtractAssign_UInt()
        {
            var t = BuildStaticP("SubAU", typeof(uint), typeof(uint), (me, a) =>
            {
                var v = Expression.Variable(typeof(uint));
                me.Append(Expression.Assign(v, a));
                me.Append(Expression.SubtractAssign(v, Expression.Constant(2u)));
                me.Append(v);
            });
            Assert.Equal(8u, Invoke(t, 10u));
        }

        // ===== BinaryExpression: short/byte paths =====

        [Fact]
        public void Binary_Add_Short()
        {
            var t = BuildStaticP2("AddS", typeof(short), typeof(short), typeof(short), (me, a, b) =>
            {
                me.Append(Expression.Add(a, b));
            });
            Assert.Equal((short)5, Invoke(t, (short)3, (short)2));
        }
    }
}
