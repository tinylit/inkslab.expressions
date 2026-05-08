using Inkslab.Emitters;
using System;
using System.Reflection;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 针对 EmitUtils 常量发射的更多类型支持，以及 ParameterEmitter 默认值的边界测试。
    /// </summary>
    public class ConstantEmissionExtraTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CE_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, params object[] args) => t.GetMethod("Run").Invoke(null, args);

        // ===== 各种数组常量 =====

        [Fact]
        public void Constant_IntArray()
        {
            var arr = new[] { 1, 2, 3 };
            var t = BuildStatic("CIA", typeof(int[]), m => m.Append(Expression.Constant(arr)));
            Assert.Equal(arr, (int[])Invoke(t));
        }

        [Fact]
        public void Constant_StringArray()
        {
            var arr = new[] { "a", "b", "c" };
            var t = BuildStatic("CSA", typeof(string[]), m => m.Append(Expression.Constant(arr)));
            Assert.Equal(arr, (string[])Invoke(t));
        }

        [Fact]
        public void Constant_DoubleArray()
        {
            var arr = new[] { 1.5, 2.5 };
            var t = BuildStatic("CDA", typeof(double[]), m => m.Append(Expression.Constant(arr)));
            Assert.Equal(arr, (double[])Invoke(t));
        }

        [Fact]
        public void Constant_LongArray()
        {
            var arr = new long[] { 100L, 200L };
            var t = BuildStatic("CLA", typeof(long[]), m => m.Append(Expression.Constant(arr)));
            Assert.Equal(arr, (long[])Invoke(t));
        }

        [Fact]
        public void Constant_ByteArray()
        {
            var arr = new byte[] { 1, 2, 3 };
            var t = BuildStatic("CBA", typeof(byte[]), m => m.Append(Expression.Constant(arr)));
            Assert.Equal(arr, (byte[])Invoke(t));
        }

        [Fact]
        public void Constant_ShortArray()
        {
            var arr = new short[] { 10, 20, 30 };
            var t = BuildStatic("CShA", typeof(short[]), m => m.Append(Expression.Constant(arr)));
            Assert.Equal(arr, (short[])Invoke(t));
        }

        [Fact]
        public void Constant_FloatArray()
        {
            var arr = new[] { 1.5f, 2.5f };
            var t = BuildStatic("CFA", typeof(float[]), m => m.Append(Expression.Constant(arr)));
            Assert.Equal(arr, (float[])Invoke(t));
        }

        [Fact]
        public void Constant_BoolArray()
        {
            var arr = new[] { true, false, true };
            var t = BuildStatic("CBoA", typeof(bool[]), m => m.Append(Expression.Constant(arr)));
            Assert.Equal(arr, (bool[])Invoke(t));
        }

        [Fact]
        public void Constant_TypeArray()
        {
            var arr = new[] { typeof(int), typeof(string) };
            var t = BuildStatic("CTA", typeof(Type[]), m => m.Append(Expression.Constant(arr)));
            Assert.Equal(arr, (Type[])Invoke(t));
        }

        // ===== 特殊类型常量 =====

        [Fact]
        public void Constant_TimeSpan_Big()
        {
            var ts = TimeSpan.FromDays(365);
            var t = BuildStatic("CTS2", typeof(TimeSpan), m => m.Append(Expression.Constant(ts)));
            Assert.Equal(ts, Invoke(t));
        }

        [Fact]
        public void Constant_DateTimeOffset_Specific()
        {
            var dto = new DateTimeOffset(2025, 5, 1, 12, 0, 0, TimeSpan.FromHours(8));
            var t = BuildStatic("CDTO", typeof(DateTimeOffset), m => m.Append(Expression.Constant(dto)));
            Assert.Equal(dto, Invoke(t));
        }

        [Fact]
        public void Constant_IntPtr()
        {
            var p = new IntPtr(0x12345);
            var t = BuildStatic("CIP", typeof(IntPtr), m => m.Append(Expression.Constant(p)));
            Assert.Equal(p, Invoke(t));
        }

        [Fact]
        public void Constant_UIntPtr()
        {
            var p = new UIntPtr(0x12345);
            var t = BuildStatic("CUIP", typeof(UIntPtr), m => m.Append(Expression.Constant(p)));
            Assert.Equal(p, Invoke(t));
        }

        [Fact]
        public void Constant_LargeIntegerConstant()
        {
            // Triggers Ldc_I4 (not Ldc_I4_S)
            var t = BuildStatic("CLI", typeof(int), m => m.Append(Expression.Constant(int.MaxValue)));
            Assert.Equal(int.MaxValue, Invoke(t));
        }

        [Fact]
        public void Constant_NegativeBigInt()
        {
            var t = BuildStatic("CNBI", typeof(int), m => m.Append(Expression.Constant(int.MinValue)));
            Assert.Equal(int.MinValue, Invoke(t));
        }

        [Fact]
        public void Constant_LargeDecimal()
        {
            // 大于 long.MaxValue 的 decimal 走 EmitDecimalBits 路径
            var d = decimal.MaxValue;
            var t = BuildStatic("CLD", typeof(decimal), m => m.Append(Expression.Constant(d)));
            Assert.Equal(d, Invoke(t));
        }

        [Fact]
        public void Constant_DecimalNonInteger()
        {
            // 含小数部分的 decimal 走 EmitDecimalBits 路径
            var d = 3.14159m;
            var t = BuildStatic("CDN", typeof(decimal), m => m.Append(Expression.Constant(d)));
            Assert.Equal(d, Invoke(t));
        }

        [Fact]
        public void Constant_LongDecimal()
        {
            // 在 [int.MinValue, long.MaxValue] 范围的 decimal 走 EmitLong + ctor(long)
            var d = ((decimal)int.MaxValue) * 1000m;
            var t = BuildStatic("CLD2", typeof(decimal), m => m.Append(Expression.Constant(d)));
            Assert.Equal(d, Invoke(t));
        }

        [Fact]
        public void Constant_Ulong_Zero()
        {
            var t = BuildStatic("CUZ", typeof(ulong), m => m.Append(Expression.Constant((ulong)0)));
            Assert.Equal(0UL, Invoke(t));
        }

        [Fact]
        public void Constant_Char()
        {
            var t = BuildStatic("CCh", typeof(char), m => m.Append(Expression.Constant('Z')));
            Assert.Equal('Z', Invoke(t));
        }

        [Fact]
        public void Constant_Uint()
        {
            var t = BuildStatic("CUI2", typeof(uint), m => m.Append(Expression.Constant(uint.MaxValue)));
            Assert.Equal(uint.MaxValue, Invoke(t));
        }

        [Fact]
        public void Constant_Ushort()
        {
            var t = BuildStatic("CUSh", typeof(ushort), m => m.Append(Expression.Constant((ushort)42)));
            Assert.Equal((ushort)42, Invoke(t));
        }

        [Fact]
        public void Constant_Sbyte()
        {
            var t = BuildStatic("CSb", typeof(sbyte), m => m.Append(Expression.Constant((sbyte)-1)));
            Assert.Equal((sbyte)-1, Invoke(t));
        }

        // ===== Nullable 常量 =====

        [Fact]
        public void Constant_NullableInt_HasValue()
        {
            int? x = 42;
            var t = BuildStatic("CNIH", typeof(int?), m => m.Append(Expression.Constant(x, typeof(int?))));
            Assert.Equal(x, (int?)Invoke(t));
        }

        [Fact]
        public void Constant_NullableInt_Null()
        {
            var t = BuildStatic("CNIN", typeof(int?), m => m.Append(Expression.Constant(null, typeof(int?))));
            Assert.Null((int?)Invoke(t));
        }

        // ===== Object boxing =====

        [Fact]
        public void Constant_IntBoxedAsObject()
        {
            var t = BuildStatic("CIBO", typeof(object), m => m.Append(Expression.Constant(42, typeof(object))));
            Assert.Equal(42, Invoke(t));
        }

        [Fact]
        public void Constant_NullAsObject()
        {
            var t = BuildStatic("CNO", typeof(object), m => m.Append(Expression.Constant(null, typeof(object))));
            Assert.Null(Invoke(t));
        }

        // ===== ParameterEmitter SetConstant =====

        [Fact]
        public void ParameterEmitter_SetConstant_NullForRefType()
        {
            var cls = _mod.DefineType($"PE_NRT_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(string));
            var p = me.DefineParameter(typeof(string), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "x");
            p.SetConstant(null);
            me.Append(p);
            var t = cls.CreateType();
            Assert.Equal("hi", t.GetMethod("Run").Invoke(null, new object[] { "hi" }));
        }

        [Fact]
        public void ParameterEmitter_SetConstant_NullForNullable()
        {
            var cls = _mod.DefineType($"PE_NN_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int?));
            var p = me.DefineParameter(typeof(int?), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "x");
            p.SetConstant(null);
            me.Append(p);
            var t = cls.CreateType();
            Assert.Equal(42, (int?)t.GetMethod("Run").Invoke(null, new object[] { 42 }));
        }

        [Fact]
        public void ParameterEmitter_SetConstant_NullForValueType_Throws()
        {
            // null 不能作为 int 的默认值
            var cls = _mod.DefineType($"PE_NVT_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p = me.DefineParameter(typeof(int), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "x");
            Assert.Throws<NotSupportedException>(() => p.SetConstant(null));
        }

        [Fact]
        public void ParameterEmitter_SetConstant_Convertible()
        {
            // double → int 自动转换
            var cls = _mod.DefineType($"PE_CV_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p = me.DefineParameter(typeof(int), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "x");
            p.SetConstant(42.0);
            me.Append(p);
            var t = cls.CreateType();
            Assert.Equal(42, t.GetMethod("Run").Invoke(null, new object[] { 42 }));
        }

        [Fact]
        public void ParameterEmitter_SetConstant_Missing()
        {
            // Missing 应该被忽略，不会设置 _hasDefaultValue
            var cls = _mod.DefineType($"PE_MS_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p = me.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            p.SetConstant(Missing.Value);
            me.Append(p);
            var t = cls.CreateType();
            Assert.Equal(99, t.GetMethod("Run").Invoke(null, new object[] { 99 }));
        }

        [Fact]
        public void ParameterEmitter_SetConstant_NullableEnumDefault()
        {
            // 测试 Nullable<Enum> 默认值。enum 类型必须先 Build。
            var cls = _mod.DefineType($"PE_NE_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(DayOfWeek?));
            var p = me.DefineParameter(typeof(DayOfWeek?), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "x");
            p.SetConstant(DayOfWeek.Monday);
            me.Append(p);
            var t = cls.CreateType();
            Assert.Equal(DayOfWeek.Friday, t.GetMethod("Run").Invoke(null, new object[] { DayOfWeek.Friday }));
        }

        // ===== Custom attributes =====

        [Fact]
        public void ParameterEmitter_SetCustomAttribute_NullData_Throws()
        {
            var cls = _mod.DefineType($"PE_NCA_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p = me.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            Assert.Throws<ArgumentNullException>(() => p.SetCustomAttribute((CustomAttributeData)null));
        }

        [Fact]
        public void ParameterEmitter_SetCustomAttribute_NullBuilder_Throws()
        {
            var cls = _mod.DefineType($"PE_NCB_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p = me.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            Assert.Throws<ArgumentNullException>(() => p.SetCustomAttribute((System.Reflection.Emit.CustomAttributeBuilder)null));
        }

        [Fact]
        public void ParameterEmitter_SetCustomAttribute_Builder_NoThrow()
        {
            // 验证调用不会抛异常即可（不验证反射读取，因为 ParameterBuilder 行为依赖运行时）
            var cls = _mod.DefineType($"PE_CB_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p = me.DefineParameter(typeof(int), ParameterAttributes.None, "x");
            p.SetCustomAttribute<MyParamAttribute>();
            me.Append(p);
            var t = cls.CreateType();
            Assert.Equal(7, t.GetMethod("Run").Invoke(null, new object[] { 7 }));
        }

        [Fact]
        public void ParameterEmitter_SetCustomAttribute_FromData_NoThrow()
        {
            var cls = _mod.DefineType($"PE_AD_{Guid.NewGuid():N}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var p = me.DefineParameter(typeof(int), ParameterAttributes.None, "x");

            var sampleMethod = typeof(SampleClass).GetMethod(nameof(SampleClass.HasParamAttr));
            var data = sampleMethod.GetParameters()[0].CustomAttributes;
            foreach (var d in data)
            {
                p.SetCustomAttribute(d);
            }

            me.Append(p);
            var t = cls.CreateType();
            Assert.Equal(13, t.GetMethod("Run").Invoke(null, new object[] { 13 }));
        }

        [AttributeUsage(AttributeTargets.Parameter)]
        public class MyParamAttribute : Attribute
        {
            public MyParamAttribute() { }
        }

        public class SampleClass
        {
            public static int HasParamAttr([MyParam] int x) => x;
        }

        // ===== EmitUtils.SetConstantOfType =====

        [Fact]
        public void EmitUtils_SetConstantOfType_Null_RefType()
        {
            Assert.Null(EmitUtils.SetConstantOfType(null, typeof(string)));
        }

        [Fact]
        public void EmitUtils_SetConstantOfType_Null_Nullable()
        {
            Assert.Null(EmitUtils.SetConstantOfType(null, typeof(int?)));
        }

        [Fact]
        public void EmitUtils_SetConstantOfType_Null_ValueType_Throws()
        {
            Assert.Throws<NotSupportedException>(() => EmitUtils.SetConstantOfType(null, typeof(int)));
        }

        [Fact]
        public void EmitUtils_SetConstantOfType_SameType()
        {
            Assert.Equal(42, EmitUtils.SetConstantOfType(42, typeof(int)));
        }

        [Fact]
        public void EmitUtils_SetConstantOfType_NullableSameUnderlying()
        {
            Assert.Equal(42, EmitUtils.SetConstantOfType(42, typeof(int?)));
        }

        [Fact]
        public void EmitUtils_SetConstantOfType_Convertible()
        {
            // 自动 Convert.ChangeType
            Assert.Equal(42, EmitUtils.SetConstantOfType(42.0, typeof(int)));
        }

        // ===== EmitUtils.CreateCustomAttribute =====

        [Fact]
        public void EmitUtils_CreateCustomAttribute_Generic()
        {
            var b = EmitUtils.CreateCustomAttribute<MyParamAttribute>();
            Assert.NotNull(b);
        }

        [Fact]
        public void EmitUtils_CreateCustomAttribute_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => EmitUtils.CreateCustomAttribute(null));
        }

        [Fact]
        public void EmitUtils_CreateCustomAttribute_FromData()
        {
            var sampleMethod = typeof(SampleClass).GetMethod(nameof(SampleClass.HasParamAttr));
            var data = sampleMethod.GetParameters()[0].CustomAttributes.GetEnumerator();
            data.MoveNext();
            var b = EmitUtils.CreateCustomAttribute(data.Current);
            Assert.NotNull(b);
        }

        // ===== EmitUtils.IsAssignableFromSignatureTypes / EqualSignatureTypes 边界 =====

        [Fact]
        public void EmitUtils_EqualSignatureTypes_GenericParameter_DiffPosition()
        {
            // 创建两个不同位置的泛型参数
            var t = typeof(MyGen<,>);
            var args = t.GetGenericArguments();
            Assert.False(EmitUtils.EqualSignatureTypes(args[0], args[1]));
        }

        [Fact]
        public void EmitUtils_EqualSignatureTypes_SameGenericParameter()
        {
            var t = typeof(MyGen<,>);
            var args = t.GetGenericArguments();
            Assert.True(EmitUtils.EqualSignatureTypes(args[0], args[0]));
        }

        [Fact]
        public void EmitUtils_EqualSignatureTypes_GenericVsNonGeneric()
        {
            var t = typeof(MyGen<,>);
            var args = t.GetGenericArguments();
            // GenericParameter 与非 GenericParameter
            Assert.False(EmitUtils.EqualSignatureTypes(args[0], typeof(int)));
        }

        [Fact]
        public void EmitUtils_EqualSignatureTypes_DifferentGenericTypeDef()
        {
            // List<int> 与 IList<int> — 不同 GenericTypeDefinition
            Assert.False(EmitUtils.EqualSignatureTypes(typeof(System.Collections.Generic.List<int>), typeof(System.Collections.Generic.IList<int>)));
        }

        [Fact]
        public void EmitUtils_IsAssignableFromSignatureTypes_BaseType()
        {
            // IList<int> 可以从 List<int> 赋值
            Assert.True(EmitUtils.IsAssignableFromSignatureTypes(typeof(System.Collections.Generic.IList<int>), typeof(System.Collections.Generic.List<int>)));
        }

        public class MyGen<T1, T2>
        {
        }
    }
}
