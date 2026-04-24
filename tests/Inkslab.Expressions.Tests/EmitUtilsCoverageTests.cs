using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// EmitUtils 覆盖率测试。
    /// </summary>
    public class EmitUtilsCoverageTests
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"EU_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public);
            te.DefineDefaultConstructor();
            var me = te.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, string name = "Run", params object[] args)
            => t.GetMethod(name).Invoke(null, args);

        #region EmitUtils.AreEquivalent / EqualSignatureTypes / IsAssignableFromSignatureTypes

        [Fact]
        public void AreEquivalent_SameType_True()
        {
            Assert.True(EmitUtils.AreEquivalent(typeof(int), typeof(int)));
        }

        [Fact]
        public void AreEquivalent_DifferentType_False()
        {
            Assert.False(EmitUtils.AreEquivalent(typeof(int), typeof(string)));
        }

        [Fact]
        public void EqualSignatureTypes_Same_True()
        {
            Assert.True(EmitUtils.EqualSignatureTypes(typeof(int), typeof(int)));
        }

        [Fact]
        public void EqualSignatureTypes_Diff_False()
        {
            Assert.False(EmitUtils.EqualSignatureTypes(typeof(int), typeof(long)));
        }

        [Fact]
        public void IsAssignableFromSignatureTypes_Same_True()
        {
            Assert.True(EmitUtils.IsAssignableFromSignatureTypes(typeof(object), typeof(string)));
        }

        [Fact]
        public void IsAssignableFromSignatureTypes_Incompatible_False()
        {
            Assert.False(EmitUtils.IsAssignableFromSignatureTypes(typeof(int), typeof(string)));
        }

        #endregion

        #region EmitUtils.EmitString

        [Fact]
        public void EmitString_Loads()
        {
            var t = BuildStatic("ES", typeof(string), me =>
            {
                me.Append(Expression.Constant("hello"));
            });
            Assert.Equal("hello", Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitBoolean

        [Fact]
        public void EmitBoolean_True()
        {
            var t = BuildStatic("EBT", typeof(bool), me =>
            {
                me.Append(Expression.Constant(true));
            });
            Assert.Equal(true, Invoke(t));
        }

        [Fact]
        public void EmitBoolean_False()
        {
            var t = BuildStatic("EBF", typeof(bool), me =>
            {
                me.Append(Expression.Constant(false));
            });
            Assert.Equal(false, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitChar

        [Fact]
        public void EmitChar()
        {
            var t = BuildStatic("EC", typeof(char), me =>
            {
                me.Append(Expression.Constant('Z'));
            });
            Assert.Equal('Z', Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitByte

        [Fact]
        public void EmitByte()
        {
            var t = BuildStatic("EBy", typeof(byte), me =>
            {
                me.Append(Expression.Constant((byte)42));
            });
            Assert.Equal((byte)42, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitSByte

        [Fact]
        public void EmitSByte()
        {
            var t = BuildStatic("ESB", typeof(sbyte), me =>
            {
                me.Append(Expression.Constant((sbyte)-5));
            });
            Assert.Equal((sbyte)-5, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitInt16

        [Fact]
        public void EmitInt16()
        {
            var t = BuildStatic("EI16", typeof(short), me =>
            {
                me.Append(Expression.Constant((short)1000));
            });
            Assert.Equal((short)1000, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitUInt16

        [Fact]
        public void EmitUInt16()
        {
            var t = BuildStatic("EU16", typeof(ushort), me =>
            {
                me.Append(Expression.Constant((ushort)65000));
            });
            Assert.Equal((ushort)65000, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitInt

        [Fact]
        public void EmitInt()
        {
            var t = BuildStatic("EI", typeof(int), me =>
            {
                me.Append(Expression.Constant(123456));
            });
            Assert.Equal(123456, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitUInt

        [Fact]
        public void EmitUInt()
        {
            var t = BuildStatic("EUI", typeof(uint), me =>
            {
                me.Append(Expression.Constant((uint)4000000000));
            });
            Assert.Equal((uint)4000000000, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitLong

        [Fact]
        public void EmitLong()
        {
            var t = BuildStatic("EL", typeof(long), me =>
            {
                me.Append(Expression.Constant(999999999999L));
            });
            Assert.Equal(999999999999L, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitULong

        [Fact]
        public void EmitULong()
        {
            var t = BuildStatic("EUL", typeof(ulong), me =>
            {
                me.Append(Expression.Constant((ulong)18000000000000000000));
            });
            Assert.Equal((ulong)18000000000000000000, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitDouble

        [Fact]
        public void EmitDouble()
        {
            var t = BuildStatic("ED", typeof(double), me =>
            {
                me.Append(Expression.Constant(3.14159));
            });
            Assert.Equal(3.14159, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitSingle

        [Fact]
        public void EmitSingle()
        {
            var t = BuildStatic("ESi", typeof(float), me =>
            {
                me.Append(Expression.Constant(2.71828f));
            });
            Assert.Equal(2.71828f, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitDecimal

        [Fact]
        public void EmitDecimal()
        {
            var t = BuildStatic("EDec", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(123.456m));
            });
            Assert.Equal(123.456m, Invoke(t));
        }

        [Fact]
        public void EmitDecimal_Zero()
        {
            var t = BuildStatic("EDecZ", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(0m));
            });
            Assert.Equal(0m, Invoke(t));
        }

        [Fact]
        public void EmitDecimal_One()
        {
            var t = BuildStatic("EDecO", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(1m));
            });
            Assert.Equal(1m, Invoke(t));
        }

        [Fact]
        public void EmitDecimal_MinusOne()
        {
            var t = BuildStatic("EDecMO", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(-1m));
            });
            Assert.Equal(-1m, Invoke(t));
        }

        [Fact]
        public void EmitDecimal_Large()
        {
            var t = BuildStatic("EDecL", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(79228162514264337593543950335m));
            });
            Assert.Equal(79228162514264337593543950335m, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitConvertToType - numeric conversions

        [Fact]
        public void Convert_IntToLong()
        {
            var t = BuildStatic("CIL", typeof(long), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(42), typeof(long)));
            });
            Assert.Equal(42L, Invoke(t));
        }

        [Fact]
        public void Convert_LongToInt()
        {
            var t = BuildStatic("CLI", typeof(int), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(42L), typeof(int)));
            });
            Assert.Equal(42, Invoke(t));
        }

        [Fact]
        public void Convert_IntToFloat()
        {
            var t = BuildStatic("CIF", typeof(float), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(42), typeof(float)));
            });
            Assert.Equal(42.0f, Invoke(t));
        }

        [Fact]
        public void Convert_FloatToDouble()
        {
            var t = BuildStatic("CFD", typeof(double), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(1.5f), typeof(double)));
            });
            Assert.Equal((double)1.5f, Invoke(t));
        }

        [Fact]
        public void Convert_IntToDouble()
        {
            var t = BuildStatic("CID", typeof(double), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(7), typeof(double)));
            });
            Assert.Equal(7.0, Invoke(t));
        }

        [Fact]
        public void Convert_DoubleToInt()
        {
            var t = BuildStatic("CDI", typeof(int), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(7.9), typeof(int)));
            });
            Assert.Equal(7, Invoke(t));
        }

        [Fact]
        public void Convert_ByteToInt()
        {
            var t = BuildStatic("CBI", typeof(int), me =>
            {
                me.Append(Expression.Convert(Expression.Constant((byte)200), typeof(int)));
            });
            Assert.Equal(200, Invoke(t));
        }

        [Fact]
        public void Convert_IntToByte()
        {
            var t = BuildStatic("CIB", typeof(byte), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(200), typeof(byte)));
            });
            Assert.Equal((byte)200, Invoke(t));
        }

        [Fact]
        public void Convert_IntToShort()
        {
            var t = BuildStatic("CIS", typeof(short), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(100), typeof(short)));
            });
            Assert.Equal((short)100, Invoke(t));
        }

        [Fact]
        public void Convert_IntToUInt()
        {
            var t = BuildStatic("CIU", typeof(uint), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(100), typeof(uint)));
            });
            Assert.Equal((uint)100, Invoke(t));
        }

        [Fact]
        public void Convert_IntToULong()
        {
            var t = BuildStatic("CIUL", typeof(ulong), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(100), typeof(ulong)));
            });
            Assert.Equal((ulong)100, Invoke(t));
        }

        [Fact]
        public void Convert_IntToSByte()
        {
            var t = BuildStatic("CISB", typeof(sbyte), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(42), typeof(sbyte)));
            });
            Assert.Equal((sbyte)42, Invoke(t));
        }

        [Fact]
        public void Convert_IntToUShort()
        {
            var t = BuildStatic("CIUS", typeof(ushort), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(42), typeof(ushort)));
            });
            Assert.Equal((ushort)42, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitConvertToType - nullable conversions

        [Fact]
        public void Convert_IntToNullableInt()
        {
            var t = BuildStatic("CINI", typeof(int?), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(42), typeof(int?)));
            });
            Assert.Equal((int?)42, Invoke(t));
        }

        [Fact]
        public void Convert_NullableIntToInt()
        {
            var t = BuildStatic("CNII", typeof(int), me =>
            {
                var v = Expression.Variable(typeof(int?));
                me.Append(Expression.Assign(v, Expression.Constant(42, typeof(int?))));
                me.Append(Expression.Convert(v, typeof(int)));
            });
            Assert.Equal(42, Invoke(t));
        }

        [Fact]
        public void Convert_NullableIntToNullableLong()
        {
            var t = BuildStatic("CNINL", typeof(long?), me =>
            {
                var v = Expression.Variable(typeof(int?));
                me.Append(Expression.Assign(v, Expression.Constant(42, typeof(int?))));
                me.Append(Expression.Convert(v, typeof(long?)));
            });
            Assert.Equal((long?)42, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitConvertToType - reference / boxing

        [Fact]
        public void Convert_IntToObject_Box()
        {
            var t = BuildStatic("CIOB", typeof(object), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(42), typeof(object)));
            });
            Assert.Equal(42, Invoke(t));
        }

        [Fact]
        public void Convert_ObjectToInt_Unbox()
        {
            var t = BuildStatic("COIU", typeof(int), me =>
            {
                me.Append(Expression.Convert(Expression.Constant(42, typeof(object)), typeof(int)));
            });
            Assert.Equal(42, Invoke(t));
        }

        [Fact]
        public void Convert_StringToObject()
        {
            var t = BuildStatic("CSO", typeof(object), me =>
            {
                me.Append(Expression.Convert(Expression.Constant("hi"), typeof(object)));
            });
            Assert.Equal("hi", Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitDefaultOfType

        [Fact]
        public void Default_Int()
        {
            var t = BuildStatic("DI", typeof(int), me =>
            {
                me.Append(Expression.Default(typeof(int)));
            });
            Assert.Equal(0, Invoke(t));
        }

        [Fact]
        public void Default_String()
        {
            var t = BuildStatic("DS", typeof(string), me =>
            {
                me.Append(Expression.Default(typeof(string)));
            });
            Assert.Null(Invoke(t));
        }

        [Fact]
        public void Default_Bool()
        {
            var t = BuildStatic("DB", typeof(bool), me =>
            {
                me.Append(Expression.Default(typeof(bool)));
            });
            Assert.Equal(false, Invoke(t));
        }

        [Fact]
        public void Default_Double()
        {
            var t = BuildStatic("DD", typeof(double), me =>
            {
                me.Append(Expression.Default(typeof(double)));
            });
            Assert.Equal(0.0, Invoke(t));
        }

        [Fact]
        public void Default_Long()
        {
            var t = BuildStatic("DLo", typeof(long), me =>
            {
                me.Append(Expression.Default(typeof(long)));
            });
            Assert.Equal(0L, Invoke(t));
        }

        #endregion

        #region EmitUtils.EmitConstantOfType — non-primitive

        [Fact]
        public void Constant_Null()
        {
            var t = BuildStatic("CN", typeof(string), me =>
            {
                me.Append(Expression.Constant(null, typeof(string)));
            });
            Assert.Null(Invoke(t));
        }

        [Fact]
        public void Constant_Enum()
        {
            var t = BuildStatic("CE", typeof(DayOfWeek), me =>
            {
                me.Append(Expression.Constant(DayOfWeek.Friday));
            });
            Assert.Equal(DayOfWeek.Friday, Invoke(t));
        }

        [Fact]
        public void Constant_Type()
        {
            var t = BuildStatic("CT", typeof(Type), me =>
            {
                me.Append(Expression.Constant(typeof(int), typeof(Type)));
            });
            Assert.Equal(typeof(int), Invoke(t));
        }

        #endregion

        #region EmitUtils.SetConstantOfType

        [Fact]
        public void SetConstantOfType_IntToLong()
        {
            var result = EmitUtils.SetConstantOfType(42, typeof(long));
            Assert.IsType<long>(result);
            Assert.Equal(42L, result);
        }

        [Fact]
        public void SetConstantOfType_StringToString()
        {
            var result = EmitUtils.SetConstantOfType("hello", typeof(string));
            Assert.Equal("hello", result);
        }

        [Fact]
        public void SetConstantOfType_IntToDouble()
        {
            var result = EmitUtils.SetConstantOfType(42, typeof(double));
            Assert.IsType<double>(result);
        }

        [Fact]
        public void SetConstantOfType_Null()
        {
            var result = EmitUtils.SetConstantOfType(null, typeof(string));
            Assert.Null(result);
        }

        #endregion

        #region EmitUtils.CreateCustomAttribute

        [Fact]
        public void CreateCustomAttribute_Generic()
        {
            var builder = EmitUtils.CreateCustomAttribute<ObsoleteAttribute>();
            Assert.NotNull(builder);
        }

        [Fact]
        public void CreateCustomAttribute_FromData()
        {
            // Get an attribute from a known type
            var attrs = typeof(FlagsTestEnum).GetCustomAttributesData();
            foreach (var attr in attrs)
            {
                if (attr.AttributeType == typeof(FlagsAttribute))
                {
                    var builder = EmitUtils.CreateCustomAttribute(attr);
                    Assert.NotNull(builder);
                }
            }
        }

        [Flags]
        private enum FlagsTestEnum { A = 1, B = 2 }

        #endregion

        #region EmitUtils.EmitNew

        [Fact]
        public void EmitNew_ObjectConstructor()
        {
            var t = BuildStatic("EN", typeof(object), me =>
            {
                me.Append(Expression.New(typeof(object)));
            });
            Assert.NotNull(Invoke(t));
        }

        #endregion

        #region Edge cases — EmitInt with different sizes

        [Fact]
        public void EmitInt_MinusOne()
        {
            var t = BuildStatic("EIM1", typeof(int), me =>
            {
                me.Append(Expression.Constant(-1));
            });
            Assert.Equal(-1, Invoke(t));
        }

        [Fact]
        public void EmitInt_Zero()
        {
            var t = BuildStatic("EI0", typeof(int), me =>
            {
                me.Append(Expression.Constant(0));
            });
            Assert.Equal(0, Invoke(t));
        }

        [Fact]
        public void EmitInt_One()
        {
            var t = BuildStatic("EI1", typeof(int), me =>
            {
                me.Append(Expression.Constant(1));
            });
            Assert.Equal(1, Invoke(t));
        }

        [Fact]
        public void EmitInt_Two()
        {
            var t = BuildStatic("EI2", typeof(int), me =>
            {
                me.Append(Expression.Constant(2));
            });
            Assert.Equal(2, Invoke(t));
        }

        [Fact]
        public void EmitInt_Three()
        {
            var t = BuildStatic("EI3", typeof(int), me =>
            {
                me.Append(Expression.Constant(3));
            });
            Assert.Equal(3, Invoke(t));
        }

        [Fact]
        public void EmitInt_Four()
        {
            var t = BuildStatic("EI4", typeof(int), me =>
            {
                me.Append(Expression.Constant(4));
            });
            Assert.Equal(4, Invoke(t));
        }

        [Fact]
        public void EmitInt_Five()
        {
            var t = BuildStatic("EI5", typeof(int), me =>
            {
                me.Append(Expression.Constant(5));
            });
            Assert.Equal(5, Invoke(t));
        }

        [Fact]
        public void EmitInt_Six()
        {
            var t = BuildStatic("EI6", typeof(int), me =>
            {
                me.Append(Expression.Constant(6));
            });
            Assert.Equal(6, Invoke(t));
        }

        [Fact]
        public void EmitInt_Seven()
        {
            var t = BuildStatic("EI7", typeof(int), me =>
            {
                me.Append(Expression.Constant(7));
            });
            Assert.Equal(7, Invoke(t));
        }

        [Fact]
        public void EmitInt_Eight()
        {
            var t = BuildStatic("EI8", typeof(int), me =>
            {
                me.Append(Expression.Constant(8));
            });
            Assert.Equal(8, Invoke(t));
        }

        [Fact]
        public void EmitInt_SByte_Range()
        {
            var t = BuildStatic("EISBR", typeof(int), me =>
            {
                me.Append(Expression.Constant(100));
            });
            Assert.Equal(100, Invoke(t));
        }

        [Fact]
        public void EmitInt_Large()
        {
            var t = BuildStatic("EILa", typeof(int), me =>
            {
                me.Append(Expression.Constant(1000000));
            });
            Assert.Equal(1000000, Invoke(t));
        }

        #endregion
    }
}
