using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Ë¶ÜÁõñÁéáÊèêÂçáÊµãËØ?Part 8 ‚Ä?InvocationExpression, MethodCallExpression, PropertyExpression, ConvertExpression, BinaryExpression, TypeIsExpression, Expression factory„Ä?
    /// </summary>
    public class CoverageBoostTests8
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB8_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, string name, params object[] args) => t.GetMethod(name).Invoke(null, args);

        #region InvocationExpression ‚Ä?MethodInfo overload with typed return, void return, static/instance

        [Fact]
        public void Invoke_StaticMethodInfo_IntReturn()
        {
            // MethodInfo overload: RuntimeType = int, triggers EmitConvertToType
            var mi = typeof(int).GetMethod("Parse", new[] { typeof(string) });
            var t = BuildStatic("ISMIR", typeof(int), m =>
            {
                var args = m.DefineParameter(typeof(object[]), "args");
                m.Append(Expression.Invoke(mi, args));
            });
            Assert.Equal(42, Invoke(t, "ISMIR", (object)new object[] { "42" }));
        }

        [Fact]
        public void Invoke_InstanceMethodInfo_VoidReturn()
        {
            // MethodInfo overload: IsVoid triggers Pop
            var mi = typeof(List<int>).GetMethod("Add");
            var t = BuildStatic("IIMV", typeof(void), m =>
            {
                var inst = m.DefineParameter(typeof(List<int>), "inst");
                var args = m.DefineParameter(typeof(object[]), "args");
                m.Append(Expression.Invoke(inst, mi, args));
            });
            var list = new List<int>();
            Invoke(t, "IIMV", list, new object[] { 42 });
            Assert.Single(list);
            Assert.Equal(42, list[0]);
        }

        [Fact]
        public void Invoke_StaticMethodInfo_NullInstance()
        {
            // Static method: instanceAst is null ‚Ü?Ldnull path
            var mi = typeof(Math).GetMethod("Abs", new[] { typeof(int) });
            var t = BuildStatic("ISMN", typeof(int), m =>
            {
                var args = m.DefineParameter(typeof(object[]), "args");
                m.Append(Expression.Invoke(mi, args));
            });
            Assert.Equal(5, Invoke(t, "ISMN", (object)new object[] { -5 }));
        }

        [Fact]
        public void Invoke_ExpressionOverload_ReturnsObject()
        {
            // Expression overload: RuntimeType == typeof(object), no conversion
            var mi = typeof(int).GetMethod("Parse", new[] { typeof(string) });
            var t = BuildStatic("IEOR", typeof(object), m =>
            {
                var args = m.DefineParameter(typeof(object[]), "args");
                m.Append(Expression.Invoke(Expression.Constant(mi, typeof(MethodInfo)), args));
            });
            Assert.Equal(42, Invoke(t, "IEOR", (object)new object[] { "42" }));
        }

        #endregion

        #region MethodCallExpression ‚Ä?static, instance virtual, instance non-virtual, ByRef args

        [Fact]
        public void Call_StaticWithMultipleArgs()
        {
            var mi = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            var t = BuildStatic("CSMA", typeof(string), m =>
            {
                var a = m.DefineParameter(typeof(string), "a");
                var b = m.DefineParameter(typeof(string), "b");
                m.Append(Expression.Call(mi, a, b));
            });
            Assert.Equal("ab", Invoke(t, "CSMA", "a", "b"));
        }

        [Fact]
        public void Call_Instance_ListContains()
        {
            var mi = typeof(List<int>).GetMethod("Contains");
            var t = BuildStatic("CILC", typeof(bool), m =>
            {
                var list = m.DefineParameter(typeof(List<int>), "list");
                var val = m.DefineParameter(typeof(int), "val");
                m.Append(Expression.Call(list, mi, val));
            });
            var l = new List<int> { 1, 2, 3 };
            Assert.True((bool)Invoke(t, "CILC", l, 2));
            Assert.False((bool)Invoke(t, "CILC", l, 5));
        }

        [Fact]
        public void Call_ByRefParam_TryParse()
        {
            var mi = typeof(int).GetMethod("TryParse", new[] { typeof(string), typeof(int).MakeByRefType() });
            var t = BuildStatic("CBRTP", typeof(bool), m =>
            {
                var s = m.DefineParameter(typeof(string), "s");
                var r = m.DefineParameter(typeof(int).MakeByRefType(), "r");
                m.Append(Expression.Call(mi, s, r));
            });
            var args = new object[] { "42", 0 };
            var result = (bool)t.GetMethod("CBRTP").Invoke(null, args);
            Assert.True(result);
            Assert.Equal(42, args[1]);
        }

        #endregion

        #region PropertyExpression ‚Ä?static property read, instance write more

        [Fact]
        public void Property_StaticRead()
        {
            var pi = typeof(DateTime).GetProperty("Now");
            var t = BuildStatic("PSR", typeof(DateTime), m =>
            {
                m.Append(Expression.Property(pi));
            });
            var result = (DateTime)Invoke(t, "PSR");
            Assert.True(result > DateTime.MinValue);
        }

        [Fact]
        public void Property_InstanceRead_ListCount()
        {
            var pi = typeof(List<int>).GetProperty("Count");
            var t = BuildStatic("PIRLC", typeof(int), m =>
            {
                var list = m.DefineParameter(typeof(List<int>), "list");
                m.Append(Expression.Property(list, pi));
            });
            Assert.Equal(3, Invoke(t, "PIRLC", new List<int> { 1, 2, 3 }));
        }

        [Fact]
        public void Property_InstanceWrite_ListCapacity()
        {
            var pi = typeof(List<int>).GetProperty("Capacity");
            var t = BuildStatic("PIWLC", typeof(void), m =>
            {
                var list = m.DefineParameter(typeof(List<int>), "list");
                m.Append(Expression.Assign(Expression.Property(list, pi), Expression.Constant(100)));
            });
            var l = new List<int>();
            Invoke(t, "PIWLC", l);
            Assert.Equal(100, l.Capacity);
        }

        #endregion

        #region ConvertExpression ‚Ä?int‚Üídouble, short‚Üíint, int‚Üíobject (box), object‚Üíint (unbox)

        [Fact]
        public void Convert_IntToDouble()
        {
            var t = BuildStatic("I2D", typeof(double), m =>
            {
                var i = m.DefineParameter(typeof(int), "i");
                m.Append(Expression.Convert(i, typeof(double)));
            });
            Assert.Equal(42.0, Invoke(t, "I2D", 42));
        }

        [Fact]
        public void Convert_ShortToInt()
        {
            var t = BuildStatic("S2I", typeof(int), m =>
            {
                var s = m.DefineParameter(typeof(short), "s");
                m.Append(Expression.Convert(s, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, "S2I", (short)42));
        }

        [Fact]
        public void Convert_IntToObject()
        {
            var t = BuildStatic("I2O", typeof(object), m =>
            {
                var i = m.DefineParameter(typeof(int), "i");
                m.Append(Expression.Convert(i, typeof(object)));
            });
            Assert.Equal(42, Invoke(t, "I2O", 42));
        }

        [Fact]
        public void Convert_ObjectToInt()
        {
            var t = BuildStatic("O2I", typeof(int), m =>
            {
                var o = m.DefineParameter(typeof(object), "o");
                m.Append(Expression.Convert(o, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, "O2I", (object)42));
        }

        [Fact]
        public void Convert_IntToLong()
        {
            var t = BuildStatic("I2L", typeof(long), m =>
            {
                var i = m.DefineParameter(typeof(int), "i");
                m.Append(Expression.Convert(i, typeof(long)));
            });
            Assert.Equal(42L, Invoke(t, "I2L", 42));
        }

        [Fact]
        public void Convert_DoubleToInt()
        {
            var t = BuildStatic("D2I", typeof(int), m =>
            {
                var d = m.DefineParameter(typeof(double), "d");
                m.Append(Expression.Convert(d, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, "D2I", 42.0));
        }

        [Fact]
        public void Convert_UIntToLong()
        {
            var t = BuildStatic("UI2L", typeof(long), m =>
            {
                var u = m.DefineParameter(typeof(uint), "u");
                m.Append(Expression.Convert(u, typeof(long)));
            });
            Assert.Equal(42L, Invoke(t, "UI2L", 42u));
        }

        [Fact]
        public void Convert_IntToFloat()
        {
            var t = BuildStatic("I2F", typeof(float), m =>
            {
                var i = m.DefineParameter(typeof(int), "i");
                m.Append(Expression.Convert(i, typeof(float)));
            });
            Assert.Equal(42.0f, Invoke(t, "I2F", 42));
        }

        [Fact]
        public void Convert_ULongToDouble()
        {
            var t = BuildStatic("UL2D", typeof(double), m =>
            {
                var u = m.DefineParameter(typeof(ulong), "u");
                m.Append(Expression.Convert(u, typeof(double)));
            });
            Assert.Equal(42.0, Invoke(t, "UL2D", 42UL));
        }

        [Fact]
        public void Convert_SByteToInt()
        {
            var t = BuildStatic("SB2I", typeof(int), m =>
            {
                var s = m.DefineParameter(typeof(sbyte), "s");
                m.Append(Expression.Convert(s, typeof(int)));
            });
            Assert.Equal(-5, Invoke(t, "SB2I", (sbyte)-5));
        }

        [Fact]
        public void Convert_CharToInt()
        {
            var t = BuildStatic("C2I", typeof(int), m =>
            {
                var c = m.DefineParameter(typeof(char), "c");
                m.Append(Expression.Convert(c, typeof(int)));
            });
            Assert.Equal(65, Invoke(t, "C2I", 'A'));
        }

        #endregion

        #region BinaryExpression ‚Ä?float/double arithmetic, LessThan/GreaterThan float

        [Fact]
        public void Binary_Add_Float()
        {
            var t = BuildStatic("ADFL", typeof(float), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.Add(a, b));
            });
            Assert.Equal(5.0f, Invoke(t, "ADFL", 2.0f, 3.0f));
        }

        [Fact]
        public void Binary_Subtract_Float()
        {
            var t = BuildStatic("SBFL", typeof(float), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.Subtract(a, b));
            });
            Assert.Equal(1.0f, Invoke(t, "SBFL", 3.0f, 2.0f));
        }

        [Fact]
        public void Binary_Multiply_Float()
        {
            var t = BuildStatic("MLFL", typeof(float), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.Multiply(a, b));
            });
            Assert.Equal(6.0f, Invoke(t, "MLFL", 2.0f, 3.0f));
        }

        [Fact]
        public void Binary_Divide_Float()
        {
            var t = BuildStatic("DVFL", typeof(float), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.Divide(a, b));
            });
            Assert.Equal(5.0f, Invoke(t, "DVFL", 10.0f, 2.0f));
        }

        [Fact]
        public void Binary_LessThan_Float()
        {
            var t = BuildStatic("LTFL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.LessThan(a, b));
            });
            Assert.True((bool)Invoke(t, "LTFL", 1.0f, 2.0f));
        }

        [Fact]
        public void Binary_GreaterThan_Float()
        {
            var t = BuildStatic("GTFL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.GreaterThan(a, b));
            });
            Assert.True((bool)Invoke(t, "GTFL", 3.0f, 2.0f));
        }

        [Fact]
        public void Binary_LessThanOrEqual_Float()
        {
            var t = BuildStatic("LTEF", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.LessThanOrEqual(a, b));
            });
            Assert.True((bool)Invoke(t, "LTEF", 2.0f, 2.0f));
        }

        [Fact]
        public void Binary_GreaterThanOrEqual_Float()
        {
            var t = BuildStatic("GTEF", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.GreaterThanOrEqual(a, b));
            });
            Assert.True((bool)Invoke(t, "GTEF", 2.0f, 2.0f));
        }

        [Fact]
        public void Binary_Equal_Float()
        {
            var t = BuildStatic("EFL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.Equal(a, b));
            });
            Assert.True((bool)Invoke(t, "EFL", 2.0f, 2.0f));
        }

        [Fact]
        public void Binary_NotEqual_Float()
        {
            var t = BuildStatic("NEFL", typeof(bool), m =>
            {
                var a = m.DefineParameter(typeof(float), "a");
                var b = m.DefineParameter(typeof(float), "b");
                m.Append(Expression.NotEqual(a, b));
            });
            Assert.True((bool)Invoke(t, "NEFL", 1.0f, 2.0f));
        }

        #endregion

        #region TypeIsExpression ‚Ä?Nullable, sealed

        [Fact]
        public void TypeIs_NullableInt()
        {
            var t = BuildStatic("TINI", typeof(bool), m =>
            {
                var o = m.DefineParameter(typeof(object), "o");
                m.Append(Expression.TypeIs(o, typeof(int?)));
            });
            Assert.True((bool)Invoke(t, "TINI", (object)42));
        }

        [Fact]
        public void TypeIs_SealedType()
        {
            var t = BuildStatic("TIST", typeof(bool), m =>
            {
                var o = m.DefineParameter(typeof(object), "o");
                m.Append(Expression.TypeIs(o, typeof(string)));
            });
            Assert.True((bool)Invoke(t, "TIST", (object)"hello"));
            Assert.False((bool)Invoke(t, "TIST", (object)42));
        }

        #endregion

        #region Expression factory ‚Ä?various factory methods for coverage

        [Fact]
        public void Factory_Throw_TypeOnly()
        {
            var t = BuildStatic("FTT", typeof(void), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.IfThen(flag, Expression.Throw(typeof(InvalidOperationException))));
            });
            Invoke(t, "FTT", false);
            Assert.Throws<TargetInvocationException>(() => Invoke(t, "FTT", true));
        }

        [Fact]
        public void Factory_Throw_WithMessage()
        {
            var t = BuildStatic("FTM", typeof(void), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.IfThen(flag, Expression.Throw(typeof(InvalidOperationException), "test")));
            });
            Invoke(t, "FTM", false);
        }

        [Fact]
        public void Factory_New_DefaultCtor()
        {
            var t = BuildStatic("FND", typeof(object), m =>
            {
                m.Append(Expression.New(typeof(object)));
            });
            Assert.NotNull(Invoke(t, "FND"));
        }

        [Fact]
        public void Factory_New_CtorWithArgs()
        {
            var ctor = typeof(string).GetConstructor(new[] { typeof(char), typeof(int) });
            var t = BuildStatic("FNCA", typeof(string), m =>
            {
                m.Append(Expression.New(ctor, Expression.Constant('x'), Expression.Constant(3)));
            });
            Assert.Equal("xxx", Invoke(t, "FNCA"));
        }

        [Fact]
        public void Factory_Goto()
        {
            var t = BuildStatic("FGO", typeof(void), m =>
            {
                var label = Expression.Label();
                m.Append(Expression.Goto(label));
                m.Append(Expression.Label(label));
            });
            Invoke(t, "FGO");
        }

        [Fact]
        public void Factory_Condition_ThreeWay()
        {
            var t = BuildStatic("FC3", typeof(int), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.Condition(flag, Expression.Constant(1), Expression.Constant(0)));
            });
            Assert.Equal(1, Invoke(t, "FC3", true));
            Assert.Equal(0, Invoke(t, "FC3", false));
        }

        [Fact]
        public void Factory_ArrayLength()
        {
            var t = BuildStatic("FAL", typeof(int), m =>
            {
                var arr = m.DefineParameter(typeof(int[]), "arr");
                m.Append(Expression.ArrayLength(arr));
            });
            Assert.Equal(3, Invoke(t, "FAL", (object)new int[] { 1, 2, 3 }));
        }

        [Fact]
        public void Factory_DeclaringCall()
        {
            // DeclaringCall forces non-virtual call
            var mi = typeof(object).GetMethod("ToString");
            var t = BuildStatic("FDC", typeof(string), m =>
            {
                var obj = m.DefineParameter(typeof(CoverageTarget), "obj");
                m.Append(Expression.DeclaringCall(obj, mi));
            });
            // This calls Object.ToString() directly, not CoverageTarget.ToString()
            var target = new CoverageTarget();
            var result = (string)Invoke(t, "FDC", target);
            Assert.NotNull(result);
        }

        #endregion
    }
}
