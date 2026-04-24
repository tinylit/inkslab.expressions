using Xunit;
using System;
using System.Reflection;
using Inkslab.Emitters;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// 覆盖率提升测试 Part 5 — 精准补缺。
    /// </summary>
    public class CoverageBoostTests5
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB5_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var te = _mod.DefineType($"{name}_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var me = te.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return te.CreateType();
        }

        private object Invoke(Type t, string name, params object[] args) => t.GetMethod(name).Invoke(null, args);

        #region NewArrayExpression — 无类型参数构造

        [Fact]
        public void NewArray_DefaultObjectType()
        {
            // Expression.NewArray(int) creates object[]
            var t = BuildStatic("NAD", typeof(object[]), m => m.Append(Expression.NewArray(3)));
            var arr = (object[])Invoke(t, "NAD");
            Assert.Equal(3, arr.Length);
        }

        #endregion

        #region ContinueExpression — 异常路径

        [Fact]
        public void Continue_OutsideLoop_Throws()
        {
            Assert.Throws<AstException>(() =>
            {
                BuildStatic("COL", typeof(void), m => m.Append(Expression.Continue()));
            });
        }

        #endregion

        #region FieldExpression — 异常路径

        [Fact]
        public void Field_NonStaticWithoutInstance_Throws()
        {
            var fi = typeof(CoverageTarget).GetField("_tag", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi == null)
            {
                // Field not accessible, use a public instance field
                fi = typeof(FieldTarget5).GetField("Value");
            }
            Assert.Throws<InvalidOperationException>(() => Expression.Field(fi));
        }

        [Fact]
        public void Field_StaticWithInstance_Throws()
        {
            var fi = typeof(string).GetField(nameof(string.Empty));
            Assert.Throws<InvalidOperationException>(() => Expression.Field(Expression.Constant("x"), fi));
        }

        #endregion

        #region ReturnExpression — void return / body.IsVoid throws

        [Fact]
        public void Return_WithVoidBody_Throws()
        {
            Assert.Throws<AstException>(() => Expression.Return(Expression.Default(typeof(void))));
        }

        #endregion

        #region TypeAsExpression — 值类型 as nullable / 值类型 as 引用类型

        [Fact]
        public void TypeAs_IntBoxedToNullableInt()
        {
            var t = BuildStatic("TANI", typeof(int?), m =>
            {
                var o = m.DefineParameter(typeof(object), "o");
                m.Append(Expression.TypeAs(o, typeof(int?)));
            });
            Assert.Equal((int?)42, Invoke(t, "TANI", (object)42));
            Assert.Null(Invoke(t, "TANI", (object)"hello"));
        }

        [Fact]
        public void TypeAs_ValueTypeToObject()
        {
            var t = BuildStatic("TAVT", typeof(IComparable), m =>
            {
                var v = m.DefineParameter(typeof(int), "v");
                m.Append(Expression.TypeAs(Expression.Convert(v, typeof(object)), typeof(IComparable)));
            });
            Assert.NotNull(Invoke(t, "TAVT", 42));
        }

        #endregion

        #region MemberInitExpression — 字段绑定

        [Fact]
        public void MemberInit_FieldBinding()
        {
            var fieldInfo = typeof(FieldTarget5).GetField("Value");
            var t = BuildStatic("MIF", typeof(FieldTarget5), m =>
            {
                m.Append(Expression.MemberInit(
                    Expression.New(typeof(FieldTarget5)),
                    Expression.Bind(fieldInfo, Expression.Constant(42))));
            });
            var result = (FieldTarget5)Invoke(t, "MIF");
            Assert.Equal(42, result.Value);
        }

        #endregion

        #region ConstantExpression — ToString / null ToString

        [Fact]
        public void Constant_NullToString()
        {
            var c = Expression.Constant(null, typeof(string));
            Assert.Equal("null", c.ToString());
        }

        [Fact]
        public void Constant_NonNullToString()
        {
            var c = Expression.Constant(42);
            Assert.Equal("42", c.ToString());
        }

        #endregion

        #region ArrayIndexExpression — 多类型读/写

        [Fact]
        public void ArrayIndex_ReadByte()
        {
            var t = BuildStatic("AIB", typeof(byte), m =>
            {
                var arr = m.DefineParameter(typeof(byte[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal((byte)42, Invoke(t, "AIB", new byte[] { 42, 1 }));
        }

        [Fact]
        public void ArrayIndex_ReadShort()
        {
            var t = BuildStatic("AIS", typeof(short), m =>
            {
                var arr = m.DefineParameter(typeof(short[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal((short)42, Invoke(t, "AIS", new short[] { 42 }));
        }

        [Fact]
        public void ArrayIndex_ReadUShort()
        {
            var t = BuildStatic("AIUS", typeof(ushort), m =>
            {
                var arr = m.DefineParameter(typeof(ushort[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal((ushort)42, Invoke(t, "AIUS", new ushort[] { 42 }));
        }

        [Fact]
        public void ArrayIndex_ReadUInt()
        {
            var t = BuildStatic("AIUI", typeof(uint), m =>
            {
                var arr = m.DefineParameter(typeof(uint[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal(42u, Invoke(t, "AIUI", new uint[] { 42 }));
        }

        [Fact]
        public void ArrayIndex_ReadLong()
        {
            var t = BuildStatic("AIL", typeof(long), m =>
            {
                var arr = m.DefineParameter(typeof(long[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal(42L, Invoke(t, "AIL", new long[] { 42L }));
        }

        [Fact]
        public void ArrayIndex_ReadFloat()
        {
            var t = BuildStatic("AIF", typeof(float), m =>
            {
                var arr = m.DefineParameter(typeof(float[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal(42.0f, Invoke(t, "AIF", new float[] { 42.0f }));
        }

        [Fact]
        public void ArrayIndex_ReadDouble()
        {
            var t = BuildStatic("AID", typeof(double), m =>
            {
                var arr = m.DefineParameter(typeof(double[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal(42.0, Invoke(t, "AID", new double[] { 42.0 }));
        }

        [Fact]
        public void ArrayIndex_ReadBool()
        {
            var t = BuildStatic("AIBL", typeof(bool), m =>
            {
                var arr = m.DefineParameter(typeof(bool[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.True((bool)Invoke(t, "AIBL", new bool[] { true }));
        }

        [Fact]
        public void ArrayIndex_ReadString()
        {
            var t = BuildStatic("AIST", typeof(string), m =>
            {
                var arr = m.DefineParameter(typeof(string[]), "arr");
                m.Append(Expression.ArrayIndex(arr, 0));
            });
            Assert.Equal("hello", Invoke(t, "AIST", (object)new string[] { "hello" }));
        }

        // Dynamic index (expression-based)
        [Fact]
        public void ArrayIndex_DynamicIndex()
        {
            var t = BuildStatic("AIDI", typeof(int), m =>
            {
                var arr = m.DefineParameter(typeof(int[]), "arr");
                var idx = m.DefineParameter(typeof(int), "idx");
                m.Append(Expression.ArrayIndex(arr, idx));
            });
            Assert.Equal(99, Invoke(t, "AIDI", new int[] { 10, 20, 99 }, 2));
        }

        // Array assign
        [Fact]
        public void ArrayIndex_WriteString()
        {
            var t = BuildStatic("AIWS", typeof(void), m =>
            {
                var arr = m.DefineParameter(typeof(string[]), "arr");
                m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant("replaced")));
            });
            var a = new string[] { "original" };
            Invoke(t, "AIWS", (object)a);
            Assert.Equal("replaced", a[0]);
        }

        [Fact]
        public void ArrayIndex_WriteLong()
        {
            var t = BuildStatic("AIWL", typeof(void), m =>
            {
                var arr = m.DefineParameter(typeof(long[]), "arr");
                m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant(999L)));
            });
            var a = new long[] { 0 };
            Invoke(t, "AIWL", (object)a);
            Assert.Equal(999L, a[0]);
        }

        [Fact]
        public void ArrayIndex_WriteFloat()
        {
            var t = BuildStatic("AIWF", typeof(void), m =>
            {
                var arr = m.DefineParameter(typeof(float[]), "arr");
                m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant(3.14f)));
            });
            var a = new float[] { 0 };
            Invoke(t, "AIWF", (object)a);
            Assert.Equal(3.14f, a[0]);
        }

        [Fact]
        public void ArrayIndex_WriteDouble()
        {
            var t = BuildStatic("AIWD", typeof(void), m =>
            {
                var arr = m.DefineParameter(typeof(double[]), "arr");
                m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant(3.14)));
            });
            var a = new double[] { 0 };
            Invoke(t, "AIWD", (object)a);
            Assert.Equal(3.14, a[0]);
        }

        [Fact]
        public void ArrayIndex_WriteShort()
        {
            var t = BuildStatic("AIWSH", typeof(void), m =>
            {
                var arr = m.DefineParameter(typeof(short[]), "arr");
                m.Append(Expression.Assign(Expression.ArrayIndex(arr, 0), Expression.Constant((short)42)));
            });
            var a = new short[] { 0 };
            Invoke(t, "AIWSH", (object)a);
            Assert.Equal((short)42, a[0]);
        }

        #endregion

        #region ClassEmitter — more constructor overloads

        [Fact]
        public void ClassEmitter_TwoParamConstructor()
        {
            var ce = _mod.DefineType($"C2_{Guid.NewGuid():N}", TypeAttributes.Public);
            var ct = ce.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            Assert.NotNull(ce.CreateType());
        }

        [Fact]
        public void ClassEmitter_ThreeParamConstructor()
        {
            var ce = _mod.DefineType($"C3_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(object));
            var ct = ce.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            Assert.NotNull(ce.CreateType());
        }

        #endregion

        #region NestedClassEmitter — more overloads

        [Fact]
        public void NestedClassEmitter_ThreeParam()
        {
            var te = _mod.DefineType($"P1_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            var ne = te.DefineNestedType($"Inner", TypeAttributes.NestedPublic, typeof(object));
            var nct = ne.DefineConstructor(MethodAttributes.Public);
            nct.Append(Expression.Default(typeof(void)));
            Assert.NotNull(ne.UncompiledType);
            te.CreateType();
        }

        [Fact]
        public void NestedClassEmitter_FourParam()
        {
            var te = _mod.DefineType($"P2_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            var ne = te.DefineNestedType($"Inner", TypeAttributes.NestedPublic, typeof(object), new[] { typeof(IDisposable) });
            var nct = ne.DefineConstructor(MethodAttributes.Public);
            nct.Append(Expression.Default(typeof(void)));
            var dispMethod = ne.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void));
            dispMethod.Append(Expression.Default(typeof(void)));
            te.CreateType();
        }

        #endregion

        #region FieldEmitter — static load/assign, DefaultValue null

        [Fact]
        public void FieldEmitter_StaticLoadAssign()
        {
            var te = _mod.DefineType($"FSL_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var sf = te.DefineField("Val", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            var set = te.DefineMethod("Set", MethodAttributes.Public | MethodAttributes.Static, typeof(void));
            var p = set.DefineParameter(typeof(int), ParameterAttributes.None, "v");
            set.Append(Expression.Assign(sf, p));
            var get = te.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            get.Append(sf);
            var type = te.CreateType();
            type.GetMethod("Set").Invoke(null, new object[] { 42 });
            Assert.Equal(42, type.GetMethod("Get").Invoke(null, null));
        }

        [Fact]
        public void FieldEmitter_DefaultValueSet()
        {
            var te = _mod.DefineType($"FDV_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class);
            var fe = te.DefineField("Val", typeof(int), FieldAttributes.Public);
            fe.DefaultValue = 42;
            Assert.Equal(42, fe.DefaultValue);
            // Clear the default value
            fe.DefaultValue = null;
            var ct = te.DefineConstructor(MethodAttributes.Public);
            ct.Append(Expression.Default(typeof(void)));
            te.CreateType();
        }

        #endregion

        #region BlockExpression — Throw detection result

        [Fact]
        public void Block_WithThrow_DetectsResult()
        {
            var t = BuildStatic("BWT", typeof(int), m =>
            {
                var flag = m.DefineParameter(typeof(bool), "flag");
                m.Append(Expression.IfThenElse(flag,
                    Expression.Return(Expression.Constant(1)),
                    Expression.Throw(typeof(InvalidOperationException), "fail")));
                m.Append(Expression.Constant(0));
            });
            Assert.Equal(1, Invoke(t, "BWT", true));
            Assert.Throws<TargetInvocationException>(() => Invoke(t, "BWT", false));
        }

        #endregion

        #region MethodEmitter — 更多DefineProperty/DefineMethod路径

        [Fact]
        public void ModuleEmitter_DefineType_AllOverloads()
        {
            var mod = new ModuleEmitter($"MDTO_{Guid.NewGuid():N}");
            var t1 = mod.DefineType($"T1_{Guid.NewGuid():N}");
            var ct1 = t1.DefineConstructor(MethodAttributes.Public);
            ct1.Append(Expression.Default(typeof(void)));
            t1.CreateType();

            var t3 = mod.DefineType($"T3_{Guid.NewGuid():N}", TypeAttributes.Public, typeof(object));
            var ct3 = t3.DefineConstructor(MethodAttributes.Public);
            ct3.Append(Expression.Default(typeof(void)));
            t3.CreateType();
        }

        #endregion

        #region TypeIsExpression — KnownTrue / KnownFalse / KnownAssignable

        [Fact]
        public void TypeIs_ExactSameType_Int()
        {
            // int is int — KnownTrue for value types
            var t = BuildStatic("TIST", typeof(bool), m =>
            {
                var v = m.DefineParameter(typeof(int), "v");
                m.Append(Expression.TypeIs(v, typeof(int)));
            });
            Assert.True((bool)Invoke(t, "TIST", 42));
        }

        [Fact]
        public void TypeIs_StringIsString()
        {
            // string is string — KnownAssignable (non-nullable reference)
            var t = BuildStatic("TISS", typeof(bool), m =>
            {
                var v = m.DefineParameter(typeof(string), "v");
                m.Append(Expression.TypeIs(v, typeof(string)));
            });
            Assert.True((bool)Invoke(t, "TISS", "hello"));
        }

        [Fact]
        public void TypeIs_BaseToChild()
        {
            // object is string — Unknown
            var t = BuildStatic("TIBC", typeof(bool), m =>
            {
                var v = m.DefineParameter(typeof(object), "v");
                m.Append(Expression.TypeIs(v, typeof(string)));
            });
            Assert.True((bool)Invoke(t, "TIBC", (object)"hello"));
            Assert.False((bool)Invoke(t, "TIBC", (object)42));
        }

        [Fact]
        public void TypeIs_ChildToBase()
        {
            // string is object — KnownAssignable
            var t = BuildStatic("TICB", typeof(bool), m =>
            {
                var v = m.DefineParameter(typeof(string), "v");
                m.Append(Expression.TypeIs(v, typeof(object)));
            });
            Assert.True((bool)Invoke(t, "TICB", "hello"));
        }

        #endregion

        #region ConvertExpression — same type (no-op) / void target

        [Fact]
        public void Convert_SameType_NoOp()
        {
            var t = BuildStatic("CST", typeof(int), m =>
            {
                var v = m.DefineParameter(typeof(int), "v");
                m.Append(Expression.Convert(v, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, "CST", 42));
        }

        #endregion

        #region Expression factory — misc uncovered paths

        [Fact]
        public void Variable_WithType()
        {
            var v = Expression.Variable(typeof(int));
            Assert.False(v.IsVoid);
        }

        [Fact]
        public void Assign_Variable()
        {
            var v = Expression.Variable(typeof(int));
            var assign = Expression.Assign(v, Expression.Constant(42));
            Assert.NotNull(assign);
        }

        #endregion
    }

    /// <summary>
    /// 辅助类。
    /// </summary>
    public class FieldTarget5
    {
        /// <summary>
        /// 值。
        /// </summary>
        public int Value;
    }
}
