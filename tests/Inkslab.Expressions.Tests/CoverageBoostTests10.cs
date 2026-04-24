using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    public class CoverageBoostTests10
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB10_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private Type BuildStaticP(string name, Type ret, Type p1, Action<MethodEmitter, ParameterEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            var p = me.DefineParameter(p1, "a");
            body(me, p);
            return te.CreateType();
        }

        private object Invoke(Type t, params object[] a) => t.GetMethod("Run").Invoke(null, a);

        #region BinaryExpression â€?unsigned, float comparisons, more operators

        [Fact] public void Binary_Add_Byte() { var t = BuildStatic("AByte", typeof(int), m => { m.Append(Expression.Add(Expression.Constant(10), Expression.Constant(20))); }); Assert.Equal(30, Invoke(t)); }
        [Fact] public void Binary_Subtract_Float() { var t = BuildStatic("SFloat", typeof(float), m => { m.Append(Expression.Subtract(Expression.Constant(5.0f), Expression.Constant(2.0f))); }); Assert.Equal(3.0f, Invoke(t)); }
        [Fact] public void Binary_Multiply_Float() { var t = BuildStatic("MFloat", typeof(float), m => { m.Append(Expression.Multiply(Expression.Constant(3.0f), Expression.Constant(4.0f))); }); Assert.Equal(12.0f, Invoke(t)); }
        [Fact] public void Binary_Divide_Float() { var t = BuildStatic("DFloat", typeof(float), m => { m.Append(Expression.Divide(Expression.Constant(10.0f), Expression.Constant(2.0f))); }); Assert.Equal(5.0f, Invoke(t)); }
        [Fact] public void Binary_Modulo_Float() { var t = BuildStatic("ModFloat", typeof(float), m => { m.Append(Expression.Modulo(Expression.Constant(7.0f), Expression.Constant(3.0f))); }); Assert.Equal(1.0f, Invoke(t)); }
        [Fact] public void Binary_Modulo_Double() { var t = BuildStatic("ModDbl", typeof(double), m => { m.Append(Expression.Modulo(Expression.Constant(7.0), Expression.Constant(3.0))); }); Assert.Equal(1.0, Invoke(t)); }

        [Fact] public void Binary_GT_Float() { var t = BuildStatic("GTF", typeof(bool), m => { m.Append(Expression.GreaterThan(Expression.Constant(2.0f), Expression.Constant(1.0f))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_GTE_Float() { var t = BuildStatic("GTEF", typeof(bool), m => { m.Append(Expression.GreaterThanOrEqual(Expression.Constant(2.0f), Expression.Constant(2.0f))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_LT_Float() { var t = BuildStatic("LTF", typeof(bool), m => { m.Append(Expression.LessThan(Expression.Constant(1.0f), Expression.Constant(2.0f))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_LTE_Float() { var t = BuildStatic("LTEF", typeof(bool), m => { m.Append(Expression.LessThanOrEqual(Expression.Constant(2.0f), Expression.Constant(2.0f))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_Equal_Float() { var t = BuildStatic("EqF", typeof(bool), m => { m.Append(Expression.Equal(Expression.Constant(2.0f), Expression.Constant(2.0f))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_NotEqual_Float() { var t = BuildStatic("NEqF", typeof(bool), m => { m.Append(Expression.NotEqual(Expression.Constant(1.0f), Expression.Constant(2.0f))); }); Assert.Equal(true, Invoke(t)); }

        [Fact] public void Binary_GT_Double() { var t = BuildStatic("GTD", typeof(bool), m => { m.Append(Expression.GreaterThan(Expression.Constant(2.0), Expression.Constant(1.0))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_LT_Long() { var t = BuildStatic("LTLo", typeof(bool), m => { m.Append(Expression.LessThan(Expression.Constant(1L), Expression.Constant(2L))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_Equal_Long() { var t = BuildStatic("EqLo", typeof(bool), m => { m.Append(Expression.Equal(Expression.Constant(5L), Expression.Constant(5L))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_NotEqual_Long() { var t = BuildStatic("NEqLo", typeof(bool), m => { m.Append(Expression.NotEqual(Expression.Constant(5L), Expression.Constant(6L))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_Equal_Double() { var t = BuildStatic("EqD", typeof(bool), m => { m.Append(Expression.Equal(Expression.Constant(3.14), Expression.Constant(3.14))); }); Assert.Equal(true, Invoke(t)); }
        [Fact] public void Binary_NotEqual_Double() { var t = BuildStatic("NEqD", typeof(bool), m => { m.Append(Expression.NotEqual(Expression.Constant(3.14), Expression.Constant(2.71))); }); Assert.Equal(true, Invoke(t)); }

        [Fact] public void Binary_And_Long() { var t = BuildStatic("AndL", typeof(long), m => { m.Append(Expression.And(Expression.Constant(0xFFL), Expression.Constant(0x0FL))); }); Assert.Equal(0x0FL, Invoke(t)); }
        [Fact] public void Binary_Or_Long() { var t = BuildStatic("OrL", typeof(long), m => { m.Append(Expression.Or(Expression.Constant(0xF0L), Expression.Constant(0x0FL))); }); Assert.Equal(0xFFL, Invoke(t)); }
        [Fact] public void Binary_XOr_Long() { var t = BuildStatic("XorL", typeof(long), m => { m.Append(Expression.ExclusiveOr(Expression.Constant(0xFFL), Expression.Constant(0x0FL))); }); Assert.Equal(0xF0L, Invoke(t)); }

        [Fact] public void Binary_AddAssign_Long() { var t = BuildStatic("AAL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(10L))); m.Append(Expression.AddAssign(v, Expression.Constant(5L))); m.Append(v); }); Assert.Equal(15L, Invoke(t)); }
        [Fact] public void Binary_SubAssign_Long() { var t = BuildStatic("SAL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(10L))); m.Append(Expression.SubtractAssign(v, Expression.Constant(3L))); m.Append(v); }); Assert.Equal(7L, Invoke(t)); }
        [Fact] public void Binary_MulAssign_Long() { var t = BuildStatic("MAL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(5L))); m.Append(Expression.MultiplyAssign(v, Expression.Constant(3L))); m.Append(v); }); Assert.Equal(15L, Invoke(t)); }
        [Fact] public void Binary_DivAssign_Long() { var t = BuildStatic("DAL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(20L))); m.Append(Expression.DivideAssign(v, Expression.Constant(4L))); m.Append(v); }); Assert.Equal(5L, Invoke(t)); }
        [Fact] public void Binary_ModAssign_Long() { var t = BuildStatic("ModAL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(10L))); m.Append(Expression.ModuloAssign(v, Expression.Constant(3L))); m.Append(v); }); Assert.Equal(1L, Invoke(t)); }

        [Fact] public void Binary_AddAssign_Float() { var t = BuildStatic("AAFl", typeof(float), m => { var v = Expression.Variable(typeof(float)); m.Append(Expression.Assign(v, Expression.Constant(1.5f))); m.Append(Expression.AddAssign(v, Expression.Constant(2.5f))); m.Append(v); }); Assert.Equal(4.0f, Invoke(t)); }
        [Fact] public void Binary_AddAssign_Double() { var t = BuildStatic("AADb", typeof(double), m => { var v = Expression.Variable(typeof(double)); m.Append(Expression.Assign(v, Expression.Constant(1.5))); m.Append(Expression.AddAssign(v, Expression.Constant(2.5))); m.Append(v); }); Assert.Equal(4.0, Invoke(t)); }

        #endregion

        #region SwitchExpression â€?more paths

        [Fact]
        public void Switch_Short()
        {
            var t = BuildStaticP("SShort", typeof(int), typeof(short), (me, p) =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(-1)));
                var sw = Expression.Switch(Expression.Convert(p, typeof(int)));
                sw.Case(Expression.Constant(1)).Append(Expression.Assign(result, Expression.Constant(10)));
                sw.Case(Expression.Constant(2)).Append(Expression.Assign(result, Expression.Constant(20)));
                me.Append(sw);
                me.Append(result);
            });
            Assert.Equal(10, Invoke(t, (short)1));
            Assert.Equal(20, Invoke(t, (short)2));
            Assert.Equal(-1, Invoke(t, (short)99));
        }

        [Fact]
        public void Switch_Long()
        {
            var t = BuildStaticP("SLong", typeof(int), typeof(long), (me, p) =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));
                var sw = Expression.Switch(p);
                sw.Case(Expression.Constant(1L)).Append(Expression.Assign(result, Expression.Constant(10)));
                sw.Case(Expression.Constant(2L)).Append(Expression.Assign(result, Expression.Constant(20)));
                me.Append(sw);
                me.Append(result);
            });
            Assert.Equal(10, Invoke(t, 1L));
            Assert.Equal(20, Invoke(t, 2L));
        }

        #endregion

        #region TryExpression â€?catch with variable, multiple catch

        [Fact]
        public void Try_CatchWithVariable()
        {
            var t = BuildStatic("TCVar", typeof(int), me =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));
                var tryExpr = Expression.Try();
                tryExpr.Append(Expression.Throw(typeof(InvalidOperationException)));
                var exVar = Expression.Variable(typeof(Exception));
                tryExpr.Catch(exVar).Append(Expression.Assign(result, Expression.Constant(42)));
                me.Append(tryExpr);
                me.Append(result);
            });
            Assert.Equal(42, Invoke(t));
        }

        [Fact]
        public void Try_CatchGeneric()
        {
            var t = BuildStatic("TCG", typeof(int), me =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));
                var tryExpr = Expression.Try();
                tryExpr.Append(Expression.Throw(typeof(InvalidOperationException)));
                tryExpr.Catch().Append(Expression.Assign(result, Expression.Constant(1)));
                me.Append(tryExpr);
                me.Append(result);
            });
            Assert.Equal(1, Invoke(t));
        }

        [Fact]
        public void Try_NoCatch_JustFinally()
        {
            var t = BuildStatic("TNCF", typeof(int), me =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(0)));
                var fb = Expression.Block();
                fb.Append(Expression.Assign(result, Expression.Constant(42)));
                var tryExpr = Expression.Try(fb);
                tryExpr.Append(Expression.Assign(result, Expression.Constant(10)));
                me.Append(tryExpr);
                me.Append(result);
            });
            Assert.Equal(42, Invoke(t));
        }

        #endregion

        #region MethodCallExpression â€?instance void, static void

        [Fact]
        public void MethodCall_Static_Void()
        {
            var mi = typeof(System.Threading.Thread).GetMethod("MemoryBarrier");
            var t = BuildStatic("MCSV", typeof(int), me =>
            {
                me.Append(Expression.Call(mi));
                me.Append(Expression.Constant(1));
            });
            Assert.Equal(1, Invoke(t));
        }

        [Fact]
        public void MethodCall_Instance_Void()
        {
            var mi = typeof(System.Collections.ArrayList).GetMethod("Clear");
            var t = BuildStaticP("MCIV", typeof(int), typeof(System.Collections.ArrayList), (me, p) =>
            {
                me.Append(Expression.Call(p, mi));
                me.Append(Expression.Constant(1));
            });
            Assert.Equal(1, Invoke(t, new System.Collections.ArrayList()));
        }

        #endregion

        #region PropertyExpression â€?static write, instance write

        [Fact]
        public void Property_Instance_Write()
        {
            var t = BuildStaticP("PIW", typeof(int), typeof(CoverageTarget10), (me, p) =>
            {
                var prop = typeof(CoverageTarget10).GetProperty("Value");
                me.Append(Expression.Assign(Expression.Property(p, prop), Expression.Constant(99)));
                me.Append(Expression.Property(p, prop));
            });
            Assert.Equal(99, Invoke(t, new CoverageTarget10()));
        }

        #endregion

        #region ParameterExpression â€?various positions for Load path

        [Fact] public void Param_Pos0() { var t = BuildStaticP("PP0", typeof(int), typeof(int), (m, p) => { m.Append(p); }); Assert.Equal(42, Invoke(t, 42)); }
        [Fact] public void Param_Pos1() { var te = _mod.DefineType($"PP1_{Guid.NewGuid():N}", TypeAttributes.Public); te.DefineDefaultConstructor(); var m = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int)); m.DefineParameter(typeof(int), "a"); var p1 = m.DefineParameter(typeof(int), "b"); m.Append(p1); var type = te.CreateType(); Assert.Equal(99, type.GetMethod("Run").Invoke(null, new object[] { 0, 99 })); }
        [Fact] public void Param_Pos2() { var te = _mod.DefineType($"PP2_{Guid.NewGuid():N}", TypeAttributes.Public); te.DefineDefaultConstructor(); var m = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int)); m.DefineParameter(typeof(int), "a"); m.DefineParameter(typeof(int), "b"); var p2 = m.DefineParameter(typeof(int), "c"); m.Append(p2); var type = te.CreateType(); Assert.Equal(77, type.GetMethod("Run").Invoke(null, new object[] { 0, 0, 77 })); }
        [Fact] public void Param_Pos3() { var te = _mod.DefineType($"PP3_{Guid.NewGuid():N}", TypeAttributes.Public); te.DefineDefaultConstructor(); var m = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int)); m.DefineParameter(typeof(int), "a"); m.DefineParameter(typeof(int), "b"); m.DefineParameter(typeof(int), "c"); var p3 = m.DefineParameter(typeof(int), "d"); m.Append(p3); var type = te.CreateType(); Assert.Equal(55, type.GetMethod("Run").Invoke(null, new object[] { 0, 0, 0, 55 })); }

        #endregion

        #region InvocationExpression â€?more typed paths

        [Fact]
        public void Invoke_Static_Call()
        {
            var mi = typeof(Math).GetMethod("Abs", new[] { typeof(int) });
            var t = BuildStatic("ISC", typeof(int), me =>
            {
                me.Append(Expression.Call(mi, Expression.Constant(-42)));
            });
            Assert.Equal(42, Invoke(t));
        }

        #endregion

        #region MemberInitExpression â€?with field binding

        [Fact]
        public void MemberInit_FieldBind()
        {
            var field = typeof(CoverageTarget10).GetField("Tag");
            var t = BuildStatic("MIF", typeof(string), me =>
            {
                var init = Expression.MemberInit(
                    Expression.New(typeof(CoverageTarget10)),
                    Expression.Bind(field, Expression.Constant("hello")));
                var v = Expression.Variable(typeof(CoverageTarget10));
                me.Append(Expression.Assign(v, init));
                me.Append(Expression.Field(v, field));
            });
            Assert.Equal("hello", Invoke(t));
        }

        #endregion

        #region ArrayExpression â€?element type variety

        [Fact] public void Array_Long() { var t = BuildStatic("ALo", typeof(long[]), m => { m.Append(Expression.NewArray(2, typeof(long))); }); Assert.Equal(2, ((long[])Invoke(t)).Length); }
        [Fact] public void Array_Double() { var t = BuildStatic("AD", typeof(double[]), m => { m.Append(Expression.NewArray(3, typeof(double))); }); Assert.Equal(3, ((double[])Invoke(t)).Length); }
        [Fact] public void Array_Bool() { var t = BuildStatic("AB", typeof(bool[]), m => { m.Append(Expression.NewArray(4, typeof(bool))); }); Assert.Equal(4, ((bool[])Invoke(t)).Length); }

        #endregion

        #region UnaryExpression â€?more typed

        [Fact] public void Negate_Short() { var t = BuildStatic("NS", typeof(short), m => { m.Append(Expression.Negate(Expression.Constant((short)10))); }); Assert.Equal((short)-10, Invoke(t)); }
        [Fact] public void Not_Long() { var t = BuildStatic("NL", typeof(long), m => { m.Append(Expression.Not(Expression.Constant(0xFFL))); }); Assert.Equal(~0xFFL, Invoke(t)); }
        [Fact] public void Increment_Long() { var t = BuildStatic("IncL", typeof(long), m => { m.Append(Expression.Increment(Expression.Constant(41L))); }); Assert.Equal(42L, Invoke(t)); }
        [Fact] public void Decrement_Long() { var t = BuildStatic("DecL", typeof(long), m => { m.Append(Expression.Decrement(Expression.Constant(43L))); }); Assert.Equal(42L, Invoke(t)); }
        [Fact] public void Increment_Double() { var t = BuildStatic("IncD", typeof(double), m => { m.Append(Expression.Increment(Expression.Constant(1.5))); }); Assert.Equal(2.5, Invoke(t)); }
        [Fact] public void Decrement_Double() { var t = BuildStatic("DecD", typeof(double), m => { m.Append(Expression.Decrement(Expression.Constant(3.5))); }); Assert.Equal(2.5, Invoke(t)); }
        [Fact] public void Increment_Float() { var t = BuildStatic("IncF", typeof(float), m => { m.Append(Expression.Increment(Expression.Constant(1.5f))); }); Assert.Equal(2.5f, Invoke(t)); }
        [Fact] public void Decrement_Float() { var t = BuildStatic("DecF", typeof(float), m => { m.Append(Expression.Decrement(Expression.Constant(3.5f))); }); Assert.Equal(2.5f, Invoke(t)); }

        #endregion

        #region ConvertExpression â€?more paths

        [Fact] public void Convert_LongToFloat() { var t = BuildStatic("CLF", typeof(float), m => { m.Append(Expression.Convert(Expression.Constant(42L), typeof(float))); }); Assert.Equal(42.0f, Invoke(t)); }
        [Fact] public void Convert_LongToDouble() { var t = BuildStatic("CLD", typeof(double), m => { m.Append(Expression.Convert(Expression.Constant(42L), typeof(double))); }); Assert.Equal(42.0, Invoke(t)); }
        [Fact] public void Convert_FloatToInt() { var t = BuildStatic("CFI", typeof(int), m => { m.Append(Expression.Convert(Expression.Constant(3.14f), typeof(int))); }); Assert.Equal(3, Invoke(t)); }
        [Fact] public void Convert_FloatToLong() { var t = BuildStatic("CFL", typeof(long), m => { m.Append(Expression.Convert(Expression.Constant(3.14f), typeof(long))); }); Assert.Equal(3L, Invoke(t)); }
        [Fact] public void Convert_DoubleToFloat() { var t = BuildStatic("CDF", typeof(float), m => { m.Append(Expression.Convert(Expression.Constant(3.14), typeof(float))); }); Assert.Equal((float)3.14, Invoke(t)); }
        [Fact] public void Convert_DoubleToLong() { var t = BuildStatic("CDL", typeof(long), m => { m.Append(Expression.Convert(Expression.Constant(3.14), typeof(long))); }); Assert.Equal(3L, Invoke(t)); }
        [Fact] public void Convert_ShortToInt() { var t = BuildStatic("CSI", typeof(int), m => { m.Append(Expression.Convert(Expression.Constant((short)42), typeof(int))); }); Assert.Equal(42, Invoke(t)); }
        [Fact] public void Convert_IntToChar() { var t = BuildStatic("CIC", typeof(char), m => { m.Append(Expression.Convert(Expression.Constant(65), typeof(char))); }); Assert.Equal('A', Invoke(t)); }
        [Fact] public void Convert_CharToInt() { var t = BuildStatic("CCI", typeof(int), m => { m.Append(Expression.Convert(Expression.Constant('A'), typeof(int))); }); Assert.Equal(65, Invoke(t)); }

        #endregion

        #region ModuleEmitter â€?additional overloads

        [Fact]
        public void ModuleEmitter_DefineType_WithBase()
        {
            var mod = new ModuleEmitter($"MDBase_{Guid.NewGuid():N}");
            var te = mod.DefineType($"T_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(System.Collections.ArrayList));
            te.DefineDefaultConstructor();
            var type = te.CreateType();
            Assert.True(typeof(System.Collections.ArrayList).IsAssignableFrom(type));
        }

        [Fact]
        public void ModuleEmitter_DefineType_Attrs()
        {
            var mod = new ModuleEmitter($"MDAtt_{Guid.NewGuid():N}");
            var te = mod.DefineType($"T_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Sealed);
            te.DefineDefaultConstructor();
            var type = te.CreateType();
            Assert.True(type.IsSealed);
        }

        #endregion

        #region ReturnExpression â€?void and typed

        [Fact]
        public void Return_Void()
        {
            var t = BuildStatic("RV", typeof(void), me =>
            {
                me.Append(Expression.Return());
            });
            Invoke(t);
        }

        [Fact]
        public void Return_Typed()
        {
            var t = BuildStatic("RT", typeof(int), me =>
            {
                me.Append(Expression.Return(Expression.Constant(42)));
            });
            Assert.Equal(42, Invoke(t));
        }

        #endregion

        #region BlockExpression â€?IsEmpty

        [Fact]
        public void Block_Empty()
        {
            var block = Expression.Block();
            Assert.True(block.IsEmpty);
            block.Append(Expression.Constant(1));
            Assert.False(block.IsEmpty);
        }

        #endregion

        #region Expression.Constant â€?various Type constants

        [Fact]
        public void Constant_NullableInt()
        {
            var t = BuildStatic("CNI", typeof(int?), me =>
            {
                me.Append(Expression.Constant(42, typeof(int?)));
            });
            Assert.Equal((int?)42, Invoke(t));
        }

        #endregion
    }

    public class CoverageTarget10
    {
        public int Value { get; set; }
        public string Tag;
    }
}
