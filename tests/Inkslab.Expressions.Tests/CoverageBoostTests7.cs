using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 覆盖率提升测试 Part 7。
    /// </summary>
    public class CoverageBoostTests7
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB7_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, string name, params object[] args) => t.GetMethod(name).Invoke(null, args);

        #region BinaryExpression — NotEqual, compound assigns, bitwise

        [Fact]
        public void Binary_NotEqual_Int()
        {
            var t = BuildStatic("NEI", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(t, "NEI", 1, 2));
            Assert.False((bool)Invoke(t, "NEI", 3, 3));
        }

        [Fact]
        public void Binary_AddAssign_Variable()
        {
            var t = BuildStatic("AAV", typeof(int), m =>
            {
                var r = Expression.Variable(typeof(int));
                var p = m.DefineParameter(typeof(int), "p");
                m.Append(Expression.Assign(r, p));
                m.Append(Expression.AddAssign(r, Expression.Constant(10)));
                m.Append(r);
            });
            Assert.Equal(15, Invoke(t, "AAV", 5));
        }

        [Fact]
        public void Binary_SubtractAssign_Variable()
        {
            var t = BuildStatic("SAV", typeof(int), m =>
            {
                var r = Expression.Variable(typeof(int));
                var p = m.DefineParameter(typeof(int), "p");
                m.Append(Expression.Assign(r, p));
                m.Append(Expression.SubtractAssign(r, Expression.Constant(3)));
                m.Append(r);
            });
            Assert.Equal(7, Invoke(t, "SAV", 10));
        }

        [Fact]
        public void Binary_MultiplyAssign_Variable()
        {
            var t = BuildStatic("MAV", typeof(int), m =>
            {
                var r = Expression.Variable(typeof(int));
                var p = m.DefineParameter(typeof(int), "p");
                m.Append(Expression.Assign(r, p));
                m.Append(Expression.MultiplyAssign(r, Expression.Constant(3)));
                m.Append(r);
            });
            Assert.Equal(15, Invoke(t, "MAV", 5));
        }

        [Fact]
        public void Binary_DivideAssign_Variable()
        {
            var t = BuildStatic("DAV", typeof(int), m =>
            {
                var r = Expression.Variable(typeof(int));
                var p = m.DefineParameter(typeof(int), "p");
                m.Append(Expression.Assign(r, p));
                m.Append(Expression.DivideAssign(r, Expression.Constant(2)));
                m.Append(r);
            });
            Assert.Equal(5, Invoke(t, "DAV", 10));
        }

        [Fact]
        public void Binary_ModuloAssign_Variable()
        {
            var t = BuildStatic("MOAV", typeof(int), m =>
            {
                var r = Expression.Variable(typeof(int));
                var p = m.DefineParameter(typeof(int), "p");
                m.Append(Expression.Assign(r, p));
                m.Append(Expression.ModuloAssign(r, Expression.Constant(3)));
                m.Append(r);
            });
            Assert.Equal(1, Invoke(t, "MOAV", 10));
        }

        [Fact]
        public void Binary_And_Int()
        {
            var t = BuildStatic("ANDI", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.And(a, b));
            });
            Assert.Equal(0x0F & 0xFF, Invoke(t, "ANDI", 0x0F, 0xFF));
        }

        [Fact]
        public void Binary_Or_Int()
        {
            var t = BuildStatic("ORI", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Or(a, b));
            });
            Assert.Equal(0x0F | 0xF0, Invoke(t, "ORI", 0x0F, 0xF0));
        }

        [Fact]
        public void Binary_ExclusiveOr_Int()
        {
            var t = BuildStatic("XORI", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.ExclusiveOr(a, b));
            });
            Assert.Equal(0x0F ^ 0xFF, Invoke(t, "XORI", 0x0F, 0xFF));
        }

        [Fact]
        public void Binary_NotEqual_UInt()
        {
            var t = BuildStatic("NEU", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(uint), "a");
                var b = m.DefineParameter(typeof(uint), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(t, "NEU", 1u, 2u));
        }

        [Fact]
        public void Binary_NotEqual_Long()
        {
            var t = BuildStatic("NEL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(long), "a");
                var b = m.DefineParameter(typeof(long), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(t, "NEL", 1L, 2L));
        }

        [Fact]
        public void Binary_NotEqual_Double()
        {
            var t = BuildStatic("NED", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(double), "a");
                var b = m.DefineParameter(typeof(double), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(t, "NED", 1.0, 2.0));
        }

        #endregion

        #region UnaryExpression — Negate, Not bool/int, IsFalse, Increment/Decrement typed

        [Fact]
        public void Unary_Negate()
        {
            var t = BuildStatic("NEG", typeof(int), m =>
            {
                var p = m.DefineParameter(typeof(int), "p");
                m.Append(Expression.Negate(p));
            });
            Assert.Equal(-5, Invoke(t, "NEG", 5));
        }

        [Fact]
        public void Unary_Not_Bool()
        {
            var t = BuildStatic("NB", typeof(bool), m =>
            {
                var p = m.DefineParameter(typeof(bool), "p");
                m.Append(Expression.Not(p));
            });
            Assert.False((bool)Invoke(t, "NB", true));
        }

        [Fact]
        public void Unary_Not_Int()
        {
            var t = BuildStatic("NI", typeof(int), m =>
            {
                var p = m.DefineParameter(typeof(int), "p");
                m.Append(Expression.Not(p));
            });
            Assert.Equal(~5, Invoke(t, "NI", 5));
        }

        [Fact]
        public void Unary_IsFalse()
        {
            var t = BuildStatic("IFB", typeof(bool), m =>
            {
                var p = m.DefineParameter(typeof(bool), "p");
                m.Append(Expression.IsFalse(p));
            });
            Assert.True((bool)Invoke(t, "IFB", false));
            Assert.False((bool)Invoke(t, "IFB", true));
        }

        [Fact]
        public void Unary_Increment_Short()
        {
            var t = BuildStatic("ISH", typeof(short), m =>
            {
                var p = m.DefineParameter(typeof(short), "p");
                m.Append(Expression.Increment(p));
            });
            Assert.Equal((short)6, Invoke(t, "ISH", (short)5));
        }

        [Fact]
        public void Unary_Increment_UShort()
        {
            var t = BuildStatic("IUS", typeof(ushort), m =>
            {
                var p = m.DefineParameter(typeof(ushort), "p");
                m.Append(Expression.Increment(p));
            });
            Assert.Equal((ushort)6, Invoke(t, "IUS", (ushort)5));
        }

        [Fact]
        public void Unary_Increment_UInt()
        {
            var t = BuildStatic("IUI", typeof(uint), m =>
            {
                var p = m.DefineParameter(typeof(uint), "p");
                m.Append(Expression.Increment(p));
            });
            Assert.Equal(6u, Invoke(t, "IUI", 5u));
        }

        [Fact]
        public void Unary_Increment_ULong()
        {
            var t = BuildStatic("IULO", typeof(ulong), m =>
            {
                var p = m.DefineParameter(typeof(ulong), "p");
                m.Append(Expression.Increment(p));
            });
            Assert.Equal(6UL, Invoke(t, "IULO", 5UL));
        }

        [Fact]
        public void Unary_Increment_Float()
        {
            var t = BuildStatic("IFL", typeof(float), m =>
            {
                var p = m.DefineParameter(typeof(float), "p");
                m.Append(Expression.Increment(p));
            });
            Assert.Equal(6.0f, Invoke(t, "IFL", 5.0f));
        }

        [Fact]
        public void Unary_Decrement_Float()
        {
            var t = BuildStatic("DFL", typeof(float), m =>
            {
                var p = m.DefineParameter(typeof(float), "p");
                m.Append(Expression.Decrement(p));
            });
            Assert.Equal(4.0f, Invoke(t, "DFL", 5.0f));
        }

        [Fact]
        public void Unary_Decrement_ULong()
        {
            var t = BuildStatic("DULO", typeof(ulong), m =>
            {
                var p = m.DefineParameter(typeof(ulong), "p");
                m.Append(Expression.Decrement(p));
            });
            Assert.Equal(4UL, Invoke(t, "DULO", 5UL));
        }

        [Fact]
        public void Unary_DecrementAssign_Float()
        {
            var t = BuildStatic("DAF", typeof(float), m =>
            {
                var v = Expression.Variable(typeof(float));
                m.Append(Expression.Assign(v, Expression.Constant(5.0f)));
                m.Append(Expression.DecrementAssign(v));
                m.Append(v);
            });
            Assert.Equal(4.0f, Invoke(t, "DAF"));
        }

        [Fact]
        public void Unary_Decrement_Short()
        {
            var t = BuildStatic("DSH", typeof(short), m =>
            {
                var p = m.DefineParameter(typeof(short), "p");
                m.Append(Expression.Decrement(p));
            });
            Assert.Equal((short)4, Invoke(t, "DSH", (short)5));
        }

        [Fact]
        public void Unary_Decrement_UShort()
        {
            var t = BuildStatic("DUSH", typeof(ushort), m =>
            {
                var p = m.DefineParameter(typeof(ushort), "p");
                m.Append(Expression.Decrement(p));
            });
            Assert.Equal((ushort)4, Invoke(t, "DUSH", (ushort)5));
        }

        [Fact]
        public void Unary_Decrement_UInt()
        {
            var t = BuildStatic("DUINT", typeof(uint), m =>
            {
                var p = m.DefineParameter(typeof(uint), "p");
                m.Append(Expression.Decrement(p));
            });
            Assert.Equal(4u, Invoke(t, "DUINT", 5u));
        }

        #endregion

        #region ParameterExpression — ByRef typed paths

        [Fact]
        public void Param_ByRef_Int()
        {
            var t = BuildStatic("PBI", typeof(void), m =>
            {
                var p1 = m.DefineParameter(typeof(int).MakeByRefType(), "p1");
                var p2 = m.DefineParameter(typeof(int).MakeByRefType(), "p2");
                m.Append(Expression.Assign(p2, p1));
            });
            var args = new object[] { 42, 0 };
            t.GetMethod("PBI").Invoke(null, args);
            Assert.Equal(42, args[1]);
        }

        [Fact]
        public void Param_ByRef_Long()
        {
            var t = BuildStatic("PBL", typeof(void), m =>
            {
                var p1 = m.DefineParameter(typeof(long).MakeByRefType(), "p1");
                var p2 = m.DefineParameter(typeof(long).MakeByRefType(), "p2");
                m.Append(Expression.Assign(p2, p1));
            });
            var args = new object[] { 42L, 0L };
            t.GetMethod("PBL").Invoke(null, args);
            Assert.Equal(42L, args[1]);
        }

        [Fact]
        public void Param_ByRef_Float()
        {
            var t = BuildStatic("PBF", typeof(void), m =>
            {
                var p1 = m.DefineParameter(typeof(float).MakeByRefType(), "p1");
                var p2 = m.DefineParameter(typeof(float).MakeByRefType(), "p2");
                m.Append(Expression.Assign(p2, p1));
            });
            var args = new object[] { 3.14f, 0f };
            t.GetMethod("PBF").Invoke(null, args);
            Assert.Equal(3.14f, args[1]);
        }

        [Fact]
        public void Param_ByRef_Double()
        {
            var t = BuildStatic("PBD", typeof(void), m =>
            {
                var p1 = m.DefineParameter(typeof(double).MakeByRefType(), "p1");
                var p2 = m.DefineParameter(typeof(double).MakeByRefType(), "p2");
                m.Append(Expression.Assign(p2, p1));
            });
            var args = new object[] { 3.14, 0.0 };
            t.GetMethod("PBD").Invoke(null, args);
            Assert.Equal(3.14, args[1]);
        }

        [Fact]
        public void Param_ByRef_Byte()
        {
            var t = BuildStatic("PBB", typeof(void), m =>
            {
                var p1 = m.DefineParameter(typeof(byte).MakeByRefType(), "p1");
                var p2 = m.DefineParameter(typeof(byte).MakeByRefType(), "p2");
                m.Append(Expression.Assign(p2, p1));
            });
            var args = new object[] { (byte)42, (byte)0 };
            t.GetMethod("PBB").Invoke(null, args);
            Assert.Equal((byte)42, args[1]);
        }

        [Fact]
        public void Param_ByRef_Short()
        {
            var t = BuildStatic("PBS", typeof(void), m =>
            {
                var p1 = m.DefineParameter(typeof(short).MakeByRefType(), "p1");
                var p2 = m.DefineParameter(typeof(short).MakeByRefType(), "p2");
                m.Append(Expression.Assign(p2, p1));
            });
            var args = new object[] { (short)42, (short)0 };
            t.GetMethod("PBS").Invoke(null, args);
            Assert.Equal((short)42, args[1]);
        }

        [Fact]
        public void Param_ByRef_Bool()
        {
            var t = BuildStatic("PBBOOL", typeof(void), m =>
            {
                var p1 = m.DefineParameter(typeof(bool).MakeByRefType(), "p1");
                var p2 = m.DefineParameter(typeof(bool).MakeByRefType(), "p2");
                m.Append(Expression.Assign(p2, p1));
            });
            var args = new object[] { true, false };
            t.GetMethod("PBBOOL").Invoke(null, args);
            Assert.Equal(true, args[1]);
        }

        [Fact]
        public void Param_ByRef_Char()
        {
            var t = BuildStatic("PBC", typeof(void), m =>
            {
                var p1 = m.DefineParameter(typeof(char).MakeByRefType(), "p1");
                var p2 = m.DefineParameter(typeof(char).MakeByRefType(), "p2");
                m.Append(Expression.Assign(p2, p1));
            });
            var args = new object[] { 'A', '\0' };
            t.GetMethod("PBC").Invoke(null, args);
            Assert.Equal('A', args[1]);
        }

        [Fact]
        public void Param_Assign_NonByRef()
        {
            var t = BuildStatic("PANB", typeof(int), m =>
            {
                var p = m.DefineParameter(typeof(int), "p");
                m.Append(Expression.Assign(p, Expression.Constant(99)));
                m.Append(p);
            });
            Assert.Equal(99, Invoke(t, "PANB", 0));
        }

        #endregion

        #region PropertyExpression — instance write

        [Fact]
        public void Property_InstanceWrite()
        {
            var t = BuildStatic("PIW", typeof(void), m =>
            {
                var obj = m.DefineParameter(typeof(CoverageTarget), "obj");
                var pi = typeof(CoverageTarget).GetProperty("Name");
                m.Append(Expression.Assign(Expression.Property(obj, pi), Expression.Constant("updated")));
            });
            var target = new CoverageTarget();
            Invoke(t, "PIW", target);
            Assert.Equal("updated", target.Name);
        }

        #endregion

        #region MethodCallExpression — static via Call

        [Fact]
        public void Call_Static()
        {
            var t = BuildStatic("CS", typeof(int), m =>
            {
                var s = m.DefineParameter(typeof(string), "s");
                m.Append(Expression.Call(typeof(int).GetMethod("Parse", new[] { typeof(string) }), s));
            });
            Assert.Equal(42, Invoke(t, "CS", "42"));
        }

        [Fact]
        public void Call_InstanceVirtual()
        {
            var t = BuildStatic("CIV", typeof(string), m =>
            {
                var obj = m.DefineParameter(typeof(object), "obj");
                m.Append(Expression.Call(obj, typeof(object).GetMethod("ToString")));
            });
            Assert.Equal("hello", Invoke(t, "CIV", "hello"));
        }

        #endregion

        #region ConvertExpression — more conversions

        [Fact]
        public void Convert_LongToInt()
        {
            var t = BuildStatic("L2I", typeof(int), m =>
            {
                var l = m.DefineParameter(typeof(long), "l");
                m.Append(Expression.Convert(l, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, "L2I", 42L));
        }

        [Fact]
        public void Convert_FloatToDouble()
        {
            var t = BuildStatic("F2D", typeof(double), m =>
            {
                var f = m.DefineParameter(typeof(float), "f");
                m.Append(Expression.Convert(f, typeof(double)));
            });
            var result = (double)Invoke(t, "F2D", 3.0f);
            Assert.True(Math.Abs(result - 3.0) < 0.1);
        }

        [Fact]
        public void Convert_ByteToInt()
        {
            var t = BuildStatic("B2I", typeof(int), m =>
            {
                var b = m.DefineParameter(typeof(byte), "b");
                m.Append(Expression.Convert(b, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, "B2I", (byte)42));
        }

        [Fact]
        public void Convert_IntToEnum()
        {
            var t = BuildStatic("I2E", typeof(DayOfWeek), m =>
            {
                var i = m.DefineParameter(typeof(int), "i");
                m.Append(Expression.Convert(i, typeof(DayOfWeek)));
            });
            Assert.Equal(DayOfWeek.Friday, Invoke(t, "I2E", 5));
        }

        [Fact]
        public void Convert_EnumToInt()
        {
            var t = BuildStatic("E2I", typeof(int), m =>
            {
                var e = m.DefineParameter(typeof(DayOfWeek), "e");
                m.Append(Expression.Convert(e, typeof(int)));
            });
            Assert.Equal(5, Invoke(t, "E2I", DayOfWeek.Friday));
        }

        #endregion

        #region MemberInitExpression — multiple bindings

        [Fact]
        public void MemberInit_MultipleProperties()
        {
            var t = BuildStatic("MIMP", typeof(CoverageTarget), m =>
            {
                m.Append(Expression.MemberInit(
                    Expression.New(typeof(CoverageTarget)),
                    Expression.Bind(typeof(CoverageTarget).GetProperty("Name"), Expression.Constant("test")),
                    Expression.Bind(typeof(CoverageTarget).GetProperty("Value"), Expression.Constant(42))));
            });
            var result = (CoverageTarget)Invoke(t, "MIMP");
            Assert.Equal("test", result.Name);
            Assert.Equal(42, result.Value);
        }

        #endregion
    }
}
