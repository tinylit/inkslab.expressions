using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 覆盖率提升测试 Part 3 — 深度覆盖 Expression Load 路径。
    /// </summary>
    public class CoverageBoostTests3
    {
        private readonly ModuleEmitter _emitter = new ModuleEmitter($"Inkslab.Expressions.Tests.CoverageBoost3.{Guid.NewGuid():N}");

        private Type BuildStaticMethod(string name, Type returnType, Action<MethodEmitter> body)
        {
            var typeEmitter = _emitter.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var methodEmitter = typeEmitter.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, returnType);
            body(methodEmitter);
            return typeEmitter.CreateType();
        }

        private object InvokeStatic(Type type, string name, params object[] args)
        {
            return type.GetMethod(name).Invoke(null, args);
        }

        #region BinaryExpression — 全面覆盖 Load 中的比较路径

        [Fact]
        public void Equal_Long() { var type = BuildStaticMethod("EqL", typeof(bool), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.Equal(a, b)); }); Assert.True((bool)InvokeStatic(type, "EqL", 5L, 5L)); Assert.False((bool)InvokeStatic(type, "EqL", 5L, 6L)); }
        [Fact]
        public void NotEqual_Long() { var type = BuildStaticMethod("NeL", typeof(bool), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.NotEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "NeL", 5L, 6L)); }
        [Fact]
        public void LessThan_Long() { var type = BuildStaticMethod("LtL", typeof(bool), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.LessThan(a, b)); }); Assert.True((bool)InvokeStatic(type, "LtL", 1L, 2L)); }
        [Fact]
        public void GreaterThan_Double() { var type = BuildStaticMethod("GtD", typeof(bool), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.GreaterThan(a, b)); }); Assert.True((bool)InvokeStatic(type, "GtD", 3.0, 1.0)); }
        [Fact]
        public void Equal_Double() { var type = BuildStaticMethod("EqD", typeof(bool), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.Equal(a, b)); }); Assert.True((bool)InvokeStatic(type, "EqD", 1.0, 1.0)); }
        [Fact]
        public void NotEqual_Double() { var type = BuildStaticMethod("NeD", typeof(bool), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.NotEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "NeD", 1.0, 2.0)); }
        [Fact]
        public void Equal_Float() { var type = BuildStaticMethod("EqF", typeof(bool), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.Equal(a, b)); }); Assert.True((bool)InvokeStatic(type, "EqF", 1.0f, 1.0f)); }
        [Fact]
        public void LessThan_Float() { var type = BuildStaticMethod("LtF", typeof(bool), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.LessThan(a, b)); }); Assert.True((bool)InvokeStatic(type, "LtF", 1.0f, 2.0f)); }
        [Fact]
        public void Divide_Int() { var type = BuildStaticMethod("DivI", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.Divide(a, b)); }); Assert.Equal(5, InvokeStatic(type, "DivI", 15, 3)); }
        [Fact]
        public void Multiply_Double() { var type = BuildStaticMethod("MulD", typeof(double), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.Multiply(a, b)); }); Assert.Equal(6.0, InvokeStatic(type, "MulD", 2.0, 3.0)); }
        [Fact]
        public void Modulo_Long() { var type = BuildStaticMethod("ModL", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.Modulo(a, b)); }); Assert.Equal(1L, InvokeStatic(type, "ModL", 7L, 3L)); }
        [Fact]
        public void Subtract_Float() { var type = BuildStaticMethod("SubF", typeof(float), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.Subtract(a, b)); }); Assert.Equal(1.5f, InvokeStatic(type, "SubF", 3.5f, 2.0f)); }
        [Fact]
        public void Add_Float() { var type = BuildStaticMethod("AddF", typeof(float), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.Add(a, b)); }); Assert.Equal(5.5f, InvokeStatic(type, "AddF", 2.5f, 3.0f)); }
        [Fact]
        public void Divide_Float() { var type = BuildStaticMethod("DivF", typeof(float), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.Divide(a, b)); }); Assert.Equal(2.5f, InvokeStatic(type, "DivF", 5.0f, 2.0f)); }
        [Fact]
        public void Divide_Long() { var type = BuildStaticMethod("DivL2", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.Divide(a, b)); }); Assert.Equal(10L, InvokeStatic(type, "DivL2", 30L, 3L)); }
        [Fact]
        public void Modulo_Float() { var type = BuildStaticMethod("ModF", typeof(float), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.Modulo(a, b)); }); Assert.Equal(1.0f, InvokeStatic(type, "ModF", 3.0f, 2.0f)); }
        [Fact]
        public void NotEqual_Float() { var type = BuildStaticMethod("NeF", typeof(bool), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.NotEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "NeF", 1.0f, 2.0f)); }
        [Fact]
        public void GreaterThan_Float() { var type = BuildStaticMethod("GtF", typeof(bool), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.GreaterThan(a, b)); }); Assert.True((bool)InvokeStatic(type, "GtF", 3.0f, 1.0f)); }
        [Fact]
        public void GreaterThanOrEqual_Float() { var type = BuildStaticMethod("GteF", typeof(bool), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.GreaterThanOrEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "GteF", 5.0f, 5.0f)); }
        [Fact]
        public void LessThanOrEqual_Float() { var type = BuildStaticMethod("LteF", typeof(bool), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.LessThanOrEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "LteF", 2.0f, 2.0f)); }
        [Fact]
        public void GreaterThanOrEqual_Long() { var type = BuildStaticMethod("GteL", typeof(bool), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.GreaterThanOrEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "GteL", 5L, 5L)); }
        [Fact]
        public void LessThanOrEqual_Double() { var type = BuildStaticMethod("LteD", typeof(bool), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.LessThanOrEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "LteD", 3.0, 3.0)); }

        // Compound assigns — various types
        [Fact]
        public void MultiplyAssign_Long() { var type = BuildStaticMethod("MaL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(5L))); m.Append(Expression.MultiplyAssign(v, Expression.Constant(3L))); m.Append(v); }); Assert.Equal(15L, InvokeStatic(type, "MaL")); }
        [Fact]
        public void DivideAssign_Long() { var type = BuildStaticMethod("DaL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(20L))); m.Append(Expression.DivideAssign(v, Expression.Constant(4L))); m.Append(v); }); Assert.Equal(5L, InvokeStatic(type, "DaL")); }
        [Fact]
        public void SubtractAssign_Double() { var type = BuildStaticMethod("SaD", typeof(double), m => { var v = Expression.Variable(typeof(double)); m.Append(Expression.Assign(v, Expression.Constant(10.0))); m.Append(Expression.SubtractAssign(v, Expression.Constant(3.0))); m.Append(v); }); Assert.Equal(7.0, InvokeStatic(type, "SaD")); }
        [Fact]
        public void AddAssign_Float() { var type = BuildStaticMethod("AaF", typeof(float), m => { var v = Expression.Variable(typeof(float)); m.Append(Expression.Assign(v, Expression.Constant(1.5f))); m.Append(Expression.AddAssign(v, Expression.Constant(2.5f))); m.Append(v); }); Assert.Equal(4.0f, InvokeStatic(type, "AaF")); }
        [Fact]
        public void MultiplyAssign_Float() { var type = BuildStaticMethod("MaF", typeof(float), m => { var v = Expression.Variable(typeof(float)); m.Append(Expression.Assign(v, Expression.Constant(3.0f))); m.Append(Expression.MultiplyAssign(v, Expression.Constant(4.0f))); m.Append(v); }); Assert.Equal(12.0f, InvokeStatic(type, "MaF")); }

        #endregion

        #region ConvertExpression — 更多类型转换

        [Fact]
        public void Convert_DoubleToLong() { var type = BuildStaticMethod("D2L", typeof(long), m => { var d = m.DefineParameter(typeof(double), "d"); m.Append(Expression.Convert(d, typeof(long))); }); Assert.Equal(42L, InvokeStatic(type, "D2L", 42.9)); }
        [Fact]
        public void Convert_FloatToInt() { var type = BuildStaticMethod("F2I", typeof(int), m => { var f = m.DefineParameter(typeof(float), "f"); m.Append(Expression.Convert(f, typeof(int))); }); Assert.Equal(42, InvokeStatic(type, "F2I", 42.5f)); }
        [Fact]
        public void Convert_FloatToLong() { var type = BuildStaticMethod("F2L", typeof(long), m => { var f = m.DefineParameter(typeof(float), "f"); m.Append(Expression.Convert(f, typeof(long))); }); Assert.Equal(42L, InvokeStatic(type, "F2L", 42.0f)); }
        [Fact]
        public void Convert_LongToFloat() { var type = BuildStaticMethod("L2F", typeof(float), m => { var l = m.DefineParameter(typeof(long), "l"); m.Append(Expression.Convert(l, typeof(float))); }); Assert.Equal(42.0f, InvokeStatic(type, "L2F", 42L)); }
        [Fact]
        public void Convert_IntToDouble() { var type = BuildStaticMethod("I2D", typeof(double), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(double))); }); Assert.Equal(42.0, InvokeStatic(type, "I2D", 42)); }
        [Fact]
        public void Convert_IntToSByte() { var type = BuildStaticMethod("I2SB", typeof(sbyte), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(sbyte))); }); Assert.Equal((sbyte)42, InvokeStatic(type, "I2SB", 42)); }
        [Fact]
        public void Convert_IntToChar() { var type = BuildStaticMethod("I2C", typeof(char), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(char))); }); Assert.Equal('A', InvokeStatic(type, "I2C", 65)); }
        [Fact]
        public void Convert_IntToUShort() { var type = BuildStaticMethod("I2US", typeof(ushort), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(ushort))); }); Assert.Equal((ushort)42, InvokeStatic(type, "I2US", 42)); }
        [Fact]
        public void Convert_LongToShort() { var type = BuildStaticMethod("L2S", typeof(short), m => { var l = m.DefineParameter(typeof(long), "l"); m.Append(Expression.Convert(l, typeof(short))); }); Assert.Equal((short)42, InvokeStatic(type, "L2S", 42L)); }
        [Fact]
        public void Convert_LongToByte() { var type = BuildStaticMethod("L2B", typeof(byte), m => { var l = m.DefineParameter(typeof(long), "l"); m.Append(Expression.Convert(l, typeof(byte))); }); Assert.Equal((byte)42, InvokeStatic(type, "L2B", 42L)); }
        [Fact]
        public void Convert_StringToObject() { var type = BuildStaticMethod("S2O", typeof(object), m => { var s = m.DefineParameter(typeof(string), "s"); m.Append(Expression.Convert(s, typeof(object))); }); Assert.Equal("hi", InvokeStatic(type, "S2O", "hi")); }
        [Fact]
        public void Convert_LongToUInt() { var type = BuildStaticMethod("L2UI", typeof(uint), m => { var l = m.DefineParameter(typeof(long), "l"); m.Append(Expression.Convert(l, typeof(uint))); }); Assert.Equal(42u, InvokeStatic(type, "L2UI", 42L)); }

        #endregion

        #region ConstantExpression — 更多值路径

        [Fact]
        public void Constant_Int2() { var type = BuildStaticMethod("I2", typeof(int), m => m.Append(Expression.Constant(2))); Assert.Equal(2, InvokeStatic(type, "I2")); }
        [Fact]
        public void Constant_Int3() { var type = BuildStaticMethod("I3", typeof(int), m => m.Append(Expression.Constant(3))); Assert.Equal(3, InvokeStatic(type, "I3")); }
        [Fact]
        public void Constant_Int4() { var type = BuildStaticMethod("I4", typeof(int), m => m.Append(Expression.Constant(4))); Assert.Equal(4, InvokeStatic(type, "I4")); }
        [Fact]
        public void Constant_Int5() { var type = BuildStaticMethod("I5", typeof(int), m => m.Append(Expression.Constant(5))); Assert.Equal(5, InvokeStatic(type, "I5")); }
        [Fact]
        public void Constant_Int6() { var type = BuildStaticMethod("I6", typeof(int), m => m.Append(Expression.Constant(6))); Assert.Equal(6, InvokeStatic(type, "I6")); }
        [Fact]
        public void Constant_Int7() { var type = BuildStaticMethod("I7", typeof(int), m => m.Append(Expression.Constant(7))); Assert.Equal(7, InvokeStatic(type, "I7")); }
        [Fact]
        public void Constant_Int127() { var type = BuildStaticMethod("I127", typeof(int), m => m.Append(Expression.Constant(127))); Assert.Equal(127, InvokeStatic(type, "I127")); }
        [Fact]
        public void Constant_IntNeg100() { var type = BuildStaticMethod("INeg100", typeof(int), m => m.Append(Expression.Constant(-100))); Assert.Equal(-100, InvokeStatic(type, "INeg100")); }
        [Fact]
        public void Constant_Long0() { var type = BuildStaticMethod("L0", typeof(long), m => m.Append(Expression.Constant(0L))); Assert.Equal(0L, InvokeStatic(type, "L0")); }
        [Fact]
        public void Constant_Long1() { var type = BuildStaticMethod("L1", typeof(long), m => m.Append(Expression.Constant(1L))); Assert.Equal(1L, InvokeStatic(type, "L1")); }
        [Fact]
        public void Constant_LongNeg1() { var type = BuildStaticMethod("LN1", typeof(long), m => m.Append(Expression.Constant(-1L))); Assert.Equal(-1L, InvokeStatic(type, "LN1")); }
        [Fact]
        public void Constant_LongSmall() { var type = BuildStaticMethod("LSm", typeof(long), m => m.Append(Expression.Constant(42L))); Assert.Equal(42L, InvokeStatic(type, "LSm")); }
        [Fact]
        public void Constant_Enum() { var type = BuildStaticMethod("CE", typeof(DayOfWeek), m => m.Append(Expression.Constant(DayOfWeek.Friday))); Assert.Equal(DayOfWeek.Friday, InvokeStatic(type, "CE")); }

        #endregion

        #region TypeIsExpression — 更多类型检查路径

        [Fact]
        public void TypeIs_String() { var type = BuildStaticMethod("IsStr", typeof(bool), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeIs(o, typeof(string))); }); Assert.True((bool)InvokeStatic(type, "IsStr", (object)"hello")); Assert.False((bool)InvokeStatic(type, "IsStr", (object)42)); }
        [Fact]
        public void TypeIs_List() { var type = BuildStaticMethod("IsLst", typeof(bool), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeIs(o, typeof(List<int>))); }); Assert.True((bool)InvokeStatic(type, "IsLst", (object)new List<int>())); Assert.False((bool)InvokeStatic(type, "IsLst", (object)"hello")); }
        [Fact]
        public void TypeIs_Double() { var type = BuildStaticMethod("IsDbl", typeof(bool), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeIs(o, typeof(double))); }); Assert.True((bool)InvokeStatic(type, "IsDbl", (object)3.14)); Assert.False((bool)InvokeStatic(type, "IsDbl", (object)"hello")); }
        [Fact]
        public void TypeIs_IEnumerable() { var type = BuildStaticMethod("IsIE", typeof(bool), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeIs(o, typeof(System.Collections.IEnumerable))); }); Assert.True((bool)InvokeStatic(type, "IsIE", (object)new int[0])); Assert.False((bool)InvokeStatic(type, "IsIE", (object)42)); }

        #endregion

        #region TypeAsExpression — 更多路径

        [Fact]
        public void TypeAs_List() { var type = BuildStaticMethod("AsList", typeof(List<int>), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeAs(o, typeof(List<int>))); }); Assert.NotNull(InvokeStatic(type, "AsList", (object)new List<int>())); Assert.Null(InvokeStatic(type, "AsList", (object)"hello")); }
        [Fact]
        public void TypeAs_NullableDouble() { var type = BuildStaticMethod("AsND", typeof(double?), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeAs(o, typeof(double?))); }); Assert.Equal((double?)3.14, InvokeStatic(type, "AsND", (object)3.14)); Assert.Null(InvokeStatic(type, "AsND", (object)"hello")); }

        #endregion

        #region UnaryExpression — 更多路径

        [Fact]
        public void Decrement_Int() { var type = BuildStaticMethod("DecI", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); m.Append(Expression.Decrement(a)); }); Assert.Equal(4, InvokeStatic(type, "DecI", 5)); }
        [Fact]
        public void Increment_Int() { var type = BuildStaticMethod("IncI", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); m.Append(Expression.Increment(a)); }); Assert.Equal(6, InvokeStatic(type, "IncI", 5)); }
        [Fact]
        public void DecrementAssign_Long() { var type = BuildStaticMethod("DecAL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(100L))); m.Append(Expression.DecrementAssign(v)); m.Append(v); }); Assert.Equal(99L, InvokeStatic(type, "DecAL")); }
        [Fact]
        public void IncrementAssign_Long() { var type = BuildStaticMethod("IncAL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(100L))); m.Append(Expression.IncrementAssign(v)); m.Append(v); }); Assert.Equal(101L, InvokeStatic(type, "IncAL")); }
        [Fact]
        public void DecrementAssign_Double() { var type = BuildStaticMethod("DecAD", typeof(double), m => { var v = Expression.Variable(typeof(double)); m.Append(Expression.Assign(v, Expression.Constant(5.5))); m.Append(Expression.DecrementAssign(v)); m.Append(v); }); Assert.Equal(4.5, InvokeStatic(type, "DecAD")); }
        [Fact]
        public void IncrementAssign_Double() { var type = BuildStaticMethod("IncAD", typeof(double), m => { var v = Expression.Variable(typeof(double)); m.Append(Expression.Assign(v, Expression.Constant(5.5))); m.Append(Expression.IncrementAssign(v)); m.Append(v); }); Assert.Equal(6.5, InvokeStatic(type, "IncAD")); }

        #endregion

        #region MemberInitExpression — 属性绑定

        [Fact]
        public void MemberInit_PropertyBinding()
        {
            var p1 = typeof(CoverageTarget).GetProperty(nameof(CoverageTarget.Name));
            var type = BuildStaticMethod("MIP", typeof(CoverageTarget), m =>
            {
                m.Append(Expression.MemberInit(Expression.New(typeof(CoverageTarget)),
                    Expression.Bind(p1, Expression.Constant("bound"))));
            });
            var result = (CoverageTarget)InvokeStatic(type, "MIP");
            Assert.Equal("bound", result.Name);
        }

        #endregion

        #region TryExpression — 更多路径

        [Fact]
        public void Try_CatchAndRethrow()
        {
            var type = BuildStaticMethod("TCR", typeof(int), m =>
            {
                var r = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(r, Expression.Constant(0)));
                var t = Expression.Try();
                t.Append(Expression.Throw(typeof(InvalidOperationException)));
                t.Catch(typeof(Exception)).Append(Expression.Assign(r, Expression.Constant(99)));
                m.Append(t);
                m.Append(r);
            });
            Assert.Equal(99, InvokeStatic(type, "TCR"));
        }

        #endregion

        #region SwitchExpression — 更多路径

        [Fact]
        public void Switch_MultipleCases()
        {
            var type = BuildStaticMethod("SMC", typeof(void), m =>
            {
                var val = m.DefineParameter(typeof(int), "val");
                var r = Expression.Variable(typeof(string));
                var sw = Expression.Switch(val, Expression.Assign(r, Expression.Constant("default")));
                sw.Case(Expression.Constant(10)).Append(Expression.Assign(r, Expression.Constant("ten")));
                sw.Case(Expression.Constant(20)).Append(Expression.Assign(r, Expression.Constant("twenty")));
                sw.Case(Expression.Constant(30)).Append(Expression.Assign(r, Expression.Constant("thirty")));
                sw.Case(Expression.Constant(40)).Append(Expression.Assign(r, Expression.Constant("forty")));
                sw.Case(Expression.Constant(50)).Append(Expression.Assign(r, Expression.Constant("fifty")));
                m.Append(sw);
            });
            InvokeStatic(type, "SMC", 10);
            InvokeStatic(type, "SMC", 20);
            InvokeStatic(type, "SMC", 30);
            InvokeStatic(type, "SMC", 40);
            InvokeStatic(type, "SMC", 50);
            InvokeStatic(type, "SMC", 99);
        }

        #endregion

        #region ReturnExpression — 条件返回

        [Fact]
        public void Return_ConditionalEarly()
        {
            var type = BuildStaticMethod("RCE", typeof(int), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.IfThenElse(flag, Expression.Return(Expression.Constant(1)), Expression.Return(Expression.Constant(2))));
                m.Append(Expression.Constant(0));
            });
            Assert.Equal(1, InvokeStatic(type, "RCE", true));
            Assert.Equal(2, InvokeStatic(type, "RCE", false));
        }

        #endregion

        #region MethodCallExpression — 实例方法

        [Fact]
        public void Call_StringContains()
        {
            var mi = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
            var type = BuildStaticMethod("CC", typeof(bool), m =>
            {
                var s = m.DefineParameter(typeof(string), "s");
                var sub = m.DefineParameter(typeof(string), "sub");
                m.Append(Expression.Call(s, mi, sub));
            });
            Assert.True((bool)InvokeStatic(type, "CC", "hello world", "world"));
            Assert.False((bool)InvokeStatic(type, "CC", "hello world", "xyz"));
        }

        [Fact]
        public void Call_ListAdd()
        {
            var mi = typeof(List<int>).GetMethod(nameof(List<int>.Add));
            var type = BuildStaticMethod("CLA", typeof(void), m =>
            {
                var list = m.DefineParameter(typeof(List<int>), "list");
                var val = m.DefineParameter(typeof(int), "val");
                m.Append(Expression.Call(list, mi, val));
            });
            var lst = new List<int>();
            InvokeStatic(type, "CLA", lst, 42);
            Assert.Single(lst);
            Assert.Equal(42, lst[0]);
        }

        [Fact]
        public void Call_Static_IntParse()
        {
            var mi = typeof(int).GetMethod(nameof(int.Parse), new[] { typeof(string) });
            var type = BuildStaticMethod("CIP", typeof(int), m =>
            {
                var s = m.DefineParameter(typeof(string), "s");
                m.Append(Expression.Call(mi, s));
            });
            Assert.Equal(42, InvokeStatic(type, "CIP", "42"));
        }

        #endregion

        #region PropertyExpression — 更多路径

        [Fact]
        public void Property_StringLength()
        {
            var pi = typeof(string).GetProperty(nameof(string.Length));
            var type = BuildStaticMethod("SL", typeof(int), m =>
            {
                var s = m.DefineParameter(typeof(string), "s");
                m.Append(Expression.Property(s, pi));
            });
            Assert.Equal(5, InvokeStatic(type, "SL", "hello"));
        }

        [Fact]
        public void Property_ListCount()
        {
            var pi = typeof(List<int>).GetProperty(nameof(List<int>.Count));
            var type = BuildStaticMethod("LC", typeof(int), m =>
            {
                var l = m.DefineParameter(typeof(List<int>), "l");
                m.Append(Expression.Property(l, pi));
            });
            Assert.Equal(3, InvokeStatic(type, "LC", new List<int> { 1, 2, 3 }));
        }

        #endregion

        #region ArrayExpression — 更多路径

        [Fact]
        public void ArrayIndex_ReadObject()
        {
            var type = BuildStaticMethod("ARO", typeof(object), m =>
            {
                var arr = m.DefineParameter(typeof(object[]), "arr");
                var idx = m.DefineParameter(typeof(int), "idx");
                m.Append(Expression.ArrayIndex(arr, idx));
            });
            Assert.Equal("hello", InvokeStatic(type, "ARO", new object[] { "hello", 42 }, 0));
            Assert.Equal(42, InvokeStatic(type, "ARO", new object[] { "hello", 42 }, 1));
        }

        [Fact]
        public void NewArray_DynamicSize()
        {
            var type = BuildStaticMethod("NAS", typeof(int[]), m =>
            {
                m.Append(Expression.NewArray(5, typeof(int)));
            });
            Assert.Equal(5, ((int[])InvokeStatic(type, "NAS")).Length);
        }

        #endregion

        #region AbstractTypeEmitter — 泛型参数

        [Fact]
        public void GenericType_Definition()
        {
            var te = _emitter.DefineType($"GT_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(IComparable) });
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            var gm = te.DefineMethod("CompareTo", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int));
            gm.DefineParameter(typeof(object), ParameterAttributes.None, "obj");
            gm.Append(Expression.Constant(0));
            var t = te.CreateType();
            Assert.True(typeof(IComparable).IsAssignableFrom(t));
        }

        #endregion

        #region ModuleEmitter — 多个类型

        [Fact]
        public void ModuleEmitter_ComplexType()
        {
            var mod = new ModuleEmitter($"MCT_{Guid.NewGuid():N}");
            var te = mod.DefineType($"CT_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f1 = te.DefineField("X", typeof(int), FieldAttributes.Public);
            var f2 = te.DefineField("Y", typeof(string), FieldAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            var p1 = ct.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            var p2 = ct.DefineParameter(typeof(string), ParameterAttributes.None, "y");
            ct.Append(Expression.Assign(f1, p1));
            ct.Append(Expression.Assign(f2, p2));
            var gx = te.DefineMethod("GetX", MethodAttributes.Public, typeof(int)); gx.Append(f1);
            var gy = te.DefineMethod("GetY", MethodAttributes.Public, typeof(string)); gy.Append(f2);
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t, 42, "hello");
            Assert.Equal(42, t.GetMethod("GetX").Invoke(inst, null));
            Assert.Equal("hello", t.GetMethod("GetY").Invoke(inst, null));
        }

        #endregion

        #region FieldExpression — 更多路径

        [Fact]
        public void Field_ConstantRead()
        {
            var fi = typeof(string).GetField(nameof(string.Empty));
            var type = BuildStaticMethod("FCR", typeof(string), m => m.Append(Expression.Field(fi)));
            Assert.Equal(string.Empty, InvokeStatic(type, "FCR"));
        }

        #endregion

        #region NewArray — 动态大小

        [Fact]
        public void NewArray_ExpressionSize()
        {
            var type = BuildStaticMethod("NAE", typeof(string[]), m =>
            {
                m.Append(Expression.NewArray(3, typeof(string)));
            });
            Assert.Equal(3, ((string[])InvokeStatic(type, "NAE")).Length);
        }

        #endregion
    }
}
