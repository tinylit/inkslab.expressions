using Inkslab.Emitters;
using System;
using System.Reflection;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Coverage push: AbstractTypeEmitter, TypeCompiler, EmitUtils deep paths,
    /// ParameterExpression positions, BinaryExpression remaining paths,
    /// ArrayExpression, MemberExpression, ModuleEmitter.
    /// </summary>
    public class CoverageBoostTests15
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB15_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var cls = _mod.DefineType($"C15_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return cls.CreateType();
        }

        private Type BuildStaticP(string name, Type ret, Type p1, Action<MethodEmitter, ParameterExpression> body)
        {
            var cls = _mod.DefineType($"C15P_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            var param = me.DefineParameter(p1, ParameterAttributes.None, "a");
            body(me, param);
            return cls.CreateType();
        }

        private Type BuildStaticP2(string name, Type ret, Type p1, Type p2, Action<MethodEmitter, ParameterExpression, ParameterExpression> body)
        {
            var cls = _mod.DefineType($"C15P2_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            var a = me.DefineParameter(p1, ParameterAttributes.None, "a");
            var b = me.DefineParameter(p2, ParameterAttributes.None, "b");
            body(me, a, b);
            return cls.CreateType();
        }

        private object Invoke(Type t, params object[] args) => t.GetMethod("Run").Invoke(null, args);

        // ===== AbstractTypeEmitter: properties & field overloads =====

        [Fact]
        public void ATE_DefineField_NonSerializable()
        {
            var cls = _mod.DefineType("ATE_FNS", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var f = cls.DefineField("_ns", typeof(int), false);
            var t = cls.CreateType();
            var fi = t.GetField("_ns", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(fi);
            Assert.True(fi.IsNotSerialized);
        }

        [Fact]
        public void ATE_DefineField_WithFieldInfo()
        {
            var cls = _mod.DefineType("ATE_FFI", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            // Create a simple field then create type
            var f = cls.DefineField("myField", typeof(string), FieldAttributes.Public);
            var t = cls.CreateType();
            Assert.NotNull(t.GetField("myField"));
        }

        [Fact]
        public void ATE_DefineMethod_MultipleOverloads()
        {
            var cls = _mod.DefineType("ATE_MO", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            var m1 = cls.DefineMethod("A", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            m1.Append(Expression.Constant(1));

            var m2 = cls.DefineMethod("B", MethodAttributes.Public | MethodAttributes.Virtual, typeof(string));
            m2.Append(Expression.Constant("ok"));

            var t = cls.CreateType();
            Assert.Equal(1, t.GetMethod("A").Invoke(null, null));
            var inst = Activator.CreateInstance(t);
            Assert.Equal("ok", t.GetMethod("B").Invoke(inst, null));
        }

        [Fact]
        public void ATE_DefineProperty()
        {
            var cls = _mod.DefineType("ATE_DP", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var field = cls.DefineField("_val", typeof(int));

            var prop = cls.DefineProperty("Value", PropertyAttributes.None, typeof(int));
            var getter = cls.DefineMethod("get_Value", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, typeof(int));
            getter.Append(field);
            var setter = cls.DefineMethod("set_Value", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, typeof(void));
            var sv = setter.DefineParameter(typeof(int), ParameterAttributes.None, "value");
            setter.Append(Expression.Assign(field, sv));

            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);

            var t = cls.CreateType();
            var inst = Activator.CreateInstance(t);
            t.GetProperty("Value").SetValue(inst, 42);
            Assert.Equal(42, t.GetProperty("Value").GetValue(inst));
        }

        [Fact]
        public void ATE_NestedType()
        {
            var cls = _mod.DefineType("ATE_NT", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            var nested = cls.DefineNestedType("Inner");
            var nctor = nested.DefineConstructor(MethodAttributes.Public);
            nctor.InvokeBaseConstructor();

            var t = cls.CreateType();
            var nt = t.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotEmpty(nt);
        }

        [Fact]
        public void ATE_TypeInitializer_NonEmpty()
        {
            var cls = _mod.DefineType("ATE_TI2", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var sf = cls.DefineField("_sf", typeof(int), FieldAttributes.Static | FieldAttributes.Private);

            cls.TypeInitializer.Append(Expression.Assign(sf, Expression.Constant(99)));

            var run = cls.DefineMethod("GetSF", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            run.Append(sf);

            var t = cls.CreateType();
            Assert.Equal(99, t.GetMethod("GetSF").Invoke(null, null));
        }

        [Fact]
        public void ATE_BaseType_WithParent()
        {
            var cls = _mod.DefineType("ATE_BT", TypeAttributes.Public, typeof(Exception));
            Assert.Equal(typeof(Exception), cls.BaseType);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var t = cls.CreateType();
            Assert.True(typeof(Exception).IsAssignableFrom(t));
        }

        [Fact]
        public void ATE_WithInterface()
        {
            var cls = _mod.DefineType("ATE_IF", TypeAttributes.Public, null, new[] { typeof(ICloneable) });
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var clone = cls.DefineMethod("Clone", MethodAttributes.Public | MethodAttributes.Virtual, typeof(object));
            clone.Append(Expression.Constant(null, typeof(object)));
            var t = cls.CreateType();
            Assert.True(typeof(ICloneable).IsAssignableFrom(t));
        }

        // ===== TypeCompiler: GetConstructor, GetField, GetMethod with non-generic types =====

        [Fact]
        public void TypeCompiler_GetConstructor_NonGeneric()
        {
            var ctor = typeof(Exception).GetConstructor(new[] { typeof(string) });
            var result = TypeCompiler.GetConstructor(typeof(Exception), ctor);
            Assert.Equal(ctor, result);
        }

        [Fact]
        public void TypeCompiler_GetField_NonGeneric()
        {
            var fieldInfo = typeof(string).GetField("Empty");
            var result = TypeCompiler.GetField(typeof(string), fieldInfo);
            Assert.Equal(fieldInfo, result);
        }

        [Fact]
        public void TypeCompiler_GetMethod_NonGeneric()
        {
            var mi = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var result = TypeCompiler.GetMethod(typeof(string), mi);
            Assert.Equal(mi, result);
        }

        [Fact]
        public void TypeCompiler_GetConstructor_Null_Type()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetConstructor(null, typeof(object).GetConstructor(Type.EmptyTypes)));
        }

        [Fact]
        public void TypeCompiler_GetConstructor_Null_Ctor()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetConstructor(typeof(object), null));
        }

        [Fact]
        public void TypeCompiler_GetField_Null_Type()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetField(null, typeof(string).GetField("Empty")));
        }

        [Fact]
        public void TypeCompiler_GetField_Null_Field()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetField(typeof(string), null));
        }

        [Fact]
        public void TypeCompiler_GetMethod_Null_Type()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetMethod(null, typeof(object).GetMethod("ToString")));
        }

        [Fact]
        public void TypeCompiler_GetMethod_Null_Method()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetMethod(typeof(object), null));
        }

        [Fact]
        public void TypeCompiler_GetReturnType_NonGeneric()
        {
            var mi = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var result = TypeCompiler.GetReturnType(mi, Type.EmptyTypes, Type.EmptyTypes);
            Assert.Equal(typeof(bool), result);
        }

        [Fact]
        public void TypeCompiler_GetReturnType_Null()
        {
            Assert.Throws<ArgumentNullException>(() => TypeCompiler.GetReturnType(null, null, null));
        }

        [Fact]
        public void TypeCompiler_GetConstructor_GenericType()
        {
            var listType = typeof(System.Collections.Generic.List<>);
            var listInt = typeof(System.Collections.Generic.List<int>);
            var ctor = listType.GetConstructor(Type.EmptyTypes);
            var result = TypeCompiler.GetConstructor(listInt, ctor);
            Assert.NotNull(result);
        }

        [Fact]
        public void TypeCompiler_GetMethod_GenericType()
        {
            var listStr = typeof(System.Collections.Generic.List<string>);
            var mi = listStr.GetMethod("Add");
            var result = TypeCompiler.GetMethod(listStr, mi);
            Assert.NotNull(result);
            Assert.Equal("Add", result.Name);
        }

        // ===== EmitUtils: AreEquivalent, EqualSignatureTypes =====

        [Fact]
        public void EmitUtils_AreEquivalent_Same()
        {
            Assert.True(EmitUtils.AreEquivalent(typeof(int), typeof(int)));
        }

        [Fact]
        public void EmitUtils_AreEquivalent_Different()
        {
            Assert.False(EmitUtils.AreEquivalent(typeof(int), typeof(string)));
        }

        [Fact]
        public void EmitUtils_EqualSignatureTypes_Same()
        {
            Assert.True(EmitUtils.EqualSignatureTypes(typeof(int), typeof(int)));
        }

        [Fact]
        public void EmitUtils_EqualSignatureTypes_Different()
        {
            Assert.False(EmitUtils.EqualSignatureTypes(typeof(int), typeof(string)));
        }

        [Fact]
        public void EmitUtils_EqualSignatureTypes_GenericTypes()
        {
            var t1 = typeof(System.Collections.Generic.List<int>);
            var t2 = typeof(System.Collections.Generic.List<int>);
            Assert.True(EmitUtils.EqualSignatureTypes(t1, t2));
        }

        [Fact]
        public void EmitUtils_EqualSignatureTypes_DifferentGenericArgs()
        {
            var t1 = typeof(System.Collections.Generic.List<int>);
            var t2 = typeof(System.Collections.Generic.List<string>);
            Assert.False(EmitUtils.EqualSignatureTypes(t1, t2));
        }

        [Fact]
        public void EmitUtils_IsAssignableFromSignatureTypes_Same()
        {
            Assert.True(EmitUtils.IsAssignableFromSignatureTypes(typeof(object), typeof(string)));
        }

        [Fact]
        public void EmitUtils_IsAssignableFromSignatureTypes_NotAssignable()
        {
            Assert.False(EmitUtils.IsAssignableFromSignatureTypes(typeof(int), typeof(string)));
        }

        // ===== BinaryExpression: more operator paths =====

        [Fact]
        public void Binary_Divide_Long()
        {
            var t = BuildStaticP2("DivL", typeof(long), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.Divide(a, b));
            });
            Assert.Equal(5L, Invoke(t, 10L, 2L));
        }

        [Fact]
        public void Binary_Modulo_Long()
        {
            var t = BuildStaticP2("ModL", typeof(long), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.Modulo(a, b));
            });
            Assert.Equal(1L, Invoke(t, 7L, 3L));
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
        public void Binary_Subtract_Long()
        {
            var t = BuildStaticP2("SubL", typeof(long), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.Subtract(a, b));
            });
            Assert.Equal(1L, Invoke(t, 3L, 2L));
        }

        [Fact]
        public void Binary_Equal_Long()
        {
            var t = BuildStaticP2("EqL", typeof(bool), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.Equal(a, b));
            });
            Assert.Equal(true, Invoke(t, 5L, 5L));
            Assert.Equal(false, Invoke(t, 5L, 6L));
        }

        [Fact]
        public void Binary_NotEqual_Long()
        {
            var t = BuildStaticP2("NeqL", typeof(bool), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.NotEqual(a, b));
            });
            Assert.Equal(false, Invoke(t, 5L, 5L));
            Assert.Equal(true, Invoke(t, 5L, 6L));
        }

        [Fact]
        public void Binary_GreaterThanOrEqual_Long()
        {
            var t = BuildStaticP2("GteL", typeof(bool), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.GreaterThanOrEqual(a, b));
            });
            Assert.Equal(true, Invoke(t, 5L, 5L));
            Assert.Equal(true, Invoke(t, 6L, 5L));
            Assert.Equal(false, Invoke(t, 4L, 5L));
        }

        [Fact]
        public void Binary_LessThanOrEqual_Long()
        {
            var t = BuildStaticP2("LteL", typeof(bool), typeof(long), typeof(long), (me, a, b) =>
            {
                me.Append(Expression.LessThanOrEqual(a, b));
            });
            Assert.Equal(true, Invoke(t, 5L, 5L));
            Assert.Equal(true, Invoke(t, 4L, 5L));
            Assert.Equal(false, Invoke(t, 6L, 5L));
        }

        [Fact]
        public void Binary_Add_Double()
        {
            var t = BuildStaticP2("AddD", typeof(double), typeof(double), typeof(double), (me, a, b) =>
            {
                me.Append(Expression.Add(a, b));
            });
            Assert.Equal(5.5, Invoke(t, 3.0, 2.5));
        }

        [Fact]
        public void Binary_Subtract_Double()
        {
            var t = BuildStaticP2("SubD", typeof(double), typeof(double), typeof(double), (me, a, b) =>
            {
                me.Append(Expression.Subtract(a, b));
            });
            Assert.Equal(0.5, Invoke(t, 3.0, 2.5));
        }

        [Fact]
        public void Binary_Multiply_Double()
        {
            var t = BuildStaticP2("MulD", typeof(double), typeof(double), typeof(double), (me, a, b) =>
            {
                me.Append(Expression.Multiply(a, b));
            });
            Assert.Equal(6.0, Invoke(t, 3.0, 2.0));
        }

        [Fact]
        public void Binary_Divide_Double()
        {
            var t = BuildStaticP2("DivD", typeof(double), typeof(double), typeof(double), (me, a, b) =>
            {
                me.Append(Expression.Divide(a, b));
            });
            Assert.Equal(5.0, Invoke(t, 10.0, 2.0));
        }

        [Fact]
        public void Binary_LessThan_Double()
        {
            var t = BuildStaticP2("LtD", typeof(bool), typeof(double), typeof(double), (me, a, b) =>
            {
                me.Append(Expression.LessThan(a, b));
            });
            Assert.Equal(true, Invoke(t, 1.0, 2.0));
            Assert.Equal(false, Invoke(t, 2.0, 1.0));
        }

        [Fact]
        public void Binary_GreaterThan_Double()
        {
            var t = BuildStaticP2("GtD", typeof(bool), typeof(double), typeof(double), (me, a, b) =>
            {
                me.Append(Expression.GreaterThan(a, b));
            });
            Assert.Equal(true, Invoke(t, 3.0, 1.0));
            Assert.Equal(false, Invoke(t, 1.0, 3.0));
        }

        // ===== BinaryExpression: float arithmetic =====

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
        public void Binary_Multiply_Float()
        {
            var t = BuildStaticP2("MulF", typeof(float), typeof(float), typeof(float), (me, a, b) =>
            {
                me.Append(Expression.Multiply(a, b));
            });
            Assert.Equal(6.0f, Invoke(t, 3.0f, 2.0f));
        }

        // ===== ParameterExpression: various positions (4+) =====

        [Fact]
        public void ParameterExpression_Position4()
        {
            var cls = _mod.DefineType("PE_P4", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p0 = me.DefineParameter(typeof(int), ParameterAttributes.None, "a");
            var p1 = me.DefineParameter(typeof(int), ParameterAttributes.None, "b");
            var p2 = me.DefineParameter(typeof(int), ParameterAttributes.None, "c");
            var p3 = me.DefineParameter(typeof(int), ParameterAttributes.None, "d");
            var p4 = me.DefineParameter(typeof(int), ParameterAttributes.None, "e");
            me.Append(p4);
            var t = cls.CreateType();
            Assert.Equal(99, t.GetMethod("Run").Invoke(null, new object[] { 1, 2, 3, 4, 99 }));
        }

        [Fact]
        public void ParameterExpression_Position0to3()
        {
            var cls = _mod.DefineType("PE_P03", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p0 = me.DefineParameter(typeof(int), ParameterAttributes.None, "a");
            var p1 = me.DefineParameter(typeof(int), ParameterAttributes.None, "b");
            var p2 = me.DefineParameter(typeof(int), ParameterAttributes.None, "c");
            var p3 = me.DefineParameter(typeof(int), ParameterAttributes.None, "d");
            // Sum all
            me.Append(Expression.Add(Expression.Add(p0, p1), Expression.Add(p2, p3)));
            var t = cls.CreateType();
            Assert.Equal(10, t.GetMethod("Run").Invoke(null, new object[] { 1, 2, 3, 4 }));
        }

        // ===== ArrayExpression: object array with boxing =====

        [Fact]
        public void Array_ObjectBoxing()
        {
            var t = BuildStatic("ArrOB", typeof(object[]), me =>
            {
                me.Append(Expression.Array(typeof(object),
                    Expression.Convert(Expression.Constant(42), typeof(object)),
                    Expression.Convert(Expression.Constant(3.14), typeof(object)),
                    Expression.Constant("hello")));
            });
            var arr = (object[])Invoke(t);
            Assert.Equal(3, arr.Length);
            Assert.Equal(42, arr[0]);
            Assert.Equal(3.14, arr[1]);
            Assert.Equal("hello", arr[2]);
        }

        [Fact]
        public void Array_EmptyObject()
        {
            var t = BuildStatic("ArrEO", typeof(object[]), me =>
            {
                me.Append(Expression.Array(typeof(object)));
            });
            var arr = (object[])Invoke(t);
            Assert.Empty(arr);
        }

        // ===== ConvertExpression: more numeric paths =====

        [Fact]
        public void Convert_UIntToLong()
        {
            var t = BuildStaticP("UI2L", typeof(long), typeof(uint), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(long)));
            });
            Assert.Equal(42L, Invoke(t, 42u));
        }

        [Fact]
        public void Convert_ShortToLong()
        {
            var t = BuildStaticP("S2L", typeof(long), typeof(short), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(long)));
            });
            Assert.Equal(42L, Invoke(t, (short)42));
        }

        [Fact]
        public void Convert_IntToChar()
        {
            var t = BuildStaticP("I2C", typeof(char), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(char)));
            });
            Assert.Equal('A', Invoke(t, 65));
        }

        // ===== FieldExpression: static field =====

        [Fact]
        public void FieldExpression_StaticField()
        {
            var t = BuildStatic("FE_SF", typeof(string), me =>
            {
                var fi = typeof(string).GetField("Empty");
                me.Append(Expression.Field(fi));
            });
            Assert.Equal("", Invoke(t));
        }

        // ===== Constant: more types for EmitUtils coverage =====

        [Fact]
        public void Constant_Int_SpecialValues()
        {
            // Test special int values: -1, 0-8, sbyte range, larger
            var t1 = BuildStatic("CI_M1", typeof(int), me => me.Append(Expression.Constant(-1)));
            Assert.Equal(-1, Invoke(t1));

            var t2 = BuildStatic("CI_0", typeof(int), me => me.Append(Expression.Constant(0)));
            Assert.Equal(0, Invoke(t2));

            var t3 = BuildStatic("CI_8", typeof(int), me => me.Append(Expression.Constant(8)));
            Assert.Equal(8, Invoke(t3));

            var t4 = BuildStatic("CI_100", typeof(int), me => me.Append(Expression.Constant(100)));
            Assert.Equal(100, Invoke(t4));

            var t5 = BuildStatic("CI_1000", typeof(int), me => me.Append(Expression.Constant(1000)));
            Assert.Equal(1000, Invoke(t5));
        }

        [Fact]
        public void Constant_DecimalInt()
        {
            var d = 42m;
            var t = BuildStatic("CDI", typeof(decimal), me => me.Append(Expression.Constant(d)));
            Assert.Equal(d, Invoke(t));
        }

        // ===== Default for more types =====

        [Fact]
        public void Default_Bool()
        {
            var t = BuildStatic("DB", typeof(bool), me => me.Append(Expression.Default(typeof(bool))));
            Assert.Equal(false, Invoke(t));
        }

        [Fact]
        public void Default_Byte()
        {
            var t = BuildStatic("DBY", typeof(byte), me => me.Append(Expression.Default(typeof(byte))));
            Assert.Equal((byte)0, Invoke(t));
        }

        [Fact]
        public void Default_Char()
        {
            var t = BuildStatic("DC", typeof(char), me => me.Append(Expression.Default(typeof(char))));
            Assert.Equal('\0', Invoke(t));
        }

        [Fact]
        public void Default_Short()
        {
            var t = BuildStatic("DSH", typeof(short), me => me.Append(Expression.Default(typeof(short))));
            Assert.Equal((short)0, Invoke(t));
        }

        [Fact]
        public void Default_Decimal()
        {
            var t = BuildStatic("DDC", typeof(decimal), me => me.Append(Expression.Default(typeof(decimal))));
            Assert.Equal(0m, Invoke(t));
        }

        [Fact]
        public void Default_UInt()
        {
            var t = BuildStatic("DUI", typeof(uint), me => me.Append(Expression.Default(typeof(uint))));
            Assert.Equal(0u, Invoke(t));
        }

        [Fact]
        public void Default_ULong()
        {
            var t = BuildStatic("DUL", typeof(ulong), me => me.Append(Expression.Default(typeof(ulong))));
            Assert.Equal(0UL, Invoke(t));
        }

        [Fact]
        public void Default_SByte()
        {
            var t = BuildStatic("DSB", typeof(sbyte), me => me.Append(Expression.Default(typeof(sbyte))));
            Assert.Equal((sbyte)0, Invoke(t));
        }

        [Fact]
        public void Default_UShort()
        {
            var t = BuildStatic("DUS", typeof(ushort), me => me.Append(Expression.Default(typeof(ushort))));
            Assert.Equal((ushort)0, Invoke(t));
        }

        // ===== Expression: factory method coverage =====

        [Fact]
        public void Expression_NewExpression()
        {
            var ctor = typeof(Exception).GetConstructor(new[] { typeof(string) });
            var t = BuildStatic("NewEx", typeof(string), me =>
            {
                var newExpr = Expression.New(ctor, Expression.Constant("err"));
                me.Append(Expression.Property(
                    Expression.Convert(newExpr, typeof(Exception)),
                    typeof(Exception).GetProperty("Message")));
            });
            Assert.Equal("err", Invoke(t));
        }

        [Fact]
        public void Expression_Coalesce()
        {
            var t = BuildStaticP("Coal2", typeof(string), typeof(string), (me, a) =>
            {
                me.Append(Expression.Coalesce(a, Expression.Constant("fallback")));
            });
            Assert.Equal("hello", Invoke(t, "hello"));
            Assert.Equal("fallback", Invoke(t, (string)null));
        }

        [Fact]
        public void Expression_TypeAs()
        {
            var t = BuildStaticP("TAs2", typeof(string), typeof(object), (me, a) =>
            {
                me.Append(Expression.TypeAs(a, typeof(string)));
            });
            Assert.Equal("hello", Invoke(t, (object)"hello"));
            Assert.Null(Invoke(t, (object)42));
        }
    }
}
