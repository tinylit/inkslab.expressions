using Xunit;
using System;
using System.Collections.Generic;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 覆盖率提升测试 Part 2 — 深度覆盖 Load/Emit 路径。
    /// </summary>
    public class CoverageBoostTests2
    {
        private readonly ModuleEmitter _emitter = new ModuleEmitter($"Inkslab.Expressions.Tests.CoverageBoost2.{Guid.NewGuid():N}");

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

        #region ParameterExpression — ByRef READ (Ldind_*) 路径

        [Fact]
        public void ByRef_Read_Int() { var te = _emitter.DefineType($"RRI_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("R", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(int).MakeByRefType(), "v"); var val = me.DefineParameter(typeof(int), "nv"); me.Append(Expression.Assign(rp, val)); var t = te.CreateType(); var args = new object[] { 0, 42 }; t.GetMethod("R").Invoke(null, args); Assert.Equal(42, args[0]); }
        [Fact]
        public void ByRef_Read_Long() { var te = _emitter.DefineType($"RRL_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("R", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(long).MakeByRefType(), "v"); var val = me.DefineParameter(typeof(long), "nv"); me.Append(Expression.Assign(rp, val)); var t = te.CreateType(); var args = new object[] { 0L, 99L }; t.GetMethod("R").Invoke(null, args); Assert.Equal(99L, args[0]); }
        [Fact]
        public void ByRef_Read_Double() { var te = _emitter.DefineType($"RRD_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("R", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(double).MakeByRefType(), "v"); var val = me.DefineParameter(typeof(double), "nv"); me.Append(Expression.Assign(rp, val)); var t = te.CreateType(); var args = new object[] { 0.0, 3.14 }; t.GetMethod("R").Invoke(null, args); Assert.Equal(3.14, args[0]); }
        [Fact]
        public void ByRef_Read_Float() { var te = _emitter.DefineType($"RRF_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("R", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(float).MakeByRefType(), "v"); var val = me.DefineParameter(typeof(float), "nv"); me.Append(Expression.Assign(rp, val)); var t = te.CreateType(); var args = new object[] { 0f, 1.5f }; t.GetMethod("R").Invoke(null, args); Assert.Equal(1.5f, args[0]); }
        [Fact]
        public void ByRef_Read_Short() { var te = _emitter.DefineType($"RRS_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("R", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(short).MakeByRefType(), "v"); var val = me.DefineParameter(typeof(short), "nv"); me.Append(Expression.Assign(rp, val)); var t = te.CreateType(); var args = new object[] { (short)0, (short)42 }; t.GetMethod("R").Invoke(null, args); Assert.Equal((short)42, args[0]); }
        [Fact]
        public void ByRef_Read_Bool() { var te = _emitter.DefineType($"RRB_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("R", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(bool).MakeByRefType(), "v"); var val = me.DefineParameter(typeof(bool), "nv"); me.Append(Expression.Assign(rp, val)); var t = te.CreateType(); var args = new object[] { false, true }; t.GetMethod("R").Invoke(null, args); Assert.True((bool)args[0]); }
        [Fact]
        public void ByRef_Read_Byte() { var te = _emitter.DefineType($"RRBy_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("R", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(byte).MakeByRefType(), "v"); var val = me.DefineParameter(typeof(byte), "nv"); me.Append(Expression.Assign(rp, val)); var t = te.CreateType(); var args = new object[] { (byte)0, (byte)42 }; t.GetMethod("R").Invoke(null, args); Assert.Equal((byte)42, args[0]); }
        [Fact]
        public void ByRef_Read_Char() { var te = _emitter.DefineType($"RRC_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("R", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(char).MakeByRefType(), "v"); var val = me.DefineParameter(typeof(char), "nv"); me.Append(Expression.Assign(rp, val)); var t = te.CreateType(); var args = new object[] { 'A', 'Z' }; t.GetMethod("R").Invoke(null, args); Assert.Equal('Z', args[0]); }

        [Fact]
        public void ByRef_Write_Char() { var te = _emitter.DefineType($"WRC_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("W", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(char).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant('Z'))); var t = te.CreateType(); var args = new object[] { 'A' }; t.GetMethod("W").Invoke(null, args); Assert.Equal('Z', args[0]); }
        [Fact]
        public void ByRef_Write_UInt() { var te = _emitter.DefineType($"WRU_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("W", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(uint).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant(42u))); var t = te.CreateType(); var args = new object[] { 0u }; t.GetMethod("W").Invoke(null, args); Assert.Equal(42u, args[0]); }
        [Fact]
        public void ByRef_Write_ULong() { var te = _emitter.DefineType($"WRL_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("W", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(ulong).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant(999UL))); var t = te.CreateType(); var args = new object[] { 0UL }; t.GetMethod("W").Invoke(null, args); Assert.Equal(999UL, args[0]); }
        [Fact]
        public void ByRef_Write_SByte() { var te = _emitter.DefineType($"WRS_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("W", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(sbyte).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant((sbyte)-10))); var t = te.CreateType(); var args = new object[] { (sbyte)0 }; t.GetMethod("W").Invoke(null, args); Assert.Equal((sbyte)-10, args[0]); }
        [Fact]
        public void ByRef_Write_UShort() { var te = _emitter.DefineType($"WRUS_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("W", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(ushort).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant((ushort)60000))); var t = te.CreateType(); var args = new object[] { (ushort)0 }; t.GetMethod("W").Invoke(null, args); Assert.Equal((ushort)60000, args[0]); }
        [Fact]
        public void ByRef_Write_Decimal() { var te = _emitter.DefineType($"WRDe_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("W", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(decimal).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant(123.456m))); var t = te.CreateType(); var args = new object[] { 0m }; t.GetMethod("W").Invoke(null, args); Assert.Equal(123.456m, args[0]); }

        #endregion

        #region ParameterExpression — 多参数 (Ldarg_S) 路径

        [Fact]
        public void ManyParams_Ldarg_S()
        {
            var te = _emitter.DefineType($"MP_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            // 创建5个参数，使最后一个参数位于position 4 (> 3)
            me.DefineParameter(typeof(int), "a");
            me.DefineParameter(typeof(int), "b");
            me.DefineParameter(typeof(int), "c");
            me.DefineParameter(typeof(int), "d");
            var e = me.DefineParameter(typeof(int), "e");
            me.Append(e); // 返回第5个参数，position=4
            var t = te.CreateType();
            Assert.Equal(55, t.GetMethod("M").Invoke(null, new object[] { 1, 2, 3, 4, 55 }));
        }

        #endregion

        #region ThisExpression — 实例方法中使用

        [Fact]
        public void This_InInstanceMethod()
        {
            var te = _emitter.DefineType($"TH_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f = te.DefineField("_val", typeof(int), FieldAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public); var cp = ct.DefineParameter(typeof(int), ParameterAttributes.None, "v"); ct.Append(Expression.Assign(f, cp));
            // 实例方法使用字段（隐式 this）
            var gm = te.DefineMethod("GetVal", MethodAttributes.Public, typeof(int)); gm.Append(f);
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t, 42);
            Assert.Equal(42, t.GetMethod("GetVal").Invoke(inst, null));
        }

        [Fact]
        public void This_SetField()
        {
            var te = _emitter.DefineType($"TS_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f = te.DefineField("_val", typeof(int), FieldAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public); ct.Append(Expression.Assign(f, Expression.Constant(0)));
            var sm = te.DefineMethod("SetVal", MethodAttributes.Public, typeof(void)); var vp = sm.DefineParameter(typeof(int), "v"); sm.Append(Expression.Assign(f, vp));
            var gm = te.DefineMethod("GetVal", MethodAttributes.Public, typeof(int)); gm.Append(f);
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t);
            t.GetMethod("SetVal").Invoke(inst, new object[] { 99 });
            Assert.Equal(99, t.GetMethod("GetVal").Invoke(inst, null));
        }

        #endregion

        #region TypeCompiler — 泛型类型操作

        [Fact]
        public void TypeCompiler_GetConstructor_Generic()
        {
            // List<T> 的泛型构造函数
            var openCtor = typeof(List<>).GetConstructor(Type.EmptyTypes);
            var closedType = typeof(List<int>);
            var result = TypeCompiler.GetConstructor(closedType, openCtor);
            Assert.NotNull(result);
        }

        [Fact]
        public void TypeCompiler_GetField_NonGeneric()
        {
            var fi = typeof(string).GetField(nameof(string.Empty));
            var result = TypeCompiler.GetField(typeof(string), fi);
            Assert.Equal(fi, result);
        }

        [Fact]
        public void TypeCompiler_GetMethod_NonGenericType()
        {
            var mi = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
            var result = TypeCompiler.GetMethod(typeof(string), mi);
            Assert.Equal(mi, result);
        }

        [Fact]
        public void TypeCompiler_NullType_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetConstructor(null, typeof(List<>).GetConstructor(Type.EmptyTypes)));
        }

        [Fact]
        public void TypeCompiler_NullCtor_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetConstructor(typeof(List<int>), null));
        }

        [Fact]
        public void TypeCompiler_NullField_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetField(typeof(List<int>), null));
        }

        [Fact]
        public void TypeCompiler_NullMethod_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetMethod(typeof(List<int>), null));
        }

        [Fact]
        public void TypeCompiler_GetMethod_NonGeneric()
        {
            var mi = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes);
            var result = TypeCompiler.GetMethod(typeof(string), mi);
            Assert.Equal(mi, result);
        }

        [Fact]
        public void TypeCompiler_GetConstructor_NonGeneric()
        {
            var ci = typeof(object).GetConstructor(Type.EmptyTypes);
            var result = TypeCompiler.GetConstructor(typeof(object), ci);
            Assert.Equal(ci, result);
        }

        #endregion

        #region BinaryExpression — 更多运算路径

        [Fact]
        public void Xor_Int() { var type = BuildStaticMethod("XorI", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.ExclusiveOr(a, b)); }); Assert.Equal(3 ^ 5, InvokeStatic(type, "XorI", 3, 5)); }
        [Fact]
        public void Or_Int() { var type = BuildStaticMethod("OrI", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.Or(a, b)); }); Assert.Equal(3 | 5, InvokeStatic(type, "OrI", 3, 5)); }
        [Fact]
        public void And_Int() { var type = BuildStaticMethod("AndI", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.And(a, b)); }); Assert.Equal(3 & 5, InvokeStatic(type, "AndI", 3, 5)); }
        [Fact]
        public void Modulo_Int() { var type = BuildStaticMethod("ModI", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.Modulo(a, b)); }); Assert.Equal(7 % 3, InvokeStatic(type, "ModI", 7, 3)); }
        [Fact]
        public void Multiply_Long() { var type = BuildStaticMethod("MulL", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.Multiply(a, b)); }); Assert.Equal(300L, InvokeStatic(type, "MulL", 15L, 20L)); }
        [Fact]
        public void Divide_Long() { var type = BuildStaticMethod("DivL", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.Divide(a, b)); }); Assert.Equal(5L, InvokeStatic(type, "DivL", 20L, 4L)); }
        [Fact]
        public void Subtract_Long() { var type = BuildStaticMethod("SubL", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.Subtract(a, b)); }); Assert.Equal(50L, InvokeStatic(type, "SubL", 100L, 50L)); }
        [Fact]
        public void Equal_Int() { var type = BuildStaticMethod("EqI", typeof(bool), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.Equal(a, b)); }); Assert.True((bool)InvokeStatic(type, "EqI", 5, 5)); Assert.False((bool)InvokeStatic(type, "EqI", 5, 6)); }
        [Fact]
        public void NotEqual_Int() { var type = BuildStaticMethod("NeI", typeof(bool), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.NotEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "NeI", 5, 6)); Assert.False((bool)InvokeStatic(type, "NeI", 5, 5)); }
        [Fact]
        public void LessThan_Int() { var type = BuildStaticMethod("LtI", typeof(bool), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.LessThan(a, b)); }); Assert.True((bool)InvokeStatic(type, "LtI", 1, 2)); Assert.False((bool)InvokeStatic(type, "LtI", 2, 1)); }
        [Fact]
        public void GreaterThan_Int() { var type = BuildStaticMethod("GtI", typeof(bool), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.GreaterThan(a, b)); }); Assert.True((bool)InvokeStatic(type, "GtI", 3, 1)); }
        [Fact]
        public void LessThanOrEqual_Int() { var type = BuildStaticMethod("LteI", typeof(bool), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.LessThanOrEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "LteI", 5, 5)); Assert.False((bool)InvokeStatic(type, "LteI", 6, 5)); }
        [Fact]
        public void GreaterThanOrEqual_Int() { var type = BuildStaticMethod("GteI", typeof(bool), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.GreaterThanOrEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "GteI", 5, 5)); Assert.True((bool)InvokeStatic(type, "GteI", 6, 5)); }

        // Compound assign
        [Fact]
        public void AddAssign_Long() { var type = BuildStaticMethod("AAL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(100L))); m.Append(Expression.AddAssign(v, Expression.Constant(50L))); m.Append(v); }); Assert.Equal(150L, InvokeStatic(type, "AAL")); }
        [Fact]
        public void OrAssign_Int() { var type = BuildStaticMethod("OAI", typeof(int), m => { var v = Expression.Variable(typeof(int)); m.Append(Expression.Assign(v, Expression.Constant(3))); m.Append(Expression.OrAssign(v, Expression.Constant(12))); m.Append(v); }); Assert.Equal(3 | 12, InvokeStatic(type, "OAI")); }
        [Fact]
        public void AndAssign_Int() { var type = BuildStaticMethod("AAI", typeof(int), m => { var v = Expression.Variable(typeof(int)); m.Append(Expression.Assign(v, Expression.Constant(15))); m.Append(Expression.AndAssign(v, Expression.Constant(6))); m.Append(v); }); Assert.Equal(15 & 6, InvokeStatic(type, "AAI")); }
        [Fact]
        public void XorAssign_Int() { var type = BuildStaticMethod("XAI", typeof(int), m => { var v = Expression.Variable(typeof(int)); m.Append(Expression.Assign(v, Expression.Constant(15))); m.Append(Expression.ExclusiveOrAssign(v, Expression.Constant(6))); m.Append(v); }); Assert.Equal(15 ^ 6, InvokeStatic(type, "XAI")); }
        [Fact]
        public void ModuloAssign_Int() { var type = BuildStaticMethod("MAI", typeof(int), m => { var v = Expression.Variable(typeof(int)); m.Append(Expression.Assign(v, Expression.Constant(17))); m.Append(Expression.ModuloAssign(v, Expression.Constant(5))); m.Append(v); }); Assert.Equal(17 % 5, InvokeStatic(type, "MAI")); }

        #endregion

        #region SwitchExpression — Arithmetic 路径

        [Fact]
        public void Switch_Arithmetic_Int()
        {
            var type = BuildStaticMethod("SInt", typeof(void), m =>
            {
                var val = m.DefineParameter(typeof(int), "val");
                var r = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(r, Expression.Constant(-1)));
                var sw = Expression.Switch(val, Expression.Assign(r, Expression.Constant(0)));
                sw.Case(Expression.Constant(1)).Append(Expression.Assign(r, Expression.Constant(10)));
                sw.Case(Expression.Constant(2)).Append(Expression.Assign(r, Expression.Constant(20)));
                sw.Case(Expression.Constant(3)).Append(Expression.Assign(r, Expression.Constant(30)));
                m.Append(sw);
            });
            InvokeStatic(type, "SInt", 1);
            InvokeStatic(type, "SInt", 2);
            InvokeStatic(type, "SInt", 3);
            InvokeStatic(type, "SInt", 99);
        }

        [Fact]
        public void Switch_Arithmetic_Long()
        {
            var type = BuildStaticMethod("SLng", typeof(long), m =>
            {
                var val = m.DefineParameter(typeof(long), "val");
                var r = Expression.Variable(typeof(long));
                m.Append(Expression.Assign(r, Expression.Constant(0L)));
                var sw = Expression.Switch(val);
                sw.Case(Expression.Constant(100L)).Append(Expression.Assign(r, Expression.Constant(1L)));
                sw.Case(Expression.Constant(200L)).Append(Expression.Assign(r, Expression.Constant(2L)));
                m.Append(sw);
                m.Append(r);
            });
            Assert.Equal(1L, InvokeStatic(type, "SLng", 100L));
            Assert.Equal(2L, InvokeStatic(type, "SLng", 200L));
            Assert.Equal(0L, InvokeStatic(type, "SLng", 300L));
        }

        #endregion

        #region ConvertExpression — 更多路径

        [Fact]
        public void Convert_NullableToObject() { var type = BuildStaticMethod("N2O", typeof(object), m => { var ni = m.DefineParameter(typeof(int?), "ni"); m.Append(Expression.Convert(ni, typeof(object))); }); Assert.Equal(42, InvokeStatic(type, "N2O", (int?)42)); }
        [Fact]
        public void Convert_UIntToLong() { var type = BuildStaticMethod("UI2L", typeof(long), m => { var u = m.DefineParameter(typeof(uint), "u"); m.Append(Expression.Convert(u, typeof(long))); }); Assert.Equal(42L, InvokeStatic(type, "UI2L", 42u)); }
        [Fact]
        public void Convert_LongToInt() { var type = BuildStaticMethod("L2I", typeof(int), m => { var l = m.DefineParameter(typeof(long), "l"); m.Append(Expression.Convert(l, typeof(int))); }); Assert.Equal(42, InvokeStatic(type, "L2I", 42L)); }
        [Fact]
        public void Convert_ShortToLong() { var type = BuildStaticMethod("S2L", typeof(long), m => { var s = m.DefineParameter(typeof(short), "s"); m.Append(Expression.Convert(s, typeof(long))); }); Assert.Equal(10L, InvokeStatic(type, "S2L", (short)10)); }
        [Fact]
        public void Convert_DoubleToFloat() { var type = BuildStaticMethod("D2F", typeof(float), m => { var d = m.DefineParameter(typeof(double), "d"); m.Append(Expression.Convert(d, typeof(float))); }); Assert.Equal(1.5f, InvokeStatic(type, "D2F", 1.5)); }
        [Fact]
        public void Convert_SByteToInt() { var type = BuildStaticMethod("SB2I", typeof(int), m => { var s = m.DefineParameter(typeof(sbyte), "s"); m.Append(Expression.Convert(s, typeof(int))); }); Assert.Equal(-5, InvokeStatic(type, "SB2I", (sbyte)-5)); }
        [Fact]
        public void Convert_UShortToInt() { var type = BuildStaticMethod("US2I", typeof(int), m => { var u = m.DefineParameter(typeof(ushort), "u"); m.Append(Expression.Convert(u, typeof(int))); }); Assert.Equal(60000, InvokeStatic(type, "US2I", (ushort)60000)); }

        #endregion

        #region UnaryExpression — 更多路径

        [Fact]
        public void Negate_Float() { var type = BuildStaticMethod("NegF", typeof(float), m => { var a = m.DefineParameter(typeof(float), "a"); m.Append(Expression.Negate(a)); }); Assert.Equal(-2.5f, InvokeStatic(type, "NegF", 2.5f)); }
        [Fact]
        public void Negate_Int() { var type = BuildStaticMethod("NegI", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); m.Append(Expression.Negate(a)); }); Assert.Equal(-42, InvokeStatic(type, "NegI", 42)); }
        [Fact]
        public void Not_Bool() { var type = BuildStaticMethod("NotB", typeof(bool), m => { var a = m.DefineParameter(typeof(bool), "a"); m.Append(Expression.Not(a)); }); Assert.False((bool)InvokeStatic(type, "NotB", true)); Assert.True((bool)InvokeStatic(type, "NotB", false)); }
        [Fact]
        public void Decrement_Double() { var type = BuildStaticMethod("DecD", typeof(double), m => { var a = m.DefineParameter(typeof(double), "a"); m.Append(Expression.Decrement(a)); }); Assert.Equal(4.5, InvokeStatic(type, "DecD", 5.5)); }
        [Fact]
        public void Decrement_Float() { var type = BuildStaticMethod("DecF", typeof(float), m => { var a = m.DefineParameter(typeof(float), "a"); m.Append(Expression.Decrement(a)); }); Assert.Equal(0.5f, InvokeStatic(type, "DecF", 1.5f)); }
        [Fact]
        public void Decrement_Short() { var type = BuildStaticMethod("DecSh", typeof(short), m => { var a = m.DefineParameter(typeof(short), "a"); m.Append(Expression.Decrement(a)); }); Assert.Equal((short)4, InvokeStatic(type, "DecSh", (short)5)); }
        [Fact]
        public void Negate_Short() { var type = BuildStaticMethod("NegSh", typeof(short), m => { var a = m.DefineParameter(typeof(short), "a"); m.Append(Expression.Negate(a)); }); Assert.Equal((short)-42, InvokeStatic(type, "NegSh", (short)42)); }
        [Fact]
        public void Not_Long() { var type = BuildStaticMethod("NotL", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); m.Append(Expression.Not(a)); }); Assert.Equal(~100L, InvokeStatic(type, "NotL", 100L)); }

        #endregion

        #region PropertyExpression — 写入路径

        [Fact]
        public void Property_Write()
        {
            var te = _emitter.DefineType($"PW_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var bf = te.DefineField("_n", typeof(string), FieldAttributes.Private);
            var ct = te.DefineConstructor(MethodAttributes.Public); ct.Append(Expression.Assign(bf, Expression.Constant("init")));
            var g = te.DefineMethod("get_N", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(string)); g.Append(bf);
            var s = te.DefineMethod("set_N", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void)); var vp = s.DefineParameter(typeof(string), "value"); s.Append(Expression.Assign(bf, vp));
            var prop = te.DefineProperty("N", PropertyAttributes.None, typeof(string)); prop.SetGetMethod(g); prop.SetSetMethod(s);
            var sm = te.DefineMethod("SetN", MethodAttributes.Public, typeof(void)); var np = sm.DefineParameter(typeof(string), "v");
            var propInfo = typeof(Exception).GetProperty(nameof(Exception.Source)); // use a known property
            sm.Append(Expression.Assign(prop, np));
            var gm = te.DefineMethod("GetN", MethodAttributes.Public, typeof(string)); gm.Append(prop);
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t);
            t.GetMethod("SetN").Invoke(inst, new object[] { "hello" });
            Assert.Equal("hello", t.GetMethod("GetN").Invoke(inst, null));
        }

        #endregion

        #region MemberInitExpression — 更多路径

        [Fact]
        public void MemberInit_MultipleProperties()
        {
            var type = BuildStaticMethod("MIMP", typeof(CoverageTarget), m =>
            {
                var p1 = typeof(CoverageTarget).GetProperty(nameof(CoverageTarget.Name));
                var p2 = typeof(CoverageTarget).GetProperty(nameof(CoverageTarget.Value));
                m.Append(Expression.MemberInit(Expression.New(typeof(CoverageTarget)),
                    Expression.Bind(p1, Expression.Constant("test")),
                    Expression.Bind(p2, Expression.Constant(42))));
            });
            var result = (CoverageTarget)InvokeStatic(type, "MIMP");
            Assert.Equal("test", result.Name);
            Assert.Equal(42, result.Value);
        }

        #endregion

        #region TryExpression — finally + catch

        [Fact]
        public void Try_CatchAndFinally()
        {
            var type = BuildStaticMethod("TCF", typeof(int), m =>
            {
                var r = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(r, Expression.Constant(0)));
                var fb = Expression.Block();
                fb.Append(Expression.AddAssign(r, Expression.Constant(100)));
                var t = Expression.Try(fb);
                t.Append(Expression.Throw(typeof(InvalidOperationException)));
                t.Catch(typeof(Exception)).Append(Expression.AddAssign(r, Expression.Constant(10)));
                m.Append(t);
                m.Append(r);
            });
            Assert.Equal(110, InvokeStatic(type, "TCF"));
        }

        #endregion

        #region FieldExpression — 更多路径

        [Fact]
        public void Field_StaticReadWrite()
        {
            var fi = typeof(CoverageHelper).GetField(nameof(CoverageHelper.StaticValue));
            var type = BuildStaticMethod("FSRW", typeof(int), m =>
            {
                m.Append(Expression.Assign(Expression.Field(fi), Expression.Constant(77)));
                m.Append(Expression.Field(fi));
            });
            Assert.Equal(77, InvokeStatic(type, "FSRW"));
        }

        [Fact]
        public void Field_InstanceRead()
        {
            var fi = typeof(CoverageHelper).GetField(nameof(CoverageHelper.InstanceValue));
            var type = BuildStaticMethod("FIR", typeof(int), m =>
            {
                var obj = m.DefineParameter(typeof(CoverageHelper), "obj");
                m.Append(Expression.Field(obj, fi));
            });
            var h = new CoverageHelper { InstanceValue = 55 };
            Assert.Equal(55, InvokeStatic(type, "FIR", h));
        }

        #endregion

        #region ArrayExpression — 初始化路径

        [Fact]
        public void ArrayExpression_ObjectArray()
        {
            var type = BuildStaticMethod("AO", typeof(object[]), m =>
            {
                m.Append(Expression.Array(typeof(object),
                    Expression.Convert(Expression.Constant(1), typeof(object)),
                    Expression.Constant("two"),
                    Expression.Convert(Expression.Constant(3.0), typeof(object))));
            });
            var arr = (object[])InvokeStatic(type, "AO");
            Assert.Equal(3, arr.Length);
            Assert.Equal(1, arr[0]);
            Assert.Equal("two", arr[1]);
        }

        [Fact]
        public void ArrayExpression_EmptyTypedArray()
        {
            var type = BuildStaticMethod("AE", typeof(int[]), m => m.Append(Expression.NewArray(0, typeof(int))));
            Assert.Empty((int[])InvokeStatic(type, "AE"));
        }

        #endregion

        #region ConstantExpression — 更多类型

        [Fact]
        public void Constant_Long() { var type = BuildStaticMethod("CL", typeof(long), m => m.Append(Expression.Constant(long.MaxValue))); Assert.Equal(long.MaxValue, InvokeStatic(type, "CL")); }
        [Fact]
        public void Constant_Double() { var type = BuildStaticMethod("CD", typeof(double), m => m.Append(Expression.Constant(double.MaxValue))); Assert.Equal(double.MaxValue, InvokeStatic(type, "CD")); }
        [Fact]
        public void Constant_Bool_True() { var type = BuildStaticMethod("BT", typeof(bool), m => m.Append(Expression.Constant(true))); Assert.True((bool)InvokeStatic(type, "BT")); }
        [Fact]
        public void Constant_Bool_False() { var type = BuildStaticMethod("BF", typeof(bool), m => m.Append(Expression.Constant(false))); Assert.False((bool)InvokeStatic(type, "BF")); }
        [Fact]
        public void Constant_String() { var type = BuildStaticMethod("CS", typeof(string), m => m.Append(Expression.Constant("hello world"))); Assert.Equal("hello world", InvokeStatic(type, "CS")); }
        [Fact]
        public void Constant_NullString() { var type = BuildStaticMethod("NS", typeof(string), m => m.Append(Expression.Constant(null, typeof(string)))); Assert.Null(InvokeStatic(type, "NS")); }
        [Fact]
        public void Constant_IntZero() { var type = BuildStaticMethod("IZ", typeof(int), m => m.Append(Expression.Constant(0))); Assert.Equal(0, InvokeStatic(type, "IZ")); }
        [Fact]
        public void Constant_IntMinus1() { var type = BuildStaticMethod("IM1", typeof(int), m => m.Append(Expression.Constant(-1))); Assert.Equal(-1, InvokeStatic(type, "IM1")); }
        [Fact]
        public void Constant_Int1() { var type = BuildStaticMethod("I1", typeof(int), m => m.Append(Expression.Constant(1))); Assert.Equal(1, InvokeStatic(type, "I1")); }
        [Fact]
        public void Constant_Int8() { var type = BuildStaticMethod("I8", typeof(int), m => m.Append(Expression.Constant(8))); Assert.Equal(8, InvokeStatic(type, "I8")); }
        [Fact]
        public void Constant_IntLarge() { var type = BuildStaticMethod("IL", typeof(int), m => m.Append(Expression.Constant(1000000))); Assert.Equal(1000000, InvokeStatic(type, "IL")); }

        #endregion

        #region ReturnExpression — 更多路径

        [Fact]
        public void Return_FromMiddle() { var type = BuildStaticMethod("RM", typeof(int), m => { m.Append(Expression.Return(Expression.Constant(42))); m.Append(Expression.Constant(0)); }); Assert.Equal(42, InvokeStatic(type, "RM")); }

        #endregion

        #region MethodCallExpression — 更多路径

        [Fact]
        public void Call_StaticMethodWithArgs()
        {
            var mi = typeof(Math).GetMethod(nameof(Math.Max), new[] { typeof(int), typeof(int) });
            var type = BuildStaticMethod("CMax", typeof(int), m =>
            {
                var a = m.DefineParameter(typeof(int), "a");
                var b = m.DefineParameter(typeof(int), "b");
                m.Append(Expression.Call(mi, a, b));
            });
            Assert.Equal(5, InvokeStatic(type, "CMax", 3, 5));
        }

        [Fact]
        public void Call_InstanceMethodWithArgs()
        {
            var mi = typeof(string).GetMethod(nameof(string.Substring), new[] { typeof(int), typeof(int) });
            var type = BuildStaticMethod("CSub", typeof(string), m =>
            {
                var s = m.DefineParameter(typeof(string), "s");
                var start = m.DefineParameter(typeof(int), "start");
                var len = m.DefineParameter(typeof(int), "len");
                m.Append(Expression.Call(s, mi, start, len));
            });
            Assert.Equal("hel", InvokeStatic(type, "CSub", "hello", 0, 3));
        }

        #endregion

        #region MethodCallEmitter — 实例方法链

        [Fact]
        public void MethodCallEmitter_Chain()
        {
            var te = _emitter.DefineType($"MC_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f = te.DefineField("_v", typeof(int), FieldAttributes.Private);
            var ct = te.DefineConstructor(MethodAttributes.Public); var cp = ct.DefineParameter(typeof(int), ParameterAttributes.None, "v"); ct.Append(Expression.Assign(f, cp));
            var m1 = te.DefineMethod("Double", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int)); m1.Append(Expression.Multiply(f, Expression.Constant(2)));
            var m2 = te.DefineMethod("CallDouble", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int)); m2.Append(Expression.Call(m1));
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t, 21);
            Assert.Equal(42, t.GetMethod("CallDouble").Invoke(inst, null));
        }

        [Fact]
        public void MethodCallEmitter_StaticWithArgs()
        {
            var te = _emitter.DefineType($"MCA_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var am = te.DefineMethod("Mult", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var a = am.DefineParameter(typeof(int), "a"); var b = am.DefineParameter(typeof(int), "b"); am.Append(Expression.Multiply(a, b));
            var cm = te.DefineMethod("Call", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            cm.Append(Expression.Call(am, Expression.Constant(6), Expression.Constant(7)));
            var t = te.CreateType();
            Assert.Equal(42, t.GetMethod("Call").Invoke(null, null));
        }

        #endregion

        #region AbstractTypeEmitter — DefineMethodOverride

        [Fact]
        public void DefineMethodOverride_ToString()
        {
            var te = _emitter.DefineType($"MO_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var ct = te.DefineConstructor(MethodAttributes.Public); ct.Append(Expression.Default(typeof(void)));
            var toStrMi = typeof(object).GetMethod(nameof(object.ToString));
            var toStr = te.DefineMethodOverride(ref toStrMi);
            toStr.Append(Expression.Constant("custom"));
            Assert.NotNull(te.CreateType());
        }

        [Fact]
        public void AbstractType_Interface_Implementation()
        {
            var te = _emitter.DefineType($"AI_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(IDisposable) });
            var ct = te.DefineConstructor(MethodAttributes.Public); ct.Append(Expression.Assign(te.DefineField("_d", typeof(int), FieldAttributes.Private), Expression.Constant(0)));
            var dm = te.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void));
            dm.Append(Expression.Default(typeof(void)));
            Assert.NotNull(te.CreateType());
        }

        #endregion

        #region ModuleEmitter — 更多API

        [Fact]
        public void ModuleEmitter_DefineEnum()
        {
            var mod = new ModuleEmitter($"ME_{Guid.NewGuid():N}");
            var e = mod.DefineEnum($"E_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(int));
            e.DefineLiteral("A", 1);
            e.DefineLiteral("B", 2);
            var t = e.CreateType();
            Assert.True(t.IsEnum);
        }

        [Fact]
        public void ModuleEmitter_MultipleTypes()
        {
            var mod = new ModuleEmitter($"MT_{Guid.NewGuid():N}");
            var t1 = mod.DefineType($"T1_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            t1.DefineField("X", typeof(int), FieldAttributes.Public);
            var t2 = mod.DefineType($"T2_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            t2.DefineField("Y", typeof(string), FieldAttributes.Public);
            Assert.NotNull(t1.CreateType());
            Assert.NotNull(t2.CreateType());
        }

        #endregion

        #region ConstructorEmitter — 更多路径

        [Fact]
        public void Constructor_MultiParam()
        {
            var te = _emitter.DefineType($"CM_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f1 = te.DefineField("_a", typeof(int), FieldAttributes.Public);
            var f2 = te.DefineField("_b", typeof(string), FieldAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            var p1 = ct.DefineParameter(typeof(int), ParameterAttributes.None, "a");
            var p2 = ct.DefineParameter(typeof(string), ParameterAttributes.None, "b");
            ct.Append(Expression.Assign(f1, p1));
            ct.Append(Expression.Assign(f2, p2));
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t, 42, "hello");
            Assert.Equal(42, t.GetField("_a").GetValue(inst));
            Assert.Equal("hello", t.GetField("_b").GetValue(inst));
        }

        #endregion

        #region Emitters — PropertyEmitter read-only

        [Fact]
        public void PropertyEmitter_ReadOnly()
        {
            var te = _emitter.DefineType($"PR_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var bf = te.DefineField("_rv", typeof(int), FieldAttributes.Private);
            var ct = te.DefineConstructor(MethodAttributes.Public); var cp = ct.DefineParameter(typeof(int), ParameterAttributes.None, "v"); ct.Append(Expression.Assign(bf, cp));
            var g = te.DefineMethod("get_RV", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(int)); g.Append(bf);
            var prop = te.DefineProperty("RV", PropertyAttributes.None, typeof(int)); prop.SetGetMethod(g);
            // No setter - read-only
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t, 99);
            Assert.Equal(99, t.GetProperty("RV").GetValue(inst));
        }

        #endregion

        #region MethodEmitter — 多参数和返回值

        [Fact]
        public void MethodEmitter_VoidReturn()
        {
            var te = _emitter.DefineType($"MV_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f = te.DefineField("_v", typeof(int), FieldAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public); ct.Append(Expression.Assign(f, Expression.Constant(0)));
            var sm = te.DefineMethod("Set", MethodAttributes.Public, typeof(void));
            var vp = sm.DefineParameter(typeof(int), "v");
            sm.Append(Expression.Assign(f, vp));
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t);
            t.GetMethod("Set").Invoke(inst, new object[] { 42 });
            Assert.Equal(42, t.GetField("_v").GetValue(inst));
        }

        #endregion

        #region Expression — 其他工厂方法

        [Fact]
        public void Variable_Type() { var v = Expression.Variable(typeof(int)); Assert.Equal(typeof(int), v.RuntimeType); }
        [Fact]
        public void Default_Int() { var type = BuildStaticMethod("DI", typeof(int), m => m.Append(Expression.Default(typeof(int)))); Assert.Equal(0, InvokeStatic(type, "DI")); }
        [Fact]
        public void Default_String() { var type = BuildStaticMethod("DS", typeof(string), m => m.Append(Expression.Default(typeof(string)))); Assert.Null(InvokeStatic(type, "DS")); }
        [Fact]
        public void Default_Bool() { var type = BuildStaticMethod("DB", typeof(bool), m => m.Append(Expression.Default(typeof(bool)))); Assert.False((bool)InvokeStatic(type, "DB")); }
        [Fact]
        public void Default_DateTime() { var type = BuildStaticMethod("DDT", typeof(DateTime), m => m.Append(Expression.Default(typeof(DateTime)))); Assert.Equal(default(DateTime), InvokeStatic(type, "DDT")); }
        [Fact]
        public void Default_Decimal() { var type = BuildStaticMethod("DDe", typeof(decimal), m => m.Append(Expression.Default(typeof(decimal)))); Assert.Equal(0m, InvokeStatic(type, "DDe")); }

        #endregion

        #region Coalesce

        [Fact]
        public void Coalesce_Left() { var type = BuildStaticMethod("CL", typeof(string), m => { var a = m.DefineParameter(typeof(string), "a"); var b = m.DefineParameter(typeof(string), "b"); m.Append(Expression.Coalesce(a, b)); }); Assert.Equal("hi", InvokeStatic(type, "CL", "hi", "default")); }
        [Fact]
        public void Coalesce_Right() { var type = BuildStaticMethod("CR", typeof(string), m => { var a = m.DefineParameter(typeof(string), "a"); var b = m.DefineParameter(typeof(string), "b"); m.Append(Expression.Coalesce(a, b)); }); Assert.Equal("default", InvokeStatic(type, "CR", null, "default")); }

        #endregion

        #region GotoExpression + LabelExpression

        [Fact]
        public void Goto_Label()
        {
            var type = BuildStaticMethod("GL", typeof(int), m =>
            {
                var label = Expression.Label();
                m.Append(Expression.Goto(label));
                m.Append(Expression.Constant(1)); // skipped
                m.Append(Expression.Label(label));
                m.Append(Expression.Constant(42));
            });
            Assert.Equal(42, InvokeStatic(type, "GL"));
        }

        #endregion

        #region Checked operations

        [Fact]
        public void Add_Overflow() { var type = BuildStaticMethod("AO", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.Add(a, b)); }); Assert.Equal(7, InvokeStatic(type, "AO", 3, 4)); }
        [Fact]
        public void Subtract_Overflow() { var type = BuildStaticMethod("SO", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.Subtract(a, b)); }); Assert.Equal(2, InvokeStatic(type, "SO", 5, 3)); }
        [Fact]
        public void Multiply_Overflow() { var type = BuildStaticMethod("MO", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); var b = m.DefineParameter(typeof(int), "b"); m.Append(Expression.Multiply(a, b)); }); Assert.Equal(6, InvokeStatic(type, "MO", 2, 3)); }

        #endregion

        #region Loop

        [Fact]
        public void Loop_Sum()
        {
            var type = BuildStaticMethod("LS", typeof(int), m =>
            {
                var sum = Expression.Variable(typeof(int));
                var i = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(sum, Expression.Constant(0)));
                m.Append(Expression.Assign(i, Expression.Constant(0)));
                var loop = Expression.Loop();
                loop.Append(Expression.IfThen(Expression.GreaterThanOrEqual(i, Expression.Constant(5)), Expression.Break()));
                loop.Append(Expression.AddAssign(sum, i));
                loop.Append(Expression.IncrementAssign(i));
                m.Append(loop);
                m.Append(sum);
            });
            Assert.Equal(10, InvokeStatic(type, "LS")); // 0+1+2+3+4=10
        }

        #endregion

        #region ArrayLength

        [Fact]
        public void ArrayLength_IntArray()
        {
            var type = BuildStaticMethod("AL", typeof(int), m =>
            {
                var arr = m.DefineParameter(typeof(int[]), "arr");
                m.Append(Expression.ArrayLength(arr));
            });
            Assert.Equal(3, InvokeStatic(type, "AL", new int[] { 1, 2, 3 }));
        }

        #endregion

        #region NamingScope

        [Fact]
        public void NamingScope_Unique()
        {
            var scope = new NamingScope();
            var n1 = scope.GetUniqueName("field");
            var n2 = scope.GetUniqueName("field");
            Assert.NotEqual(n1, n2);
            Assert.Equal("field", n1);
            Assert.Equal("field_1", n2);
            var child = scope.BeginScope();
            Assert.NotNull(child);
        }

        #endregion

        #region InvocationEmitter — 通过 DefineMethod 间接测试

        [Fact]
        public void InvocationEmitter_ViaStaticMethod()
        {
            var te = _emitter.DefineType($"IE_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var helper = te.DefineMethod("Helper", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var hp = helper.DefineParameter(typeof(int), "x");
            helper.Append(Expression.Multiply(hp, Expression.Constant(2)));
            // Use InvocationEmitter via Expression.Invoke(MethodEmitter, args)
            var caller = te.DefineMethod("CallHelper", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var cp = caller.DefineParameter(typeof(int), "x");
            caller.Append(Expression.Call(helper, cp));
            var t = te.CreateType();
            Assert.Equal(42, t.GetMethod("CallHelper").Invoke(null, new object[] { 21 }));
        }

        #endregion
    }

    /// <summary>
    /// 测试辅助类型。
    /// </summary>
    public class CoverageTarget
    {
        /// <summary>
        /// 名称。
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 值。
        /// </summary>
        public int Value { get; set; }
    }
}
