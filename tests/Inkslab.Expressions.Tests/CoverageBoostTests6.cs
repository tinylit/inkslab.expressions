using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Ë¶ÜÁõñÁéáÊèêÂçáÊµãËØ?Part 6 ‚Ä?Á≤æÂáÜË°•Áº∫ phase 2„Ä?
    /// </summary>
    public class CoverageBoostTests6
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB6_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, string name, params object[] args) => t.GetMethod(name).Invoke(null, args);

        #region ArrayIndexExpression ‚Ä?more type code paths + dynamic index assign

        [Fact]
        public void ArrayIndex_ReadSByte()
        {
            var t = BuildStatic("AISB", typeof(sbyte), m =>
            {
                var arr = m.DefineParameter(typeof(sbyte[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal((sbyte)-5, Invoke(t, "AISB", (object)new sbyte[] { -5 }));
        }

        [Fact]
        public void ArrayIndex_ReadChar()
        {
            var t = BuildStatic("AICH", typeof(char), m =>
            {
                var arr = m.DefineParameter(typeof(char[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal('A', Invoke(t, "AICH", (object)new char[] { 'A' }));
        }

        [Fact]
        public void ArrayIndex_ReadULong()
        {
            var t = BuildStatic("AIUL", typeof(ulong), m =>
            {
                var arr = m.DefineParameter(typeof(ulong[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal(42UL, Invoke(t, "AIUL", (object)new ulong[] { 42UL }));
        }



        [Fact]
        public void ArrayIndex_WriteByte()
        {
            var t = BuildStatic("AIWBY", typeof(void), m =>
            {
                var arr = m.DefineParameter(typeof(byte[]), "arr");
                m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant((byte)42)));
            });
            var a = new byte[] { 0 };
            Invoke(t, "AIWBY", (object)a);
            Assert.Equal((byte)42, a[0]);
        }

        [Fact]
        public void ArrayIndex_ReadEnum()
        {
            var t = BuildStatic("AIEN", typeof(DayOfWeek), m =>
            {
                var arr = m.DefineParameter(typeof(DayOfWeek[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal(DayOfWeek.Friday, Invoke(t, "AIEN", (object)new DayOfWeek[] { DayOfWeek.Friday }));
        }

        #endregion

        #region ReturnExpression ‚Ä?void return in block / return in block with value

        [Fact]
        public void Return_ValueInBlock()
        {
            var t = BuildStatic("RVB", typeof(int), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.IfThen(flag, Expression.Return(Expression.Constant(42))));
                m.Append(Expression.Constant(0));
            });
            Assert.Equal(42, Invoke(t, "RVB", true));
            Assert.Equal(0, Invoke(t, "RVB", false));
        }

        [Fact]
        public void Return_VoidInBlock()
        {
            var t = BuildStatic("RVIB", typeof(void), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.IfThen(flag, Expression.Return()));
            });
            Invoke(t, "RVIB", true);
            Invoke(t, "RVIB", false);
        }

        #endregion

        #region NestedClassEmitter ‚Ä?UncompiledType + 4-param

        [Fact]
        public void NestedClassEmitter_TwoParam()
        {
            var te = _mod.DefineType($"N2P_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            var ne = te.DefineNestedType("Inner", TypeAttributes.NestedPublic);
            var nct = ne.DefineConstructor(MethodAttributes.Public);
            nct.Append(Expression.Default(typeof(void)));
            Assert.NotNull(ne.UncompiledType);
            te.CreateType();
        }

        #endregion

        #region TypeAsExpression ‚Ä?void body throws / same type

        [Fact]
        public void TypeAs_VoidBody_Throws()
        {
            Assert.Throws<AstException>(() => Expression.TypeAs(Expression.Default(typeof(void)), typeof(string)));
        }

        [Fact]
        public void TypeAs_SameType()
        {
            var t = BuildStatic("TAST", typeof(string), m =>
            {
                var s = m.DefineParameter(typeof(string), "s");
                m.Append(Expression.TypeAs(s, typeof(string)));
            });
            Assert.Equal("hello", Invoke(t, "TAST", "hello"));
        }

        #endregion

        #region MemberInitExpression ‚Ä?ÂÄºÁ±ªÂû?+ Â≠óÊÆµÁªëÂÆö

        [Fact]
        public void MemberInit_FieldBindingOnClass()
        {
            var fi = typeof(FieldTarget5).GetField("Value");
            var t = BuildStatic("MIFC", typeof(FieldTarget5), m =>
            {
                m.Append(Expression.MemberInit(
                    Expression.New(typeof(FieldTarget5)),
                    Expression.Bind(fi, Expression.Constant(99))));
            });
            var result = (FieldTarget5)Invoke(t, "MIFC");
            Assert.Equal(99, result.Value);
        }

        #endregion

        #region EnumEmitter ‚Ä?more paths

        [Fact]
        public void EnumEmitter_ByteUnderlying()
        {
            var mod = new ModuleEmitter($"EEB_{Guid.NewGuid():N}");
            var ee = mod.DefineEnum($"BE_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(byte));
            ee.DefineLiteral("A", (byte)1);
            ee.DefineLiteral("B", (byte)2);
            var type = ee.CreateType();
            Assert.True(type.IsEnum);
            Assert.Equal(typeof(byte), Enum.GetUnderlyingType(type));
        }

        [Fact]
        public void EnumEmitter_ShortUnderlying()
        {
            var mod = new ModuleEmitter($"EES_{Guid.NewGuid():N}");
            var ee = mod.DefineEnum($"SE_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(short));
            ee.DefineLiteral("A", (short)1);
            ee.DefineLiteral("B", (short)2);
            var type = ee.CreateType();
            Assert.True(type.IsEnum);
        }

        #endregion

        #region TypeIsExpression ‚Ä?value type boxed check / interface

        [Fact]
        public void TypeIs_ValueTypeBoxed()
        {
            // object param, check for value type ‚Ä?Unknown path
            var t = BuildStatic("TIVB", typeof(bool), m =>
            {
                var o = m.DefineParameter(typeof(object), "o");
                m.Append(Expression.TypeIs(o, typeof(DateTime)));
            });
            Assert.True((bool)Invoke(t, "TIVB", (object)DateTime.Now));
            Assert.False((bool)Invoke(t, "TIVB", (object)"hello"));
        }

        [Fact]
        public void TypeIs_Interface()
        {
            var t = BuildStatic("TII", typeof(bool), m =>
            {
                var o = m.DefineParameter(typeof(object), "o");
                m.Append(Expression.TypeIs(o, typeof(IComparable)));
            });
            Assert.True((bool)Invoke(t, "TII", (object)42));
            Assert.True((bool)Invoke(t, "TII", (object)"hello"));
        }

        #endregion

        #region BlockExpression ‚Ä?readonly / append null

        [Fact]
        public void Block_AppendNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var te = _mod.DefineType($"BAN_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
                var me = te.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
                me.Append(null);
            });
        }

        #endregion

        #region PropertyExpression ‚Ä?static property / write-only detection



        #endregion

        #region ModuleEmitter ‚Ä?more overloads

        [Fact]
        public void ModuleEmitter_DefineType_NameOnly()
        {
            var mod = new ModuleEmitter($"MDNO_{Guid.NewGuid():N}");
            var te = mod.DefineType($"T_{Guid.NewGuid():N}");
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            Assert.NotNull(te.CreateType());
        }

        [Fact]
        public void ModuleEmitter_DefineType_WithBaseAndInterfaces()
        {
            var mod = new ModuleEmitter($"MDBI_{Guid.NewGuid():N}");
            var te = mod.DefineType($"T_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(object), new[] { typeof(ICloneable) });
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            var clone = te.DefineMethod("Clone", MethodAttributes.Public | MethodAttributes.Virtual, typeof(object));
            clone.Append(Expression.Constant(null, typeof(object)));
            Assert.NotNull(te.CreateType());
        }

        #endregion

        #region ConvertExpression ‚Ä?additional paths

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
        public void Convert_DoubleToFloat()
        {
            var t = BuildStatic("D2F", typeof(float), m =>
            {
                var d = m.DefineParameter(typeof(double), "d");
                m.Append(Expression.Convert(d, typeof(float)));
            });
            Assert.Equal(3.14f, Invoke(t, "D2F", 3.14));
        }

        [Fact]
        public void Convert_FloatToDouble()
        {
            var t = BuildStatic("F2D", typeof(double), m =>
            {
                var f = m.DefineParameter(typeof(float), "f");
                m.Append(Expression.Convert(f, typeof(double)));
            });
            Assert.Equal(3.0, (double)Invoke(t, "F2D", 3.0f), 1);
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
        public void Convert_ShortToLong()
        {
            var t = BuildStatic("S2L", typeof(long), m =>
            {
                var s = m.DefineParameter(typeof(short), "s");
                m.Append(Expression.Convert(s, typeof(long)));
            });
            Assert.Equal(42L, Invoke(t, "S2L", (short)42));
        }

        #endregion

        #region ParameterExpression ‚Ä?Starg_S (non-ByRef assign)

        [Fact]
        public void Param_Assign()
        {
            var t = BuildStatic("PA", typeof(int), m =>
            {
                var p = m.DefineParameter(typeof(int), "p");
                m.Append(Expression.Assign(p, Expression.Constant(99)));
                m.Append(p);
            });
            Assert.Equal(99, Invoke(t, "PA", 42));
        }

        #endregion
    }

    /// <summary>
    /// ËæÖÂä©Á±ª„Ä?
    /// </summary>
    public class WriteOnlyTarget
    {
        /// <summary>
        /// Â™ÂÂ±Êß„?
        /// </summary>
        public int Val { set { _ = value; } }
    }
}
