using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;
using System.Collections.Generic;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 覆盖率提升测part1 针对所有低于50%覆盖率的类
    /// </summary>
    public class CoverageBoostTests
    {
        private readonly ModuleEmitter _emitter = new ModuleEmitter($"Inkslab.Expressions.Tests.CoverageBoost.{Guid.NewGuid():N}");

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

        #region AstException

        [Fact]
        public void AstException_DefaultConstructor()
        {
            var ex = new AstException();
            Assert.NotNull(ex);
        }

        [Fact]
        public void AstException_MessageConstructor()
        {
            var ex = new AstException("test error");
            Assert.Equal("test error", ex.Message);
        }

        #endregion

        #region TypeComparer

        [Fact]
        public void TypeComparer_ComparesTypes()
        {
            var comparer = new TypeComparer();
            Assert.NotEqual(0, comparer.Compare(typeof(string), typeof(int)));
            Assert.Equal(0, comparer.Compare(typeof(string), typeof(string)));
        }

        [Fact]
        public void TypeComparer_Instance()
        {
            Assert.NotNull(TypeComparer.Instance);
            Assert.Equal(0, TypeComparer.Instance.Compare(typeof(int), typeof(int)));
        }

        [Fact]
        public void TypeComparer_DifferentTypes()
        {
            var comparer = new TypeComparer();
            var result = comparer.Compare(typeof(int), typeof(string));
            Assert.NotEqual(0, result);
            Assert.Equal(-result, comparer.Compare(typeof(string), typeof(int)));
        }

        #endregion

        #region ConstantExpression �?各类型覆�?

        [Fact]
        public void Constant_CharValue()
        {
            var type = BuildStaticMethod("ConstChar", typeof(char), m => m.Append(Expression.Constant('A')));
            Assert.Equal('A', InvokeStatic(type, "ConstChar"));
        }

        [Fact]
        public void Constant_ByteValue()
        {
            var type = BuildStaticMethod("ConstByte", typeof(byte), m => m.Append(Expression.Constant((byte)255)));
            Assert.Equal((byte)255, InvokeStatic(type, "ConstByte"));
        }

        [Fact]
        public void Constant_SByteValue()
        {
            var type = BuildStaticMethod("ConstSByte", typeof(sbyte), m => m.Append(Expression.Constant((sbyte)-10)));
            Assert.Equal((sbyte)-10, InvokeStatic(type, "ConstSByte"));
        }

        [Fact]
        public void Constant_ShortValue()
        {
            var type = BuildStaticMethod("ConstShort", typeof(short), m => m.Append(Expression.Constant((short)1234)));
            Assert.Equal((short)1234, InvokeStatic(type, "ConstShort"));
        }

        [Fact]
        public void Constant_UShortValue()
        {
            var type = BuildStaticMethod("ConstUShort", typeof(ushort), m => m.Append(Expression.Constant((ushort)60000)));
            Assert.Equal((ushort)60000, InvokeStatic(type, "ConstUShort"));
        }

        [Fact]
        public void Constant_UIntValue()
        {
            var type = BuildStaticMethod("ConstUInt", typeof(uint), m => m.Append(Expression.Constant(42u)));
            Assert.Equal(42u, InvokeStatic(type, "ConstUInt"));
        }

        [Fact]
        public void Constant_ULongValue()
        {
            var type = BuildStaticMethod("ConstULong", typeof(ulong), m => m.Append(Expression.Constant(999UL)));
            Assert.Equal(999UL, InvokeStatic(type, "ConstULong"));
        }

        [Fact]
        public void Constant_FloatValue()
        {
            var type = BuildStaticMethod("ConstFloat", typeof(float), m => m.Append(Expression.Constant(1.5f)));
            Assert.Equal(1.5f, InvokeStatic(type, "ConstFloat"));
        }

        [Fact]
        public void Constant_IntPtrValue()
        {
            var type = BuildStaticMethod("ConstIntPtr", typeof(IntPtr), m => m.Append(Expression.Constant(new IntPtr(42))));
            Assert.Equal(new IntPtr(42), InvokeStatic(type, "ConstIntPtr"));
        }

        [Fact]
        public void Constant_TypeValue()
        {
            var type = BuildStaticMethod("ConstType", typeof(Type), m => m.Append(Expression.Constant(typeof(string))));
            Assert.Equal(typeof(string), InvokeStatic(type, "ConstType"));
        }

        [Fact]
        public void Constant_NullValue_NullableType()
        {
            var type = BuildStaticMethod("ConstNullInt", typeof(int?), m => m.Append(Expression.Constant(null, typeof(int?))));
            Assert.Null(InvokeStatic(type, "ConstNullInt"));
        }

        [Fact]
        public void Constant_NullValue_ValueType_Throws()
        {
            Assert.ThrowsAny<Exception>(() => Expression.Constant(null, typeof(int)));
        }

        [Fact]
        public void Constant_Uri()
        {
            var type = BuildStaticMethod("ConstUri", typeof(Uri), m => m.Append(Expression.Constant(new Uri("https://example.com"))));
            Assert.Equal(new Uri("https://example.com"), InvokeStatic(type, "ConstUri"));
        }

        [Fact]
        public void Constant_Version()
        {
            var type = BuildStaticMethod("ConstVer", typeof(Version), m => m.Append(Expression.Constant(new Version(1, 2, 3, 4))));
            Assert.Equal(new Version(1, 2, 3, 4), InvokeStatic(type, "ConstVer"));
        }

        [Fact]
        public void Constant_MethodInfo()
        {
            var mi = typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) });
            var type = BuildStaticMethod("ConstMI", typeof(MethodInfo), m => m.Append(Expression.Constant(mi)));
            Assert.Equal(mi, InvokeStatic(type, "ConstMI"));
        }

        [Fact]
        public void Constant_DecimalIntPath()
        {
            var type = BuildStaticMethod("DecInt", typeof(decimal), m => m.Append(Expression.Constant(42m)));
            Assert.Equal(42m, InvokeStatic(type, "DecInt"));
        }

        [Fact]
        public void Constant_DecimalLongPath()
        {
            var type = BuildStaticMethod("DecLong", typeof(decimal), m => m.Append(Expression.Constant(5000000000m)));
            Assert.Equal(5000000000m, InvokeStatic(type, "DecLong"));
        }

        [Fact]
        public void Constant_DecimalBitsPath()
        {
            var type = BuildStaticMethod("DecBits", typeof(decimal), m => m.Append(Expression.Constant(123.456m)));
            Assert.Equal(123.456m, InvokeStatic(type, "DecBits"));
        }

        [Fact]
        public void Constant_ArrayValue()
        {
            var type = BuildStaticMethod("ConstArr", typeof(int[]), m => m.Append(Expression.Constant(new int[] { 1, 2, 3 })));
            Assert.Equal(new[] { 1, 2, 3 }, (int[])InvokeStatic(type, "ConstArr"));
        }

        #endregion

        #region ConvertExpression

        [Fact]
        public void Convert_DoubleToInt() { var type = BuildStaticMethod("D2I", typeof(int), m => { var d = m.DefineParameter(typeof(double), "d"); m.Append(Expression.Convert(d, typeof(int))); }); Assert.Equal(42, InvokeStatic(type, "D2I", 42.9)); }
        [Fact]
        public void Convert_IntToLong() { var type = BuildStaticMethod("I2L", typeof(long), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(long))); }); Assert.Equal(123L, InvokeStatic(type, "I2L", 123)); }
        [Fact]
        public void Convert_ObjectToString() { var type = BuildStaticMethod("O2S", typeof(string), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.Convert(o, typeof(string))); }); Assert.Equal("hello", InvokeStatic(type, "O2S", (object)"hello")); }
        [Fact]
        public void Convert_IntToObject() { var type = BuildStaticMethod("I2O", typeof(object), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(object))); }); Assert.Equal(42, InvokeStatic(type, "I2O", 42)); }
        [Fact]
        public void Convert_ObjectToInt() { var type = BuildStaticMethod("O2I", typeof(int), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.Convert(o, typeof(int))); }); Assert.Equal(42, InvokeStatic(type, "O2I", (object)42)); }
        [Fact]
        public void Convert_SameType() { var type = BuildStaticMethod("Same", typeof(int), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(int))); }); Assert.Equal(7, InvokeStatic(type, "Same", 7)); }
        [Fact]
        public void Convert_NullableToValue() { var type = BuildStaticMethod("NI2I", typeof(int), m => { var ni = m.DefineParameter(typeof(int?), "ni"); m.Append(Expression.Convert(ni, typeof(int))); }); Assert.Equal(42, InvokeStatic(type, "NI2I", (int?)42)); }
        [Fact]
        public void Convert_IntToFloat() { var type = BuildStaticMethod("I2F", typeof(float), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(float))); }); Assert.Equal(3.0f, InvokeStatic(type, "I2F", 3)); }
        [Fact]
        public void Convert_IntToNullable() { var type = BuildStaticMethod("I2NI", typeof(int?), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(int?))); }); Assert.Equal((int?)5, InvokeStatic(type, "I2NI", 5)); }
        [Fact]
        public void Convert_ByteToInt() { var type = BuildStaticMethod("B2I", typeof(int), m => { var b = m.DefineParameter(typeof(byte), "b"); m.Append(Expression.Convert(b, typeof(int))); }); Assert.Equal(255, InvokeStatic(type, "B2I", (byte)255)); }
        [Fact]
        public void Convert_IntToShort() { var type = BuildStaticMethod("I2S", typeof(short), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(short))); }); Assert.Equal((short)100, InvokeStatic(type, "I2S", 100)); }
        [Fact]
        public void Convert_IntToByte() { var type = BuildStaticMethod("I2B", typeof(byte), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(byte))); }); Assert.Equal((byte)42, InvokeStatic(type, "I2B", 42)); }
        [Fact]
        public void Convert_LongToDouble() { var type = BuildStaticMethod("L2D", typeof(double), m => { var l = m.DefineParameter(typeof(long), "l"); m.Append(Expression.Convert(l, typeof(double))); }); Assert.Equal(100.0, InvokeStatic(type, "L2D", 100L)); }
        [Fact]
        public void Convert_FloatToDouble() { var type = BuildStaticMethod("F2D", typeof(double), m => { var f = m.DefineParameter(typeof(float), "f"); m.Append(Expression.Convert(f, typeof(double))); }); Assert.Equal(1.5, (double)InvokeStatic(type, "F2D", 1.5f), 5); }
        [Fact]
        public void Convert_IntToUInt() { var type = BuildStaticMethod("I2UI", typeof(uint), m => { var i = m.DefineParameter(typeof(int), "i"); m.Append(Expression.Convert(i, typeof(uint))); }); Assert.Equal(42u, InvokeStatic(type, "I2UI", 42)); }

        #endregion

        #region TypeAsExpression

        [Fact]
        public void TypeAs_ReferenceType_Match() { var type = BuildStaticMethod("AsStr", typeof(string), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeAs(o, typeof(string))); }); Assert.Equal("test", InvokeStatic(type, "AsStr", (object)"test")); }
        [Fact]
        public void TypeAs_ReferenceType_NoMatch() { var type = BuildStaticMethod("AsStr2", typeof(string), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeAs(o, typeof(string))); }); Assert.Null(InvokeStatic(type, "AsStr2", (object)42)); }
        [Fact]
        public void TypeAs_NullableValueType() { var type = BuildStaticMethod("AsNI", typeof(int?), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeAs(o, typeof(int?))); }); Assert.Equal((int?)42, InvokeStatic(type, "AsNI", (object)42)); Assert.Null(InvokeStatic(type, "AsNI", (object)"test")); }
        [Fact]
        public void TypeAs_SameType() { var type = BuildStaticMethod("AsSame", typeof(string), m => { var s = m.DefineParameter(typeof(string), "s"); m.Append(Expression.TypeAs(s, typeof(string))); }); Assert.Equal("hello", InvokeStatic(type, "AsSame", "hello")); }
        [Fact]
        public void TypeAs_Interface() { var type = BuildStaticMethod("AsIComp", typeof(IComparable), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeAs(o, typeof(IComparable))); }); Assert.NotNull(InvokeStatic(type, "AsIComp", (object)42)); }

        #endregion

        #region TypeIsExpression

        [Fact]
        public void TypeIs_ValueType() { var type = BuildStaticMethod("IsInt", typeof(bool), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeIs(o, typeof(int))); }); Assert.True((bool)InvokeStatic(type, "IsInt", (object)42)); Assert.False((bool)InvokeStatic(type, "IsInt", (object)"hello")); }
        [Fact]
        public void TypeIs_Interface() { var type = BuildStaticMethod("IsDisp", typeof(bool), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeIs(o, typeof(IDisposable))); }); Assert.False((bool)InvokeStatic(type, "IsDisp", (object)"hello")); }
        [Fact]
        public void TypeIs_NullableType() { var type = BuildStaticMethod("IsNI", typeof(bool), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeIs(o, typeof(int?))); }); Assert.True((bool)InvokeStatic(type, "IsNI", (object)42)); }
        [Fact]
        public void TypeIs_BaseClass() { var type = BuildStaticMethod("IsExc", typeof(bool), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.TypeIs(o, typeof(Exception))); }); Assert.True((bool)InvokeStatic(type, "IsExc", (object)new ArgumentException())); Assert.False((bool)InvokeStatic(type, "IsExc", (object)"hello")); }

        #endregion

        #region ArrayIndexExpression

        [Fact]
        public void ArrayIndex_ByExpression() { var type = BuildStaticMethod("GetByExpr", typeof(string), m => { var arr = m.DefineParameter(typeof(string[]), "arr"); var idx = m.DefineParameter(typeof(int), "idx"); m.Append(Expression.ArrayIndex(arr, idx)); }); Assert.Equal("b", InvokeStatic(type, "GetByExpr", new[] { "a", "b", "c" }, 1)); }
        [Fact]
        public void ArrayIndex_ByConstantIndex() { var type = BuildStaticMethod("GetByConst", typeof(int), m => { var arr = m.DefineParameter(typeof(int[]), "arr"); m.Append(Expression.ArrayIndex(arr, 0)); }); Assert.Equal(10, InvokeStatic(type, "GetByConst", new[] { 10, 20 })); }
        [Fact]
        public void ArrayIndex_Assign_ByExpression() { var type = BuildStaticMethod("SetByExpr", typeof(void), m => { var arr = m.DefineParameter(typeof(string[]), "arr"); var val = m.DefineParameter(typeof(string), "val"); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), val)); }); var data = new[] { "a", "b", "c" }; InvokeStatic(type, "SetByExpr", data, "x"); Assert.Equal("x", data[0]); }
        [Fact]
        public void ArrayIndex_Assign_ByConstant() { var type = BuildStaticMethod("SetByConst", typeof(void), m => { var arr = m.DefineParameter(typeof(string[]), "arr"); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant("replaced"))); }); var data = new[] { "a", "b" }; InvokeStatic(type, "SetByConst", (object)data); Assert.Equal("replaced", data[0]); }
        [Fact]
        public void ArrayIndex_ByteArray() { var type = BuildStaticMethod("GetByte", typeof(byte), m => { var arr = m.DefineParameter(typeof(byte[]), "arr"); var idx = m.DefineParameter(typeof(int), "idx"); m.Append(Expression.ArrayIndex(arr, idx)); }); Assert.Equal((byte)42, InvokeStatic(type, "GetByte", new byte[] { 42 }, 0)); }
        [Fact]
        public void ArrayIndex_LongArray() { var type = BuildStaticMethod("GetLong", typeof(long), m => { var arr = m.DefineParameter(typeof(long[]), "arr"); var idx = m.DefineParameter(typeof(int), "idx"); m.Append(Expression.ArrayIndex(arr, idx)); }); Assert.Equal(99L, InvokeStatic(type, "GetLong", new long[] { 99L }, 0)); }
        [Fact]
        public void ArrayIndex_DoubleArray() { var type = BuildStaticMethod("GetDbl", typeof(double), m => { var arr = m.DefineParameter(typeof(double[]), "arr"); var idx = m.DefineParameter(typeof(int), "idx"); m.Append(Expression.ArrayIndex(arr, idx)); }); Assert.Equal(3.14, InvokeStatic(type, "GetDbl", new[] { 3.14 }, 0)); }
        [Fact]
        public void ArrayIndex_FloatArray() { var type = BuildStaticMethod("GetFlt", typeof(float), m => { var arr = m.DefineParameter(typeof(float[]), "arr"); var idx = m.DefineParameter(typeof(int), "idx"); m.Append(Expression.ArrayIndex(arr, idx)); }); Assert.Equal(1.5f, InvokeStatic(type, "GetFlt", new[] { 1.5f }, 0)); }
        [Fact]
        public void ArrayIndex_ShortArray() { var type = BuildStaticMethod("GetShort", typeof(short), m => { var arr = m.DefineParameter(typeof(short[]), "arr"); var idx = m.DefineParameter(typeof(int), "idx"); m.Append(Expression.ArrayIndex(arr, idx)); }); Assert.Equal((short)10, InvokeStatic(type, "GetShort", new short[] { 10 }, 0)); }
        [Fact]
        public void ArrayIndex_BoolArray() { var type = BuildStaticMethod("GetBool", typeof(bool), m => { var arr = m.DefineParameter(typeof(bool[]), "arr"); var idx = m.DefineParameter(typeof(int), "idx"); m.Append(Expression.ArrayIndex(arr, idx)); }); Assert.True((bool)InvokeStatic(type, "GetBool", new[] { true }, 0)); }
        [Fact]
        public void ArrayIndex_CharArray() { var type = BuildStaticMethod("GetChar", typeof(char), m => { var arr = m.DefineParameter(typeof(char[]), "arr"); var idx = m.DefineParameter(typeof(int), "idx"); m.Append(Expression.ArrayIndex(arr, idx)); }); Assert.Equal('A', InvokeStatic(type, "GetChar", new[] { 'A' }, 0)); }

        #endregion

        #region BinaryExpression �?更多类型

        [Fact]
        public void Add_Doubles() { var type = BuildStaticMethod("AddDbl", typeof(double), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.Add(a, b)); }); Assert.Equal(5.5, InvokeStatic(type, "AddDbl", 2.5, 3.0)); }
        [Fact]
        public void Add_Longs() { var type = BuildStaticMethod("AddLng", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.Add(a, b)); }); Assert.Equal(300L, InvokeStatic(type, "AddLng", 100L, 200L)); }
        [Fact]
        public void Subtract_Doubles() { var type = BuildStaticMethod("SubDbl", typeof(double), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.Subtract(a, b)); }); Assert.Equal(1.5, InvokeStatic(type, "SubDbl", 3.5, 2.0)); }
        [Fact]
        public void Multiply_Floats() { var type = BuildStaticMethod("MulFlt", typeof(float), m => { var a = m.DefineParameter(typeof(float), "a"); var b = m.DefineParameter(typeof(float), "b"); m.Append(Expression.Multiply(a, b)); }); Assert.Equal(6.0f, InvokeStatic(type, "MulFlt", 2.0f, 3.0f)); }
        [Fact]
        public void LessThan_Doubles() { var type = BuildStaticMethod("LtDbl", typeof(bool), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.LessThan(a, b)); }); Assert.True((bool)InvokeStatic(type, "LtDbl", 1.0, 2.0)); }
        [Fact]
        public void GreaterThan_Longs() { var type = BuildStaticMethod("GtLng", typeof(bool), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.GreaterThan(a, b)); }); Assert.True((bool)InvokeStatic(type, "GtLng", 100L, 50L)); }
        [Fact]
        public void Equal_Strings() { var type = BuildStaticMethod("EqStr", typeof(bool), m => { var a = m.DefineParameter(typeof(string), "a"); var b = m.DefineParameter(typeof(string), "b"); m.Append(Expression.Equal(a, b)); }); Assert.True((bool)InvokeStatic(type, "EqStr", "abc", "abc")); Assert.False((bool)InvokeStatic(type, "EqStr", "abc", "def")); }
        [Fact]
        public void NotEqual_Strings() { var type = BuildStaticMethod("NeStr", typeof(bool), m => { var a = m.DefineParameter(typeof(string), "a"); var b = m.DefineParameter(typeof(string), "b"); m.Append(Expression.NotEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "NeStr", "abc", "def")); }
        [Fact]
        public void Not_IntBitwise() { var type = BuildStaticMethod("NotInt", typeof(int), m => { var a = m.DefineParameter(typeof(int), "a"); m.Append(Expression.Not(a)); }); Assert.Equal(~5, InvokeStatic(type, "NotInt", 5)); }
        [Fact]
        public void Divide_Doubles() { var type = BuildStaticMethod("DivDbl", typeof(double), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.Divide(a, b)); }); Assert.Equal(2.5, InvokeStatic(type, "DivDbl", 5.0, 2.0)); }
        [Fact]
        public void Modulo_Doubles() { var type = BuildStaticMethod("ModDbl", typeof(double), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.Modulo(a, b)); }); Assert.Equal(1.0, InvokeStatic(type, "ModDbl", 5.0, 2.0)); }
        [Fact]
        public void LessThanOrEqual_Longs() { var type = BuildStaticMethod("LteLng", typeof(bool), m => { var a = m.DefineParameter(typeof(long), "a"); var b = m.DefineParameter(typeof(long), "b"); m.Append(Expression.LessThanOrEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "LteLng", 5L, 5L)); Assert.False((bool)InvokeStatic(type, "LteLng", 6L, 5L)); }
        [Fact]
        public void GreaterThanOrEqual_Doubles() { var type = BuildStaticMethod("GteDbl", typeof(bool), m => { var a = m.DefineParameter(typeof(double), "a"); var b = m.DefineParameter(typeof(double), "b"); m.Append(Expression.GreaterThanOrEqual(a, b)); }); Assert.True((bool)InvokeStatic(type, "GteDbl", 5.0, 5.0)); }
        [Fact]
        public void EqualNull() { var type = BuildStaticMethod("EqNull", typeof(bool), m => { var s = m.DefineParameter(typeof(string), "s"); m.Append(Expression.Equal(s, Expression.Constant(null, typeof(string)))); }); Assert.True((bool)InvokeStatic(type, "EqNull", new object[] { null })); Assert.False((bool)InvokeStatic(type, "EqNull", "hello")); }

        #endregion

        #region SwitchExpression

        [Fact]
        public void Switch_StringEquality()
        {
            var type = BuildStaticMethod("SwStr", typeof(void), m =>
            {
                var val = m.DefineParameter(typeof(string), "val");
                var result = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(result, Expression.Constant(0)));
                var sw = Expression.Switch(val, Expression.Assign(result, Expression.Constant(-1)));
                sw.Case(Expression.Constant("hello")).Append(Expression.Assign(result, Expression.Constant(1)));
                sw.Case(Expression.Constant("world")).Append(Expression.Assign(result, Expression.Constant(2)));
                m.Append(sw);
            });
            InvokeStatic(type, "SwStr", "hello");
            InvokeStatic(type, "SwStr", "world");
            InvokeStatic(type, "SwStr", "other");
        }

        [Fact]
        public void Switch_WithDefault_NoCase()
        {
            var type = BuildStaticMethod("SwDef", typeof(int), m =>
            {
                var val = m.DefineParameter(typeof(int), "val");
                var result = Expression.Variable(typeof(int));
                m.Append(Expression.Assign(result, Expression.Constant(0)));
                m.Append(Expression.Switch(val, Expression.Assign(result, Expression.Constant(42))));
                m.Append(result);
            });
            Assert.Equal(42, InvokeStatic(type, "SwDef", 1));
        }

        [Fact]
        public void Switch_RuntimeType()
        {
            var type = BuildStaticMethod("SwType", typeof(void), m =>
            {
                var val = m.DefineParameter(typeof(object), "val");
                var result = Expression.Variable(typeof(int));
                var sw = Expression.Switch(val, Expression.Assign(result, Expression.Constant(-1)));
                var strVar = Expression.Variable(typeof(string));
                sw.Case(strVar).Append(Expression.Assign(result, Expression.Constant(1)));
                m.Append(sw);
            });
            InvokeStatic(type, "SwType", (object)"hello");
            InvokeStatic(type, "SwType", (object)42);
        }

        [Fact]
        public void Switch_NoCasesNoDefault_Throws()
        {
            var typeEmitter = _emitter.DefineType($"SwErr_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var method = typeEmitter.DefineMethod("SwErr", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            method.DefineParameter(typeof(int), "val");
            method.Append(Expression.Switch(method.GetParameters()[0]));
            Assert.Throws<AstException>(() => typeEmitter.CreateType());
        }

        #endregion

        #region UnaryExpression

        [Fact]
        public void IncrementAssign_Variable() { var type = BuildStaticMethod("IncrA", typeof(int), m => { var v = Expression.Variable(typeof(int)); m.Append(Expression.Assign(v, Expression.Constant(5))); m.Append(Expression.IncrementAssign(v)); m.Append(v); }); Assert.Equal(6, InvokeStatic(type, "IncrA")); }
        [Fact]
        public void DecrementAssign_Variable() { var type = BuildStaticMethod("DecrA", typeof(int), m => { var v = Expression.Variable(typeof(int)); m.Append(Expression.Assign(v, Expression.Constant(5))); m.Append(Expression.DecrementAssign(v)); m.Append(v); }); Assert.Equal(4, InvokeStatic(type, "DecrA")); }
        [Fact]
        public void Increment_Long() { var type = BuildStaticMethod("IncrL", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); m.Append(Expression.Increment(a)); }); Assert.Equal(6L, InvokeStatic(type, "IncrL", 5L)); }
        [Fact]
        public void Decrement_Long() { var type = BuildStaticMethod("DecrL", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); m.Append(Expression.Decrement(a)); }); Assert.Equal(4L, InvokeStatic(type, "DecrL", 5L)); }
        [Fact]
        public void Increment_Double() { var type = BuildStaticMethod("IncrD", typeof(double), m => { var a = m.DefineParameter(typeof(double), "a"); m.Append(Expression.Increment(a)); }); Assert.Equal(6.5, InvokeStatic(type, "IncrD", 5.5)); }
        [Fact]
        public void Negate_Double() { var type = BuildStaticMethod("NegD", typeof(double), m => { var a = m.DefineParameter(typeof(double), "a"); m.Append(Expression.Negate(a)); }); Assert.Equal(-3.5, InvokeStatic(type, "NegD", 3.5)); }
        [Fact]
        public void Negate_Long() { var type = BuildStaticMethod("NegL", typeof(long), m => { var a = m.DefineParameter(typeof(long), "a"); m.Append(Expression.Negate(a)); }); Assert.Equal(-100L, InvokeStatic(type, "NegL", 100L)); }
        [Fact]
        public void Increment_Float() { var type = BuildStaticMethod("IncrF", typeof(float), m => { var a = m.DefineParameter(typeof(float), "a"); m.Append(Expression.Increment(a)); }); Assert.Equal(2.5f, InvokeStatic(type, "IncrF", 1.5f)); }
        [Fact]
        public void Increment_Short() { var type = BuildStaticMethod("IncrSh", typeof(short), m => { var a = m.DefineParameter(typeof(short), "a"); m.Append(Expression.Increment(a)); }); Assert.Equal((short)6, InvokeStatic(type, "IncrSh", (short)5)); }

        #endregion

        #region FieldExpression

        [Fact]
        public void Field_StaticWrite() { var type = BuildStaticMethod("WriteSF", typeof(void), m => { m.Append(Expression.Assign(Expression.Field(typeof(CoverageHelper).GetField(nameof(CoverageHelper.StaticValue))), Expression.Constant(42))); }); InvokeStatic(type, "WriteSF"); Assert.Equal(42, CoverageHelper.StaticValue); }
        [Fact]
        public void Field_InstanceWrite() { var f = typeof(CoverageHelper).GetField(nameof(CoverageHelper.InstanceValue)); var type = BuildStaticMethod("WriteIF", typeof(CoverageHelper), m => { var obj = m.DefineParameter(typeof(CoverageHelper), "obj"); m.Append(Expression.Assign(Expression.Field(obj, f), Expression.Constant(99))); m.Append(obj); }); var h = new CoverageHelper(); Assert.Equal(99, ((CoverageHelper)InvokeStatic(type, "WriteIF", h)).InstanceValue); }
        [Fact]
        public void Field_ReadonlyCannotWrite() { Assert.False(Expression.Field(typeof(CoverageHelper).GetField(nameof(CoverageHelper.ReadOnlyValue))).CanWrite); }

        #endregion

        #region PropertyExpression

        [Fact]
        public void Property_InstanceReadWrite() { var p = typeof(Exception).GetProperty(nameof(Exception.Source)); var type = BuildStaticMethod("PropRW", typeof(string), m => { var ex = m.DefineParameter(typeof(Exception), "ex"); var pe = Expression.Property(ex, p); m.Append(Expression.Assign(pe, Expression.Constant("src"))); m.Append(pe); }); Assert.Equal("src", InvokeStatic(type, "PropRW", new Exception())); }

        #endregion

        #region MemberInitExpression

        [Fact]
        public void MemberInit_WithField() { var f = typeof(CoverageFieldTarget).GetField(nameof(CoverageFieldTarget.X)); var type = BuildStaticMethod("MIF", typeof(CoverageFieldTarget), m => m.Append(Expression.MemberInit(Expression.New(typeof(CoverageFieldTarget)), Expression.Bind(f, Expression.Constant(42))))); Assert.Equal(42, ((CoverageFieldTarget)InvokeStatic(type, "MIF")).X); }
        [Fact]
        public void MemberInit_NoBindings() { var type = BuildStaticMethod("MIN", typeof(object), m => m.Append(Expression.MemberInit(Expression.New(typeof(object))))); Assert.NotNull(InvokeStatic(type, "MIN")); }

        #endregion

        #region TryExpression

        [Fact]
        public void Try_CatchSpecific() { var type = BuildStaticMethod("TCS", typeof(int), m => { var r = Expression.Variable(typeof(int)); m.Append(Expression.Assign(r, Expression.Constant(0))); var t = Expression.Try(); t.Append(Expression.Throw(typeof(InvalidOperationException))); t.Catch(typeof(Exception)).Append(Expression.Assign(r, Expression.Constant(1))); m.Append(t); m.Append(r); }); Assert.Equal(1, InvokeStatic(type, "TCS")); }
        [Fact]
        public void Try_FinallyOnly() { var type = BuildStaticMethod("TFO", typeof(int), m => { var r = Expression.Variable(typeof(int)); m.Append(Expression.Assign(r, Expression.Constant(0))); var fb = Expression.Block(); fb.Append(Expression.Assign(r, Expression.Constant(42))); var t = Expression.Try(fb); t.Append(Expression.Assign(r, Expression.Constant(1))); m.Append(t); m.Append(r); }); Assert.Equal(42, InvokeStatic(type, "TFO")); }
        [Fact]
        public void Try_MultipleCatch() { var type = BuildStaticMethod("TMC", typeof(int), m => { var r = Expression.Variable(typeof(int)); m.Append(Expression.Assign(r, Expression.Constant(0))); var t = Expression.Try(); t.Append(Expression.Throw(typeof(ArgumentException), "e")); t.Catch(typeof(Exception)).Append(Expression.Assign(r, Expression.Constant(2))); m.Append(t); m.Append(r); }); Assert.Equal(2, InvokeStatic(type, "TMC")); }
        [Fact]
        public void Try_CatchVariable() { var mp = typeof(Exception).GetProperty(nameof(Exception.Message)); var type = BuildStaticMethod("TCV", typeof(string), m => { var r = Expression.Variable(typeof(string)); m.Append(Expression.Assign(r, Expression.Constant(""))); var ev = Expression.Variable(typeof(Exception)); var t = Expression.Try(); t.Append(Expression.Throw(typeof(InvalidOperationException), "msg")); t.Catch(ev).Append(Expression.Assign(r, Expression.Property(ev, mp))); m.Append(t); m.Append(r); }); Assert.Equal("msg", InvokeStatic(type, "TCV")); }

        #endregion

        #region ConditionExpression

        [Fact]
        public void Condition_ExplicitReturnType() { var type = BuildStaticMethod("CRT", typeof(object), m => { var f = m.DefineParameter(typeof(bool), "f"); m.Append(Expression.Condition(f, Expression.Constant("s"), Expression.Convert(Expression.Constant(42), typeof(object)), typeof(object))); }); Assert.Equal("s", InvokeStatic(type, "CRT", true)); Assert.Equal(42, InvokeStatic(type, "CRT", false)); }
        [Fact]
        public void Condition_BothVoid() { var expr = Expression.Condition(Expression.Constant(true), Expression.Constant(1), Expression.Constant(2), typeof(object)); Assert.NotNull(expr); }

        #endregion

        #region IfThenElseExpression / IfThenExpression

        [Fact]
        public void IfThen_ExpressionCreation() { var expr = Expression.IfThen(Expression.Constant(true), Expression.Constant(1)); Assert.NotNull(expr); }
        [Fact]
        public void IfThenElse_WithReturn() { var type = BuildStaticMethod("IER", typeof(int), m => { var f = m.DefineParameter(typeof(bool), "f"); m.Append(Expression.IfThenElse(f, Expression.Return(Expression.Constant(1)), Expression.Return(Expression.Constant(2)))); m.Append(Expression.Constant(0)); }); Assert.Equal(1, InvokeStatic(type, "IER", true)); Assert.Equal(2, InvokeStatic(type, "IER", false)); }
        [Fact]
        public void IfThenElse_NonVoidBranches() { var type = BuildStaticMethod("IENV", typeof(void), m => { var f = m.DefineParameter(typeof(bool), "f"); m.Append(Expression.IfThenElse(f, Expression.Constant(1), Expression.Constant(2))); }); InvokeStatic(type, "IENV", true); InvokeStatic(type, "IENV", false); }

        #endregion

        #region ReturnExpression

        [Fact]
        public void Return_WithValue() { var type = BuildStaticMethod("RV", typeof(int), m => { m.Append(Expression.Return(Expression.Constant(99))); m.Append(Expression.Constant(0)); }); Assert.Equal(99, InvokeStatic(type, "RV")); }
        [Fact]
        public void Return_Void() { var type = BuildStaticMethod("RVd", typeof(void), m => m.Append(Expression.Return())); InvokeStatic(type, "RVd"); }
        [Fact]
        public void Return_InIfThenElse() { var type = BuildStaticMethod("RIE", typeof(int), m => { var f = m.DefineParameter(typeof(bool), "f"); m.Append(Expression.IfThenElse(f, Expression.Return(Expression.Constant(42)), Expression.Return(Expression.Constant(99)))); m.Append(Expression.Constant(0)); }); Assert.Equal(42, InvokeStatic(type, "RIE", true)); Assert.Equal(99, InvokeStatic(type, "RIE", false)); }

        #endregion

        #region NewArrayExpression

        [Fact]
        public void NewArray_ZeroSize() { var type = BuildStaticMethod("EA", typeof(int[]), m => m.Append(Expression.NewArray(0, typeof(int)))); Assert.Empty((int[])InvokeStatic(type, "EA")); }
        [Fact]
        public void NewArray_SpecificType() { var type = BuildStaticMethod("SA", typeof(string[]), m => m.Append(Expression.NewArray(3, typeof(string)))); Assert.Equal(3, ((string[])InvokeStatic(type, "SA")).Length); }
        [Fact]
        public void Array_TypedInts() { var type = BuildStaticMethod("IA", typeof(int[]), m => { var arr = Expression.Variable(typeof(int[])); m.Append(Expression.Assign(arr, Expression.NewArray(3, typeof(int)))); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant(1))); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 1), Expression.Constant(2))); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 2), Expression.Constant(3))); m.Append(arr); }); Assert.Equal(new[] { 1, 2, 3 }, (int[])InvokeStatic(type, "IA")); }
        [Fact]
        public void Array_Strings() { var type = BuildStaticMethod("SArr", typeof(string[]), m => { var arr = Expression.Variable(typeof(string[])); m.Append(Expression.Assign(arr, Expression.NewArray(2, typeof(string)))); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant("a"))); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 1), Expression.Constant("b"))); m.Append(arr); }); Assert.Equal(new[] { "a", "b" }, (string[])InvokeStatic(type, "SArr")); }
        [Fact]
        public void Array_Longs() { var type = BuildStaticMethod("LA", typeof(long[]), m => { var arr = Expression.Variable(typeof(long[])); m.Append(Expression.Assign(arr, Expression.NewArray(2, typeof(long)))); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant(100L))); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 1), Expression.Constant(200L))); m.Append(arr); }); Assert.Equal(new[] { 100L, 200L }, (long[])InvokeStatic(type, "LA")); }
        [Fact]
        public void Array_Doubles() { var type = BuildStaticMethod("DA", typeof(double[]), m => { var arr = Expression.Variable(typeof(double[])); m.Append(Expression.Assign(arr, Expression.NewArray(2, typeof(double)))); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant(1.1))); m.Append(Expression.Assign(Expression.ArrayIndex(arr, 1), Expression.Constant(2.2))); m.Append(arr); }); Assert.Equal(new[] { 1.1, 2.2 }, (double[])InvokeStatic(type, "DA")); }

        #endregion

        #region Loop + Continue

        [Fact]
        public void Loop_WithContinue() { var type = BuildStaticMethod("LC", typeof(int), m => { var sum = Expression.Variable(typeof(int)); var i = Expression.Variable(typeof(int)); m.Append(Expression.Assign(sum, Expression.Constant(0))); m.Append(Expression.Assign(i, Expression.Constant(0))); var loop = Expression.Loop(); loop.Append(Expression.IfThen(Expression.GreaterThanOrEqual(i, Expression.Constant(6)), Expression.Break())); loop.Append(Expression.IncrementAssign(i)); loop.Append(Expression.IfThen(Expression.NotEqual(Expression.Modulo(i, Expression.Constant(2)), Expression.Constant(0)), Expression.Continue())); loop.Append(Expression.AddAssign(sum, i)); m.Append(loop); m.Append(sum); }); Assert.Equal(12, InvokeStatic(type, "LC")); }

        #endregion

        #region MethodCallExpression

        [Fact]
        public void Call_Virtual() { var m2 = typeof(object).GetMethod(nameof(object.ToString)); var type = BuildStaticMethod("VC", typeof(string), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.Call(o, m2)); }); Assert.Equal("42", InvokeStatic(type, "VC", (object)42)); }
        [Fact]
        public void Call_Void() { var m2 = typeof(List<int>).GetMethod(nameof(List<int>.Clear)); var type = BuildStaticMethod("CVoid", typeof(void), m => { var l = m.DefineParameter(typeof(List<int>), "l"); m.Append(Expression.Call(l, m2)); }); var lst = new List<int> { 1 }; InvokeStatic(type, "CVoid", lst); Assert.Empty(lst); }
        [Fact]
        public void Call_ValueTypeReturn() { var m2 = typeof(string).GetMethod(nameof(string.IndexOf), new[] { typeof(char) }); var type = BuildStaticMethod("CIdx", typeof(int), m => { var s = m.DefineParameter(typeof(string), "s"); m.Append(Expression.Call(s, m2, Expression.Constant('l'))); }); Assert.Equal(2, InvokeStatic(type, "CIdx", "hello")); }
        [Fact]
        public void DeclaringCall_Instance() { var m2 = typeof(object).GetMethod(nameof(object.GetHashCode)); var type = BuildStaticMethod("DCI", typeof(int), m => { var o = m.DefineParameter(typeof(object), "o"); m.Append(Expression.DeclaringCall(o, m2)); }); Assert.IsType<int>(InvokeStatic(type, "DCI", (object)"test")); }

        #endregion

        #region InvocationExpression

        [Fact]
        public void Invoke_Static() { var mi = typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) }); var args = Expression.Array(typeof(object), Expression.Convert(Expression.Constant(-5), typeof(object))); var expr = Expression.Invoke(mi, args); Assert.NotNull(expr); }
        [Fact]
        public void Invoke_Instance() { var mi = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes); var inst = Expression.Constant("hello"); var args = Expression.Array(typeof(object)); var expr = Expression.Invoke(inst, mi, args); Assert.NotNull(expr); }
        [Fact]
        public void Invoke_Void() { var mi = typeof(List<int>).GetMethod(nameof(List<int>.Clear)); var type = BuildStaticMethod("IV", typeof(void), m => { var l = m.DefineParameter(typeof(List<int>), "l"); m.Append(Expression.Invoke(l, mi, Expression.Array(typeof(object)))); }); var lst = new List<int> { 1 }; InvokeStatic(type, "IV", lst); Assert.Empty(lst); }

        #endregion

        #region Default

        [Fact]
        public void Default_NullableInt() { var type = BuildStaticMethod("DNI", typeof(int?), m => m.Append(Expression.Default(typeof(int?)))); Assert.Null(InvokeStatic(type, "DNI")); }
        [Fact]
        public void Default_Guid() { var type = BuildStaticMethod("DG", typeof(Guid), m => m.Append(Expression.Default(typeof(Guid)))); Assert.Equal(Guid.Empty, InvokeStatic(type, "DG")); }

        #endregion

        #region Block

        [Fact]
        public void Block_Nested() { var type = BuildStaticMethod("BN", typeof(int), m => { var v = Expression.Variable(typeof(int)); m.Append(Expression.Assign(v, Expression.Constant(0))); var o = Expression.Block(); o.Append(Expression.AddAssign(v, Expression.Constant(1))); var inner = Expression.Block(); inner.Append(Expression.AddAssign(v, Expression.Constant(10))); o.Append(inner); o.Append(Expression.AddAssign(v, Expression.Constant(100))); m.Append(o); m.Append(v); }); Assert.Equal(111, InvokeStatic(type, "BN")); }

        #endregion

        #region Emitters �?PropertyEmitter

        [Fact]
        public void PropertyEmitter_Static()
        {
            var te = _emitter.DefineType($"SP_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var bf = te.DefineField("_sv", typeof(int), FieldAttributes.Private | FieldAttributes.Static);
            var g = te.DefineMethod("get_SV", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(int)); g.Append(bf);
            var s = te.DefineMethod("set_SV", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void)); var vp = s.DefineParameter(typeof(int), "value"); s.Append(Expression.Assign(bf, vp));
            var prop = te.DefineProperty("SV", PropertyAttributes.None, typeof(int)); prop.SetGetMethod(g); prop.SetSetMethod(s);
            var t = te.CreateType(); var pi = t.GetProperty("SV"); pi.SetValue(null, 123); Assert.Equal(123, pi.GetValue(null));
        }

        [Fact]
        public void PropertyEmitter_DefaultValue()
        {
            var te = _emitter.DefineType($"DP_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var bf = te.DefineField("_dv", typeof(string), FieldAttributes.Private);
            var g = te.DefineMethod("get_DV", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(string)); g.Append(bf);
            var s = te.DefineMethod("set_DV", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void)); s.DefineParameter(typeof(string), "value"); s.Append(Expression.Assign(bf, s.GetParameters()[0]));
            var prop = te.DefineProperty("DV", PropertyAttributes.HasDefault, typeof(string)); prop.DefaultValue = "dv"; prop.SetGetMethod(g); prop.SetSetMethod(s);
            Assert.NotNull(te.CreateType());
        }

        [Fact]
        public void PropertyEmitter_ClearDefault()
        {
            var te = _emitter.DefineType($"CD_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var g = te.DefineMethod("get_X", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(int)); g.Append(Expression.Constant(0));
            var s = te.DefineMethod("set_X", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void)); s.DefineParameter(typeof(int), "v"); s.Append(Expression.Default(typeof(void)));
            var prop = te.DefineProperty("X", PropertyAttributes.None, typeof(int)); prop.SetGetMethod(g); prop.SetSetMethod(s); prop.DefaultValue = 42; prop.DefaultValue = null;
            Assert.NotNull(te.CreateType());
        }

        #endregion

        #region Emitters �?EnumEmitter

        [Fact]
        public void EnumEmitter_Long() { var e = _emitter.DefineEnum($"LE_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(long)); e.DefineLiteral("A", 1L); e.DefineLiteral("B", 2L); var t = e.CreateType(); Assert.True(t.IsEnum); Assert.Equal(typeof(long), Enum.GetUnderlyingType(t)); }
        [Fact]
        public void EnumEmitter_NullThrows() { var e = _emitter.DefineEnum($"NE_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(int)); Assert.Throws<ArgumentNullException>(() => e.DefineLiteral("X", null)); }
        [Fact]
        public void EnumEmitter_DoubleCreateThrows() { var e = _emitter.DefineEnum($"DC_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(int)); e.DefineLiteral("A", 1); e.CreateType(); Assert.Throws<InvalidOperationException>(() => e.CreateType()); }
        [Fact]
        public void EnumEmitter_Byte() { var e = _emitter.DefineEnum($"BE_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(byte)); e.DefineLiteral("X", (byte)1); var t = e.CreateType(); Assert.Equal(typeof(byte), Enum.GetUnderlyingType(t)); }

        #endregion

        #region Emitters �?NestedClassEmitter

        [Fact]
        public void NestedClass_BaseType()
        {
            var oe = _emitter.DefineType($"OB_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var ne = oe.DefineNestedType("IB", TypeAttributes.NestedPublic | TypeAttributes.Class, typeof(Exception));
            ne.DefineField("V", typeof(int), FieldAttributes.Public);
            Assert.NotNull(oe.CreateType());
        }

        [Fact]
        public void NestedClass_Interface()
        {
            var oe = _emitter.DefineType($"OI_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var ie = oe.DefineNestedType("II", TypeAttributes.NestedPublic | TypeAttributes.Class, typeof(object), new[] { typeof(IDisposable) });
            ie.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void)).Append(Expression.Default(typeof(void)));
            Assert.NotNull(oe.CreateType());
        }

        [Fact]
        public void NestedClass_Uncompiled()
        {
            var oe = _emitter.DefineType($"OU_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var ie = oe.DefineNestedType("IU", TypeAttributes.NestedPublic | TypeAttributes.Class);
            ie.DefineField("Z", typeof(int), FieldAttributes.Public);
            Assert.NotNull(ie.UncompiledType);
            oe.CreateType();
        }

        #endregion

        #region Emitters �?FieldEmitter

        [Fact]
        public void FieldEmitter_Static()
        {
            var te = _emitter.DefineType($"SF_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var fe = te.DefineField("_s", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
            var g = te.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Static, typeof(int)); g.Append(fe);
            var s = te.DefineMethod("Set", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var v = s.DefineParameter(typeof(int), "v"); s.Append(Expression.Assign(fe, v));
            var t = te.CreateType(); t.GetMethod("Set").Invoke(null, new object[] { 77 }); Assert.Equal(77, t.GetMethod("Get").Invoke(null, null));
        }

        [Fact]
        public void FieldEmitter_Literal()
        {
            var te = _emitter.DefineType($"LF_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var fe = te.DefineField("C", typeof(int), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
            fe.DefaultValue = 42;
            var t = te.CreateType(); Assert.True(t.GetField("C").IsLiteral);
        }

        [Fact]
        public void FieldEmitter_HasDefault()
        {
            var te = _emitter.DefineType($"DF_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var fe = te.DefineField("_d", typeof(int), FieldAttributes.Public | FieldAttributes.HasDefault);
            fe.DefaultValue = 99;
            Assert.NotNull(te.CreateType().GetField("_d"));
        }

        #endregion

        #region Emitters �?MethodCallEmitter (via Expression.Call(MethodEmitter))

        [Fact]
        public void MethodCallEmitter_Static()
        {
            var te = _emitter.DefineType($"MCS_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var am = te.DefineMethod("Add", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var a = am.DefineParameter(typeof(int), "a"); var b = am.DefineParameter(typeof(int), "b"); am.Append(Expression.Add(a, b));
            var cm = te.DefineMethod("CA", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var x = cm.DefineParameter(typeof(int), "x"); var y = cm.DefineParameter(typeof(int), "y");
            cm.Append(Expression.Call(am, x, y));
            var t = te.CreateType(); Assert.Equal(7, t.GetMethod("CA").Invoke(null, new object[] { 3, 4 }));
        }

        [Fact]
        public void MethodCallEmitter_Instance()
        {
            var te = _emitter.DefineType($"MCI_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f = te.DefineField("_v", typeof(int), FieldAttributes.Private);
            var ct = te.DefineConstructor(MethodAttributes.Public); var cp = ct.DefineParameter(typeof(int), ParameterAttributes.None, "v"); ct.Append(Expression.Assign(f, cp));
            var gm = te.DefineMethod("GV", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int)); gm.Append(f);
            var cm = te.DefineMethod("CGV", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int)); cm.Append(Expression.Call(gm));
            var t = te.CreateType(); var i = Activator.CreateInstance(t, 99); Assert.Equal(99, t.GetMethod("CGV").Invoke(i, null));
        }

        #endregion

        #region Emitters �?MethodEmitter

        [Fact]
        public void MethodEmitter_CopyParam()
        {
            var te = _emitter.DefineType($"MP_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod("TP", MethodAttributes.Public | MethodAttributes.Static, typeof(string));
            foreach (var p in typeof(string).GetMethod(nameof(string.Substring), new[] { typeof(int), typeof(int) }).GetParameters()) me.DefineParameter(p);
            me.Append(Expression.Constant("t"));
            Assert.Equal("t", te.CreateType().GetMethod("TP").Invoke(null, new object[] { 0, 0 }));
        }

        #endregion

        #region Emitters �?NewInstanceEmitter (via Expression.New(ConstructorEmitter))

        [Fact]
        public void NewInstanceEmitter_WithParams()
        {
            var te = _emitter.DefineType($"NP_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f = te.DefineField("_x", typeof(int), FieldAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public); var cp = ct.DefineParameter(typeof(int), ParameterAttributes.None, "x"); ct.Append(Expression.Assign(f, cp));
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t, 42);
            Assert.Equal(42, t.GetField("_x").GetValue(inst));
        }

        [Fact]
        public void NewInstanceEmitter_NoParams()
        {
            var te = _emitter.DefineType($"NNP_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f = te.DefineField("_v", typeof(int), FieldAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public); ct.Append(Expression.Assign(f, Expression.Constant(77)));
            var fm = te.DefineMethod("Cr", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            fm.Append(Expression.Constant(0)); // placeholder
            var t = te.CreateType();
            var inst = Activator.CreateInstance(t);
            Assert.Equal(77, t.GetField("_v").GetValue(inst));
        }

        #endregion

        #region Emitters �?ParameterEmitter

        [Fact]
        public void ParameterEmitter_SetConstant()
        {
            var te = _emitter.DefineType($"PD_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod("WD", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var pe = me.DefineParameter(typeof(int), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "val");
            pe.SetConstant(42);
            me.Append(pe);
            Assert.True(te.CreateType().GetMethod("WD").GetParameters()[0].HasDefaultValue);
        }

        [Fact]
        public void ParameterEmitter_Out()
        {
            var te = _emitter.DefineType($"PO_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod("WO", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            me.DefineParameter(typeof(int).MakeByRefType(), ParameterAttributes.Out, "r");
            me.Append(Expression.Default(typeof(void)));
            Assert.NotNull(te.CreateType().GetMethod("WO"));
        }

        #endregion

        #region Emitters �?ClassEmitter & ConstructorEmitter

        [Fact]
        public void ClassEmitter_MultiInterface()
        {
            var te = _emitter.DefineType($"MI_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class, typeof(object), new[] { typeof(IDisposable), typeof(IComparable) });
            te.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void)).Append(Expression.Default(typeof(void)));
            var ct = te.DefineMethod("CompareTo", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int)); ct.DefineParameter(typeof(object), "o"); ct.Append(Expression.Constant(0));
            var t = te.CreateType(); Assert.True(typeof(IDisposable).IsAssignableFrom(t)); Assert.True(typeof(IComparable).IsAssignableFrom(t));
        }

        [Fact]
        public void ConstructorEmitter_Base()
        {
            var te = _emitter.DefineType($"BC_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class, typeof(Exception));
            var ct = te.DefineConstructor(MethodAttributes.Public); ct.DefineParameter(typeof(string), ParameterAttributes.None, "m"); ct.Append(Expression.Default(typeof(void)));
            Assert.NotNull(te.CreateType());
        }

        #endregion

        #region ThisExpression

        [Fact]
        public void This_Instance()
        {
            var te = _emitter.DefineType($"TI_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var f = te.DefineField("_x", typeof(int), FieldAttributes.Public);
            var ct = te.DefineConstructor(MethodAttributes.Public); var cp = ct.DefineParameter(typeof(int), ParameterAttributes.None, "x"); ct.Append(Expression.Assign(f, cp));
            var gm = te.DefineMethod("GV", MethodAttributes.Public, typeof(int)); gm.Append(f);
            var t = te.CreateType(); var i = Activator.CreateInstance(t, 42); Assert.Equal(42, t.GetMethod("GV").Invoke(i, null));
        }

        #endregion

        #region ByRef Parameters

        [Fact]
        public void ByRef_Int() { var te = _emitter.DefineType($"RI_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("AR", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(int).MakeByRefType(), "v"); var ap = me.DefineParameter(typeof(int), "a"); me.Append(Expression.Assign(rp, ap)); var t = te.CreateType(); var args = new object[] { 10, 5 }; t.GetMethod("AR").Invoke(null, args); Assert.Equal(5, args[0]); }
        [Fact]
        public void ByRef_String() { var te = _emitter.DefineType($"RS_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("SR", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(string).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant("mod"))); var t = te.CreateType(); var args = new object[] { "orig" }; t.GetMethod("SR").Invoke(null, args); Assert.Equal("mod", args[0]); }
        [Fact]
        public void ByRef_Long() { var te = _emitter.DefineType($"RL_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("LR", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(long).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant(999L))); var t = te.CreateType(); var args = new object[] { 0L }; t.GetMethod("LR").Invoke(null, args); Assert.Equal(999L, args[0]); }
        [Fact]
        public void ByRef_Double() { var te = _emitter.DefineType($"RD_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("DR", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(double).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant(3.14))); var t = te.CreateType(); var args = new object[] { 0.0 }; t.GetMethod("DR").Invoke(null, args); Assert.Equal(3.14, args[0]); }
        [Fact]
        public void ByRef_Bool() { var te = _emitter.DefineType($"RB_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("BR", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(bool).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant(true))); var t = te.CreateType(); var args = new object[] { false }; t.GetMethod("BR").Invoke(null, args); Assert.True((bool)args[0]); }
        [Fact]
        public void ByRef_Float() { var te = _emitter.DefineType($"RF_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("FR", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(float).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant(1.5f))); var t = te.CreateType(); var args = new object[] { 0.0f }; t.GetMethod("FR").Invoke(null, args); Assert.Equal(1.5f, args[0]); }
        [Fact]
        public void ByRef_Short() { var te = _emitter.DefineType($"RSh_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("ShR", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(short).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant((short)42))); var t = te.CreateType(); var args = new object[] { (short)0 }; t.GetMethod("ShR").Invoke(null, args); Assert.Equal((short)42, args[0]); }
        [Fact]
        public void ByRef_Byte() { var te = _emitter.DefineType($"RBy_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); var me = te.DefineMethod("ByR", MethodAttributes.Public | MethodAttributes.Static, typeof(void)); var rp = me.DefineParameter(typeof(byte).MakeByRefType(), "v"); me.Append(Expression.Assign(rp, Expression.Constant((byte)255))); var t = te.CreateType(); var args = new object[] { (byte)0 }; t.GetMethod("ByR").Invoke(null, args); Assert.Equal((byte)255, args[0]); }

        #endregion

        #region Compound Assign �?more types

        [Fact]
        public void SubtractAssign_Long() { var type = BuildStaticMethod("SAL", typeof(long), m => { var v = Expression.Variable(typeof(long)); m.Append(Expression.Assign(v, Expression.Constant(100L))); m.Append(Expression.SubtractAssign(v, Expression.Constant(30L))); m.Append(v); }); Assert.Equal(70L, InvokeStatic(type, "SAL")); }
        [Fact]
        public void MultiplyAssign_Double() { var type = BuildStaticMethod("MAD", typeof(double), m => { var v = Expression.Variable(typeof(double)); m.Append(Expression.Assign(v, Expression.Constant(2.5))); m.Append(Expression.MultiplyAssign(v, Expression.Constant(3.0))); m.Append(v); }); Assert.Equal(7.5, InvokeStatic(type, "MAD")); }
        [Fact]
        public void DivideAssign_Double() { var type = BuildStaticMethod("DAD", typeof(double), m => { var v = Expression.Variable(typeof(double)); m.Append(Expression.Assign(v, Expression.Constant(10.0))); m.Append(Expression.DivideAssign(v, Expression.Constant(4.0))); m.Append(v); }); Assert.Equal(2.5, InvokeStatic(type, "DAD")); }

        #endregion

        #region Throw

        [Fact]
        public void Throw_TypeAndMsg() { var type = BuildStaticMethod("TM", typeof(void), m => m.Append(Expression.Throw(typeof(ArgumentException), "bad"))); Assert.IsType<ArgumentException>(Assert.Throws<TargetInvocationException>(() => InvokeStatic(type, "TM")).InnerException); }
        [Fact]
        public void Throw_TypeOnly() { var type = BuildStaticMethod("TT", typeof(void), m => m.Append(Expression.Throw(typeof(InvalidOperationException)))); Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => InvokeStatic(type, "TT")).InnerException); }
        [Fact]
        public void Throw_Expr() { var type = BuildStaticMethod("TE", typeof(void), m => m.Append(Expression.Throw(Expression.New(typeof(InvalidOperationException))))); Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => InvokeStatic(type, "TE")).InnerException); }

        #endregion

        #region Expression factory misc

        [Fact]
        public void New_CtorInfo() { var c = typeof(object).GetConstructor(Type.EmptyTypes); var type = BuildStaticMethod("NC", typeof(object), m => m.Append(Expression.New(c))); Assert.NotNull(InvokeStatic(type, "NC")); }
        [Fact]
        public void EmptyAsts() { Assert.Empty(Expression.EmptyAsts); }
        [Fact]
        public void ModuleEmitter_Basic() { var mod = new ModuleEmitter($"M_{Guid.NewGuid():N}"); var te = mod.DefineType($"T_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class); Assert.NotNull(te.CreateType()); }

        #endregion
    }

    #region 辅助类型

    public class CoverageHelper
    {
        public static int StaticValue;
        public int InstanceValue;
        public static readonly int ReadOnlyValue = 100;
    }

    public class CoverageFieldTarget
    {
        public int X;
    }

    #endregion
}
