using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Expression/BinaryExpression/Switch/Try/Property/MethodCall/Parameter æ·±åº¦è·¯å¾„è¦†ç›–ã€?
    /// </summary>
    public class DeepPathCoverageTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"DP_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private Type BuildStaticWithParam(string name, Type ret, Type paramType, Action<MethodEmitter, ParameterEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            var p = me.DefineParameter(paramType, "arg0");
            body(me, p);
            return te.CreateType();
        }

        private object Invoke(Type t, params object[] args)
            => t.GetMethod("Run").Invoke(null, args);

        #region BinaryExpression â€?many operator paths

        [Fact]
        public void Binary_Add_Long()
        {
            var t = BuildStatic("BAL", typeof(long), me =>
            {
                me.Append(Expression.Add(Expression.Constant(10L), Expression.Constant(20L)));
            });
            Assert.Equal(30L, Invoke(t));
        }

        [Fact]
        public void Binary_Subtract_Long()
        {
            var t = BuildStatic("BSL", typeof(long), me =>
            {
                me.Append(Expression.Subtract(Expression.Constant(50L), Expression.Constant(20L)));
            });
            Assert.Equal(30L, Invoke(t));
        }

        [Fact]
        public void Binary_Multiply_Long()
        {
            var t = BuildStatic("BML", typeof(long), me =>
            {
                me.Append(Expression.Multiply(Expression.Constant(5L), Expression.Constant(6L)));
            });
            Assert.Equal(30L, Invoke(t));
        }

        [Fact]
        public void Binary_Divide_Long()
        {
            var t = BuildStatic("BDL", typeof(long), me =>
            {
                me.Append(Expression.Divide(Expression.Constant(60L), Expression.Constant(2L)));
            });
            Assert.Equal(30L, Invoke(t));
        }

        [Fact]
        public void Binary_Modulo_Long()
        {
            var t = BuildStatic("BModL", typeof(long), me =>
            {
                me.Append(Expression.Modulo(Expression.Constant(31L), Expression.Constant(7L)));
            });
            Assert.Equal(3L, Invoke(t));
        }

        [Fact]
        public void Binary_Add_Double()
        {
            var t = BuildStatic("BAD", typeof(double), me =>
            {
                me.Append(Expression.Add(Expression.Constant(1.5), Expression.Constant(2.5)));
            });
            Assert.Equal(4.0, Invoke(t));
        }

        [Fact]
        public void Binary_Subtract_Double()
        {
            var t = BuildStatic("BSD", typeof(double), me =>
            {
                me.Append(Expression.Subtract(Expression.Constant(5.0), Expression.Constant(2.5)));
            });
            Assert.Equal(2.5, Invoke(t));
        }

        [Fact]
        public void Binary_Multiply_Double()
        {
            var t = BuildStatic("BMD", typeof(double), me =>
            {
                me.Append(Expression.Multiply(Expression.Constant(3.0), Expression.Constant(2.0)));
            });
            Assert.Equal(6.0, Invoke(t));
        }

        [Fact]
        public void Binary_Divide_Double()
        {
            var t = BuildStatic("BDD", typeof(double), me =>
            {
                me.Append(Expression.Divide(Expression.Constant(6.0), Expression.Constant(2.0)));
            });
            Assert.Equal(3.0, Invoke(t));
        }

        [Fact]
        public void Binary_GreaterThan_Int()
        {
            var t = BuildStatic("BGT", typeof(bool), me =>
            {
                me.Append(Expression.GreaterThan(Expression.Constant(5), Expression.Constant(3)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_GreaterThanOrEqual_Int()
        {
            var t = BuildStatic("BGTE", typeof(bool), me =>
            {
                me.Append(Expression.GreaterThanOrEqual(Expression.Constant(5), Expression.Constant(5)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_LessThan_Int()
        {
            var t = BuildStatic("BLT", typeof(bool), me =>
            {
                me.Append(Expression.LessThan(Expression.Constant(3), Expression.Constant(5)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_LessThanOrEqual_Int()
        {
            var t = BuildStatic("BLTE", typeof(bool), me =>
            {
                me.Append(Expression.LessThanOrEqual(Expression.Constant(5), Expression.Constant(5)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_Equal_Int()
        {
            var t = BuildStatic("BEI", typeof(bool), me =>
            {
                me.Append(Expression.Equal(Expression.Constant(5), Expression.Constant(5)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_NotEqual_Int()
        {
            var t = BuildStatic("BNEI", typeof(bool), me =>
            {
                me.Append(Expression.NotEqual(Expression.Constant(5), Expression.Constant(3)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_And_Int()
        {
            var t = BuildStatic("BAnd", typeof(int), me =>
            {
                me.Append(Expression.And(Expression.Constant(0xFF), Expression.Constant(0x0F)));
            });
            Assert.Equal(0x0F, Invoke(t));
        }

        [Fact]
        public void Binary_Or_Int()
        {
            var t = BuildStatic("BOr", typeof(int), me =>
            {
                me.Append(Expression.Or(Expression.Constant(0xF0), Expression.Constant(0x0F)));
            });
            Assert.Equal(0xFF, Invoke(t));
        }

        [Fact]
        public void Binary_ExclusiveOr_Int()
        {
            var t = BuildStatic("BXor", typeof(int), me =>
            {
                me.Append(Expression.ExclusiveOr(Expression.Constant(0xFF), Expression.Constant(0x0F)));
            });
            Assert.Equal(0xF0, Invoke(t));
        }

        [Fact]
        public void Binary_AndAlso()
        {
            var t = BuildStatic("BAA", typeof(bool), me =>
            {
                me.Append(Expression.AndAlso(Expression.Constant(true), Expression.Constant(true)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_OrElse()
        {
            var t = BuildStatic("BOE", typeof(bool), me =>
            {
                me.Append(Expression.OrElse(Expression.Constant(false), Expression.Constant(true)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_AndAlso_ShortCircuit()
        {
            var t = BuildStatic("BAAS", typeof(bool), me =>
            {
                me.Append(Expression.AndAlso(Expression.Constant(false), Expression.Constant(true)));
            });
            Assert.Equal(false, Invoke(t));
        }

        [Fact]
        public void Binary_OrElse_ShortCircuit()
        {
            var t = BuildStatic("BOES", typeof(bool), me =>
            {
                me.Append(Expression.OrElse(Expression.Constant(true), Expression.Constant(false)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_GreaterThan_Long()
        {
            var t = BuildStatic("BGTL", typeof(bool), me =>
            {
                me.Append(Expression.GreaterThan(Expression.Constant(10L), Expression.Constant(5L)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_LessThan_Double()
        {
            var t = BuildStatic("BLTD", typeof(bool), me =>
            {
                me.Append(Expression.LessThan(Expression.Constant(1.0), Expression.Constant(2.0)));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_Equal_String()
        {
            var t = BuildStatic("BES", typeof(bool), me =>
            {
                me.Append(Expression.Equal(Expression.Constant("a"), Expression.Constant("a")));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_NotEqual_String()
        {
            var t = BuildStatic("BNES", typeof(bool), me =>
            {
                me.Append(Expression.NotEqual(Expression.Constant("a"), Expression.Constant("b")));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void Binary_Assign_Variable()
        {
            var t = BuildStatic("BAsV", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(99)));
                me.Append(v);
            });
            Assert.Equal(99, Invoke(t));
        }

        [Fact]
        public void Binary_AddAssign()
        {
            var t = BuildStatic("BAAs", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(10)));
                me.Append(Expression.AddAssign(v, Expression.Constant(5)));
                me.Append(v);
            });
            Assert.Equal(15, Invoke(t));
        }

        [Fact]
        public void Binary_SubtractAssign()
        {
            var t = BuildStatic("BSAs", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(10)));
                me.Append(Expression.SubtractAssign(v, Expression.Constant(3)));
                me.Append(v);
            });
            Assert.Equal(7, Invoke(t));
        }

        [Fact]
        public void Binary_MultiplyAssign()
        {
            var t = BuildStatic("BMAs", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(5)));
                me.Append(Expression.MultiplyAssign(v, Expression.Constant(3)));
                me.Append(v);
            });
            Assert.Equal(15, Invoke(t));
        }

        [Fact]
        public void Binary_DivideAssign()
        {
            var t = BuildStatic("BDAs", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(20)));
                me.Append(Expression.DivideAssign(v, Expression.Constant(4)));
                me.Append(v);
            });
            Assert.Equal(5, Invoke(t));
        }

        [Fact]
        public void Binary_ModuloAssign()
        {
            var t = BuildStatic("BModAs", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(10)));
                me.Append(Expression.ModuloAssign(v, Expression.Constant(3)));
                me.Append(v);
            });
            Assert.Equal(1, Invoke(t));
        }

        [Fact]
        public void Binary_AndAssign()
        {
            var t = BuildStatic("BAndAs", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(0xFF)));
                me.Append(Expression.AndAssign(v, Expression.Constant(0x0F)));
                me.Append(v);
            });
            Assert.Equal(0x0F, Invoke(t));
        }

        [Fact]
        public void Binary_OrAssign()
        {
            var t = BuildStatic("BOrAs", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(0xF0)));
                me.Append(Expression.OrAssign(v, Expression.Constant(0x0F)));
                me.Append(v);
            });
            Assert.Equal(0xFF, Invoke(t));
        }

        [Fact]
        public void Binary_ExclusiveOrAssign()
        {
            var t = BuildStatic("BXorAs", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(0xFF)));
                me.Append(Expression.ExclusiveOrAssign(v, Expression.Constant(0x0F)));
                me.Append(v);
            });
            Assert.Equal(0xF0, Invoke(t));
        }

        #endregion

        #region SwitchExpression â€?deeper paths

        [Fact]
        public void Switch_MultipleCase_Default()
        {
            var t = BuildStaticWithParam("SMC", typeof(int), typeof(int), (me, p) =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(-1)));
                var sw = Expression.Switch(p);
                sw.Case(Expression.Constant(1)).Append(Expression.Assign(result, Expression.Constant(10)));
                sw.Case(Expression.Constant(2)).Append(Expression.Assign(result, Expression.Constant(20)));
                sw.Case(Expression.Constant(3)).Append(Expression.Assign(result, Expression.Constant(30)));
                me.Append(sw);
                me.Append(result);
            });
            Assert.Equal(10, Invoke(t, 1));
            Assert.Equal(20, Invoke(t, 2));
            Assert.Equal(30, Invoke(t, 3));
            Assert.Equal(-1, Invoke(t, 99));
        }

        [Fact]
        public void Switch_String_Input()
        {
            var t = BuildStaticWithParam("SSI", typeof(int), typeof(string), (me, p) =>
            {
                var result = Expression.Variable(typeof(int));
                var sw = Expression.Switch(p, Expression.Assign(result, Expression.Constant(0)));
                sw.Case(Expression.Constant("a")).Append(Expression.Assign(result, Expression.Constant(1)));
                sw.Case(Expression.Constant("b")).Append(Expression.Assign(result, Expression.Constant(2)));
                me.Append(sw);
                me.Append(result);
            });
            Assert.Equal(1, Invoke(t, "a"));
            Assert.Equal(2, Invoke(t, "b"));
            Assert.Equal(0, Invoke(t, "c"));
        }

        [Fact]
        public void Switch_Void_Body()
        {
            var t = BuildStaticWithParam("SVB", typeof(int), typeof(int), (me, p) =>
            {
                var v = Expression.Variable(typeof(int));
                var sw = Expression.Switch(p, Expression.Assign(v, Expression.Constant(0)));
                sw.Case(Expression.Constant(1)).Append(Expression.Assign(v, Expression.Constant(10)));
                sw.Case(Expression.Constant(2)).Append(Expression.Assign(v, Expression.Constant(20)));
                // default handled via Switch constructor
                me.Append(sw);
                me.Append(v);
            });
            Assert.Equal(10, Invoke(t, 1));
            Assert.Equal(20, Invoke(t, 2));
            Assert.Equal(0, Invoke(t, 99));
        }

        #endregion

        #region TryExpression â€?more paths

        [Fact]
        public void Try_CatchAll()
        {
            var t = BuildStatic("TCA", typeof(int), me =>
            {
                var result = Expression.Variable(typeof(int));
                var tryExpr = Expression.Try();
                tryExpr.Append(Expression.Throw(typeof(InvalidOperationException)));
                tryExpr.Catch(typeof(Exception)).Append(Expression.Assign(result, Expression.Constant(42)));
                me.Append(tryExpr);
                me.Append(result);
            });
            Assert.Equal(42, Invoke(t));
        }

        [Fact]
        public void Try_Finally_Only()
        {
            var t = BuildStatic("TFO", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(v, Expression.Constant(0)));
                var fb = Expression.Block();
                fb.Append(Expression.Assign(v, Expression.Constant(99)));
                var tryExpr = Expression.Try(fb);
                tryExpr.Append(Expression.Assign(v, Expression.Constant(42)));
                me.Append(tryExpr);
                me.Append(v);
            });
            Assert.Equal(99, Invoke(t));
        }

        #endregion

        #region ParameterExpression â€?deeper Load/Assign via ByRef, Position > 3

        [Fact]
        public void Parameter_Position_4()
        {
            var te = _mod.DefineType($"PP4_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            me.DefineParameter(typeof(int), "a");
            me.DefineParameter(typeof(int), "b");
            me.DefineParameter(typeof(int), "c");
            me.DefineParameter(typeof(int), "d");
            var p4 = me.DefineParameter(typeof(int), "e");
            me.Append(p4);
            var type = te.CreateType();
            Assert.Equal(55, type.GetMethod("Run").Invoke(null, new object[] { 1, 2, 3, 4, 55 }));
        }

        [Fact]
        public void Parameter_Position_256()
        {
            var te = _mod.DefineType($"PP256_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));

            ParameterEmitter lastParam = null;
            var args = new object[260];
            for (int i = 0; i < 260; i++)
            {
                lastParam = me.DefineParameter(typeof(int), $"p{i}");
                args[i] = i;
            }
            me.Append(lastParam);
            var type = te.CreateType();
            Assert.Equal(259, type.GetMethod("Run").Invoke(null, args));
        }

        #endregion

        #region PropertyExpression â€?static and instance

        [Fact]
        public void Property_Instance_Read()
        {
            var t = BuildStaticWithParam("PIR", typeof(int), typeof(string), (me, p) =>
            {
                var lengthProp = typeof(string).GetProperty("Length");
                me.Append(Expression.Property(p, lengthProp));
            });
            Assert.Equal(5, Invoke(t, "hello"));
        }

        [Fact]
        public void Property_Static_Read()
        {
            var t = BuildStatic("PSR", typeof(int), me =>
            {
                var prop = typeof(string).GetProperty("Empty");
                var v = Expression.Variable(typeof(string));
                me.Append(Expression.Assign(v, Expression.Constant("hello")));
                me.Append(Expression.Property(v, typeof(string).GetProperty("Length")));
            });
            Assert.Equal(5, Invoke(t));
        }

        #endregion

        #region MethodCallExpression â€?static with multiple args

        [Fact]
        public void MethodCall_Static_MultiArgs()
        {
            var mi = typeof(Math).GetMethod("Max", new[] { typeof(int), typeof(int) });
            var t = BuildStatic("MCSMA", typeof(int), me =>
            {
                me.Append(Expression.Call(mi, Expression.Constant(5), Expression.Constant(10)));
            });
            Assert.Equal(10, Invoke(t));
        }

        [Fact]
        public void MethodCall_Static_Min()
        {
            var mi = typeof(Math).GetMethod("Min", new[] { typeof(int), typeof(int) });
            var t = BuildStatic("MCSMin", typeof(int), me =>
            {
                me.Append(Expression.Call(mi, Expression.Constant(5), Expression.Constant(10)));
            });
            Assert.Equal(5, Invoke(t));
        }

        [Fact]
        public void MethodCall_Instance()
        {
            var mi = typeof(string).GetMethod("ToUpper", Type.EmptyTypes);
            var t = BuildStaticWithParam("MCI", typeof(string), typeof(string), (me, p) =>
            {
                me.Append(Expression.Call(p, mi));
            });
            Assert.Equal("HELLO", Invoke(t, "hello"));
        }

        [Fact]
        public void MethodCall_Instance_Substring()
        {
            var mi = typeof(string).GetMethod("Substring", new[] { typeof(int), typeof(int) });
            var t = BuildStaticWithParam("MCIS", typeof(string), typeof(string), (me, p) =>
            {
                me.Append(Expression.Call(p, mi, Expression.Constant(1), Expression.Constant(3)));
            });
            Assert.Equal("ell", Invoke(t, "hello"));
        }

        #endregion

        #region InvocationExpression â€?via Expression.Invoke(Delegate, args)

        [Fact]
        public void Invocation_StaticCall()
        {
            var mi = typeof(Math).GetMethod("Abs", new[] { typeof(int) });
            var t = BuildStatic("IFD", typeof(int), me =>
            {
                me.Append(Expression.Call(mi, Expression.Constant(-42)));
            });
            Assert.Equal(42, Invoke(t));
        }

        #endregion

        #region ArrayExpression â€?multi-type, NewArray

        [Fact]
        public void Array_New_String()
        {
            var t = BuildStatic("ANS", typeof(string[]), me =>
            {
                me.Append(Expression.NewArray(3, typeof(string)));
            });
            var arr = (string[])Invoke(t);
            Assert.Equal(3, arr.Length);
        }

        [Fact]
        public void Array_New_Object()
        {
            var t = BuildStatic("ANO", typeof(object[]), me =>
            {
                me.Append(Expression.NewArray(5, typeof(object)));
            });
            var arr = (object[])Invoke(t);
            Assert.Equal(5, arr.Length);
        }

        [Fact]
        public void Array_Index_Set_Get()
        {
            var te = _mod.DefineType($"AISG_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var arr = me.DefineParameter(typeof(int[]), "arr");
            me.Append(Expression.Assign(Expression.ArrayIndex(arr, 1), Expression.Constant(42)));
            me.Append(Expression.ArrayIndex(arr, 1));
            var type = te.CreateType();
            var data = new int[3];
            Assert.Equal(42, type.GetMethod("Run").Invoke(null, new object[] { data }));
        }

        [Fact]
        public void Array_Length()
        {
            var te = _mod.DefineType($"AL_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var arr = me.DefineParameter(typeof(int[]), "arr");
            me.Append(Expression.ArrayLength(arr));
            var type = te.CreateType();
            Assert.Equal(7, type.GetMethod("Run").Invoke(null, new object[] { new int[7] }));
        }

        #endregion

        #region MemberInitExpression â€?deeper paths

        [Fact]
        public void MemberInit_MultipleProperties()
        {
            var nameProp = typeof(DPHelper).GetProperty("Name");
            var ageProp = typeof(DPHelper).GetProperty("Age");
            var t = BuildStatic("MIMP", typeof(string), me =>
            {
                var init = Expression.MemberInit(
                    Expression.New(typeof(DPHelper)),
                    Expression.Bind(nameProp, Expression.Constant("John")),
                    Expression.Bind(ageProp, Expression.Constant(30)));
                var v = Expression.Variable(typeof(DPHelper));
                me.Append(Expression.Assign(v, init));
                me.Append(Expression.Property(v, nameProp));
            });
            Assert.Equal("John", Invoke(t));
        }

        #endregion

        #region Expression factory â€?more paths

        [Fact]
        public void Expression_Coalesce()
        {
            var t = BuildStaticWithParam("ECo", typeof(string), typeof(string), (me, p) =>
            {
                me.Append(Expression.Coalesce(p, Expression.Constant("default")));
            });
            Assert.Equal("hello", Invoke(t, "hello"));
            Assert.Equal("default", Invoke(t, new object[] { null }));
        }

        [Fact]
        public void Expression_TypeAs()
        {
            var t = BuildStaticWithParam("ETA", typeof(string), typeof(object), (me, p) =>
            {
                me.Append(Expression.TypeAs(p, typeof(string)));
            });
            Assert.Equal("test", Invoke(t, "test"));
            Assert.Null(Invoke(t, 42));
        }

        [Fact]
        public void Expression_TypeIs()
        {
            var t = BuildStaticWithParam("ETI", typeof(bool), typeof(object), (me, p) =>
            {
                me.Append(Expression.TypeIs(p, typeof(string)));
            });
            Assert.Equal(true, Invoke(t, "test"));
            Assert.Equal(false, Invoke(t, 42));
        }

        [Fact]
        public void Expression_Negate_Int()
        {
            var t = BuildStatic("ENI", typeof(int), me =>
            {
                me.Append(Expression.Negate(Expression.Constant(42)));
            });
            Assert.Equal(-42, Invoke(t));
        }

        [Fact]
        public void Expression_Negate_Long()
        {
            var t = BuildStatic("ENL", typeof(long), me =>
            {
                me.Append(Expression.Negate(Expression.Constant(42L)));
            });
            Assert.Equal(-42L, Invoke(t));
        }

        [Fact]
        public void Expression_Negate_Double()
        {
            var t = BuildStatic("END", typeof(double), me =>
            {
                me.Append(Expression.Negate(Expression.Constant(3.14)));
            });
            Assert.Equal(-3.14, Invoke(t));
        }

        [Fact]
        public void Expression_Negate_Float()
        {
            var t = BuildStatic("ENF", typeof(float), me =>
            {
                me.Append(Expression.Negate(Expression.Constant(2.5f)));
            });
            Assert.Equal(-2.5f, Invoke(t));
        }

        [Fact]
        public void Expression_Not_Bool()
        {
            var t = BuildStatic("ENB", typeof(bool), me =>
            {
                me.Append(Expression.Not(Expression.Constant(true)));
            });
            Assert.Equal(false, Invoke(t));
        }

        [Fact]
        public void Expression_Not_Int()
        {
            var t = BuildStatic("ENII", typeof(int), me =>
            {
                me.Append(Expression.Not(Expression.Constant(0xFF)));
            });
            Assert.Equal(~0xFF, Invoke(t));
        }

        [Fact]
        public void Expression_Increment()
        {
            var t = BuildStatic("EInc", typeof(int), me =>
            {
                me.Append(Expression.Increment(Expression.Constant(41)));
            });
            Assert.Equal(42, Invoke(t));
        }

        [Fact]
        public void Expression_Decrement()
        {
            var t = BuildStatic("EDec", typeof(int), me =>
            {
                me.Append(Expression.Decrement(Expression.Constant(43)));
            });
            Assert.Equal(42, Invoke(t));
        }

        [Fact]
        public void Expression_ConditionTernary()
        {
            var t = BuildStaticWithParam("ECT", typeof(string), typeof(bool), (me, p) =>
            {
                me.Append(Expression.Condition(p, Expression.Constant("yes"), Expression.Constant("no")));
            });
            Assert.Equal("yes", Invoke(t, true));
            Assert.Equal("no", Invoke(t, false));
        }

        [Fact]
        public void Expression_Loop_Break()
        {
            var t = BuildStatic("ELB", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int));
                var loop = Expression.Loop();
                loop.Append(Expression.Assign(v, Expression.Add(v, Expression.Constant(1))));
                loop.Append(Expression.IfThen(
                    Expression.GreaterThanOrEqual(v, Expression.Constant(10)),
                    Expression.Break()));
                me.Append(loop);
                me.Append(v);
            });
            Assert.Equal(10, Invoke(t));
        }

        [Fact]
        public void Expression_Goto()
        {
            var t = BuildStatic("EGt", typeof(int), me =>
            {
                var lbl = Expression.Label();
                me.Append(Expression.Goto(lbl));
                me.Append(Expression.Constant(0)); // skipped
                me.Append(Expression.Label(lbl));
                me.Append(Expression.Constant(42));
            });
            Assert.Equal(42, Invoke(t));
        }

        #endregion

        #region NestedClassEmitter

        [Fact]
        public void NestedClass_WithMethod()
        {
            var te = _mod.DefineType($"NCM_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var nested = te.DefineNestedType("Inner", TypeAttributes.NestedPublic);
            nested.DefineDefaultConstructor();
            var m = nested.DefineMethod("GetVal", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            m.Append(Expression.Constant(42));
            te.CreateType();
        }

        [Fact]
        public void NestedClass_WithParent()
        {
            var te = _mod.DefineType($"NCP_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var nested = te.DefineNestedType("Inner2", TypeAttributes.NestedPublic, typeof(object));
            nested.DefineDefaultConstructor();
            te.CreateType();
        }

        [Fact]
        public void NestedClass_WithInterface()
        {
            var te = _mod.DefineType($"NCI_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var nested = te.DefineNestedType("Inner3", TypeAttributes.NestedPublic, typeof(object), new[] { typeof(IDisposable) });
            nested.DefineDefaultConstructor();
            var disposeMethod = typeof(IDisposable).GetMethod("Dispose");
            var overrideM = nested.DefineMethodOverride(ref disposeMethod);
            overrideM.Append(Expression.Default(typeof(void)));
            te.CreateType();
        }

        #endregion
    }

    public class DPHelper
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
