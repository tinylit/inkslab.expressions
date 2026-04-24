using Inkslab.Emitters;
using System;
using System.Reflection;
using Xunit;

namespace Inkslab.Expressions.Tests
{
    /// <summary>
    /// Coverage boost: PropertyEmitter, InvocationExpression, SwitchExpression RuntimeType,
    /// ParameterEmitter, ReturnExpression, ModuleEmitter, ConstructorEmitter paths.
    /// </summary>
    public class CoverageBoostTests12
    {
        private readonly ModuleEmitter _mod = new ModuleEmitter($"CB12_{Guid.NewGuid():N}");

        private Type BuildStatic(string name, Type ret, Action<MethodEmitter> body)
        {
            var cls = _mod.DefineType($"C12_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            body(me);
            return cls.CreateType();
        }

        private Type BuildStaticP(string name, Type ret, Type p1, Action<MethodEmitter, ParameterExpression> body)
        {
            var cls = _mod.DefineType($"C12P_{name}", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, ret);
            var param = me.DefineParameter(p1, ParameterAttributes.None, "a");
            body(me, param);
            return cls.CreateType();
        }

        private object Invoke(Type t, params object[] args) => t.GetMethod("Run").Invoke(null, args);

        // ===== PropertyEmitter: define property with get/set on class, emit and test =====

        [Fact]
        public void PropertyEmitter_GetterSetter()
        {
            var cls = _mod.DefineType("PE1", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            var field = cls.DefineField("_val", typeof(int), FieldAttributes.Private);

            var prop = cls.DefineProperty("Val", PropertyAttributes.None, typeof(int));

            var getter = cls.DefineMethod("get_Val", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, typeof(int));
            getter.Append(field);

            var setter = cls.DefineMethod("set_Val", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, typeof(void));
            var setParam = setter.DefineParameter(typeof(int), ParameterAttributes.None, "value");
            setter.Append(Expression.Assign(field, setParam));

            prop.SetGetMethod(getter);
            prop.SetSetMethod(setter);

            var t = cls.CreateType();
            var inst = Activator.CreateInstance(t);
            t.GetProperty("Val").SetValue(inst, 42);
            Assert.Equal(42, t.GetProperty("Val").GetValue(inst));
        }

        [Fact]
        public void PropertyEmitter_DefaultValue()
        {
            var prop = new PropertyEmitter("Val", PropertyAttributes.None, typeof(int));
            prop.DefaultValue = 99;
            Assert.Equal(99, prop.DefaultValue);

            prop.DefaultValue = null;
            Assert.Null(prop.DefaultValue);

            prop.DefaultValue = 55;
            Assert.Equal(55, prop.DefaultValue);
        }

        [Fact]
        public void PropertyEmitter_CanReadCanWrite()
        {
            var prop = new PropertyEmitter("Test", PropertyAttributes.None, typeof(int));
            Assert.False(prop.CanRead);
            Assert.False(prop.CanWrite);
        }

        [Fact]
        public void PropertyEmitter_SetCustomAttribute()
        {
            var cls = _mod.DefineType("PE3", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            var prop = cls.DefineProperty("Val", PropertyAttributes.None, typeof(string));
            prop.SetCustomAttribute<ObsoleteAttribute>();

            var getter = cls.DefineMethod("get_Val", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.Virtual, typeof(string));
            getter.Append(Expression.Constant("hello"));
            prop.SetGetMethod(getter);

            var t = cls.CreateType();
            var propInfo = t.GetProperty("Val");
            Assert.NotNull(propInfo.GetCustomAttribute<ObsoleteAttribute>());
        }

        // ===== InvocationExpression: static method with args via MethodInfo.Invoke =====

        [Fact]
        public void InvocationExpression_StaticVoidMethod()
        {
            // Invoke a static void method: GC.Collect(0)
            var mi = typeof(GC).GetMethod("Collect", new[] { typeof(int) });
            var t = BuildStatic("InvVoid", typeof(void), me =>
            {
                var args = Expression.Array(typeof(object), Expression.Convert(Expression.Constant(0), typeof(object)));
                me.Append(Expression.Invoke(mi, args));
            });
            // Should not throw
            Invoke(t);
        }

        [Fact]
        public void InvocationExpression_StaticReturnValue()
        {
            // Math.Abs(-5) returns int, InvocationExpression(MethodInfo,..) uses methodInfo.ReturnType
            var mi = typeof(Math).GetMethod("Abs", new[] { typeof(int) });
            var t = BuildStatic("InvRet", typeof(int), me =>
            {
                var args = Expression.Array(typeof(object), Expression.Convert(Expression.Constant(-5), typeof(object)));
                me.Append(Expression.Invoke(mi, args));
            });
            Assert.Equal(5, Invoke(t));
        }

        [Fact]
        public void InvocationExpression_StaticReturnTyped()
        {
            var mi = typeof(Math).GetMethod("Abs", new[] { typeof(int) });
            var t = BuildStatic("InvRetT", typeof(int), me =>
            {
                var args = Expression.Array(typeof(object), Expression.Convert(Expression.Constant(-42), typeof(object)));
                me.Append(Expression.Invoke(mi, args));
            });
            Assert.Equal(42, Invoke(t));
        }

        // ===== SwitchExpression: RuntimeType matching (switch on object) =====

        [Fact]
        public void Switch_RuntimeType_MatchString()
        {
            var t = BuildStaticP("SwRT", typeof(int), typeof(object), (me, a) =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(-1)));

                var sw = Expression.Switch(a);

                var strVar = Expression.Variable(typeof(string));
                sw.Case(strVar).Append(Expression.Assign(result, Expression.Constant(1)));

                me.Append(sw);
                me.Append(result);
            });
            Assert.Equal(1, Invoke(t, "hello"));
            Assert.Equal(-1, Invoke(t, 42));
        }

        [Fact]
        public void Switch_RuntimeType_DefaultOnly()
        {
            var t = BuildStaticP("SwDef", typeof(int), typeof(object), (me, a) =>
            {
                var sw = Expression.Switch(a, Expression.Constant(99));
                me.Append(sw);
            });
            Assert.Equal(99, Invoke(t, "anything"));
        }

        // ===== SwitchExpression: string equality =====

        [Fact]
        public void Switch_StringEquality()
        {
            var t = BuildStaticP("SwStr", typeof(int), typeof(string), (me, a) =>
            {
                var result = Expression.Variable(typeof(int));
                me.Append(Expression.Assign(result, Expression.Constant(-1)));

                var sw = Expression.Switch(a);
                sw.Case(Expression.Constant("hello")).Append(Expression.Assign(result, Expression.Constant(1)));
                sw.Case(Expression.Constant("world")).Append(Expression.Assign(result, Expression.Constant(2)));

                me.Append(sw);
                me.Append(result);
            });
            Assert.Equal(1, Invoke(t, "hello"));
            Assert.Equal(2, Invoke(t, "world"));
            Assert.Equal(-1, Invoke(t, "other"));
        }

        // ===== ParameterEmitter: default value =====

        [Fact]
        public void ParameterEmitter_SetConstant()
        {
            var cls = _mod.DefineType("PaE1", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var param = me.DefineParameter(typeof(int), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "x");
            param.SetConstant(42);
            me.Append(param);
            var t = cls.CreateType();
            // Invoke with value
            Assert.Equal(10, t.GetMethod("Run").Invoke(null, new object[] { 10 }));
        }

        [Fact]
        public void ParameterEmitter_SetConstantValue()
        {
            var cls = _mod.DefineType("PaE2", TypeAttributes.Public);
            var me = cls.DefineMethod("Run", MethodAttributes.Public | MethodAttributes.Static, typeof(int));
            var param = me.DefineParameter(typeof(int), ParameterAttributes.Optional | ParameterAttributes.HasDefault, "x");
            param.SetConstant(10);
            me.Append(param);
            var t = cls.CreateType();
            var mi = t.GetMethod("Run");
            var p = mi.GetParameters()[0];
            Assert.True(p.HasDefaultValue);
        }

        // ===== ReturnExpression: explicit return =====

        [Fact]
        public void ReturnExpression_EarlyReturn()
        {
            var t = BuildStaticP("Ret", typeof(int), typeof(int), (me, a) =>
            {
                me.Append(Expression.IfThen(
                    Expression.LessThan(a, Expression.Constant(0)),
                    Expression.Return(Expression.Constant(-1))
                ));
                me.Append(a);
            });
            Assert.Equal(-1, Invoke(t, -5));
            Assert.Equal(10, Invoke(t, 10));
        }

        [Fact]
        public void ReturnExpression_VoidReturn()
        {
            // Test void return path
            var t = BuildStatic("VoidRet", typeof(void), me =>
            {
                me.Append(Expression.Return());
            });
            Invoke(t); // should not throw
        }

        // ===== ModuleEmitter: DefineType with all overloads =====

        [Fact]
        public void ModuleEmitter_DefineType_WithBaseType()
        {
            var cls = _mod.DefineType("MTB1", TypeAttributes.Public, typeof(Exception));
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var t = cls.CreateType();
            Assert.True(typeof(Exception).IsAssignableFrom(t));
        }

        [Fact]
        public void ModuleEmitter_DefineType_WithInterface()
        {
            var cls = _mod.DefineType("MTI1", TypeAttributes.Public, null, new[] { typeof(IDisposable) });
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var dispose = cls.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void));
            dispose.Append(Expression.Default(typeof(void)));
            var t = cls.CreateType();
            Assert.True(typeof(IDisposable).IsAssignableFrom(t));
        }

        [Fact]
        public void ModuleEmitter_DefineType_WithBaseAndInterface()
        {
            var cls = _mod.DefineType("MTBI1", TypeAttributes.Public, typeof(Exception), new[] { typeof(IDisposable) });
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();
            var dispose = cls.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, typeof(void));
            dispose.Append(Expression.Default(typeof(void)));
            var t = cls.CreateType();
            Assert.True(typeof(Exception).IsAssignableFrom(t));
            Assert.True(typeof(IDisposable).IsAssignableFrom(t));
        }

        // ===== ConstructorEmitter: invoke base with parameters =====

        [Fact]
        public void ConstructorEmitter_InvokeBaseWithParams()
        {
            var cls = _mod.DefineType("CB1", TypeAttributes.Public, typeof(Exception));
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            var p = ctor.DefineParameter(typeof(string), ParameterAttributes.None, "msg");
            ctor.InvokeBaseConstructor(typeof(Exception).GetConstructor(new[] { typeof(string) }), p);
            var t = cls.CreateType();
            var inst = (Exception)Activator.CreateInstance(t, "test error");
            Assert.Equal("test error", inst.Message);
        }

        // ===== AbstractTypeEmitter: properties =====

        [Fact]
        public void AbstractTypeEmitter_Name()
        {
            var cls = _mod.DefineType("ATE_Name", TypeAttributes.Public);
            Assert.Contains("ATE_Name", cls.Name);
        }

        [Fact]
        public void AbstractTypeEmitter_BaseType_Interface()
        {
            var cls = _mod.DefineType("ATE_IF", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
            Assert.Equal(typeof(object), cls.BaseType);
        }

        [Fact]
        public void AbstractTypeEmitter_GetInterfaces_Empty()
        {
            var cls = _mod.DefineType("ATE_NoIF", TypeAttributes.Public);
            Assert.Empty(cls.GetInterfaces());
        }

        [Fact]
        public void AbstractTypeEmitter_IsGenericType()
        {
            var cls = _mod.DefineType("ATE_NoGen", TypeAttributes.Public);
            Assert.False(cls.IsGenericType);
        }

        // ===== AbstractTypeEmitter: DefineField overloads =====

        [Fact]
        public void AbstractTypeEmitter_DefineField_FieldInfo()
        {
            var cls = _mod.DefineType("ATE_FI", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            // Copy a field from an existing type
            var srcField = typeof(Version).GetField("_Major", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? typeof(Version).GetField("_Build", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Just use direct DefineField overloads
            var f1 = cls.DefineField("f1", typeof(int));
            var f2 = cls.DefineField("f2", typeof(string), false); // not serializable
            var f3 = cls.DefineField("f3", typeof(double), FieldAttributes.Public);

            var run = cls.DefineMethod("Run", MethodAttributes.Public, typeof(int));
            run.Append(f1);

            var t = cls.CreateType();
            Assert.NotNull(t.GetField("f3"));
        }

        // ===== AbstractTypeEmitter: DefineGenericParameters =====

        [Fact]
        public void AbstractTypeEmitter_DefineGenericParameters_Empty()
        {
            var cls = _mod.DefineType("ATE_GP0", TypeAttributes.Public);
            var result = cls.DefineGenericParameters(Type.EmptyTypes);
            Assert.Empty(result);
        }

        [Fact]
        public void AbstractTypeEmitter_DefineGenericParameters_AlreadyDefined()
        {
            var cls = _mod.DefineType("ATE_GP1", TypeAttributes.Public);
            cls.DefineGenericParameters(Type.EmptyTypes);
            Assert.Throws<InvalidOperationException>(() => cls.DefineGenericParameters(Type.EmptyTypes));
        }

        [Fact]
        public void AbstractTypeEmitter_DefineGenericParameters_NonGenericType()
        {
            var cls = _mod.DefineType("ATE_GP2", TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => cls.DefineGenericParameters(new[] { typeof(int) }));
        }

        // ===== AbstractTypeEmitter: TypeInitializer =====

        [Fact]
        public void AbstractTypeEmitter_TypeInitializer()
        {
            var cls = _mod.DefineType("ATE_TI", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            // TypeInitializer is a public property
            Assert.NotNull(cls.TypeInitializer);
            Assert.True(cls.TypeInitializer.IsEmpty);

            var t = cls.CreateType();
            Assert.NotNull(t);
        }

        // ===== NestedClassEmitter: define nested type =====

        [Fact]
        public void NestedClassEmitter_Basic()
        {
            var cls = _mod.DefineType("NC_Outer", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            var nested = cls.DefineNestedType("Inner");
            var nctor = nested.DefineConstructor(MethodAttributes.Public);
            nctor.InvokeBaseConstructor();

            var t = cls.CreateType();
            var nestedTypes = t.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
            Assert.NotEmpty(nestedTypes);
        }

        // ===== EmitUtils: ConvertToType with nullable conversions =====

        [Fact]
        public void Convert_NullableToNullable()
        {
            var t = BuildStaticP("N2N", typeof(long?), typeof(int?), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(long?)));
            });
            Assert.Equal(42L, ((long?)Invoke(t, (int?)42)).Value);
            Assert.Null(Invoke(t, (int?)null));
        }

        [Fact]
        public void Convert_NonNullableToNullable()
        {
            var t = BuildStaticP("V2N", typeof(int?), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(int?)));
            });
            Assert.Equal(42, ((int?)Invoke(t, 42)).Value);
        }

        [Fact]
        public void Convert_NullableToNonNullable()
        {
            var t = BuildStaticP("N2V", typeof(int), typeof(int?), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, (int?)42));
        }

        [Fact]
        public void Convert_NullableToReference()
        {
            var t = BuildStaticP("N2R", typeof(object), typeof(int?), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(object)));
            });
            Assert.Equal(42, Invoke(t, (int?)42));
            Assert.Null(Invoke(t, (int?)null));
        }

        // ===== EmitUtils: casting paths =====

        [Fact]
        public void Convert_BoxValueType()
        {
            var t = BuildStaticP("Box", typeof(object), typeof(int), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(object)));
            });
            Assert.Equal(42, Invoke(t, 42));
        }

        [Fact]
        public void Convert_UnboxValueType()
        {
            var t = BuildStaticP("Unbox", typeof(int), typeof(object), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(int)));
            });
            Assert.Equal(42, Invoke(t, (object)42));
        }

        [Fact]
        public void Convert_Castclass()
        {
            var t = BuildStaticP("Cast", typeof(string), typeof(object), (me, a) =>
            {
                me.Append(Expression.Convert(a, typeof(string)));
            });
            Assert.Equal("hello", Invoke(t, (object)"hello"));
        }

        // ===== EmitUtils: constant types (DateTime, TimeSpan, Version, Uri, Guid) =====

        [Fact]
        public void Constant_DateTime()
        {
            var dt = new DateTime(2024, 1, 15);
            var t = BuildStatic("CDT", typeof(DateTime), me =>
            {
                me.Append(Expression.Constant(dt));
            });
            Assert.Equal(dt, Invoke(t));
        }

        [Fact]
        public void Constant_TimeSpan()
        {
            var ts = TimeSpan.FromHours(1.5);
            var t = BuildStatic("CTS", typeof(TimeSpan), me =>
            {
                me.Append(Expression.Constant(ts));
            });
            Assert.Equal(ts, Invoke(t));
        }

        [Fact]
        public void Constant_Version()
        {
            var v = new Version(1, 2);
            var t = BuildStatic("CV2", typeof(Version), me =>
            {
                me.Append(Expression.Constant(v));
            });
            Assert.Equal(v, Invoke(t));
        }

        [Fact]
        public void Constant_Version3()
        {
            var v = new Version(1, 2, 3);
            var t = BuildStatic("CV3", typeof(Version), me =>
            {
                me.Append(Expression.Constant(v));
            });
            Assert.Equal(v, Invoke(t));
        }

        [Fact]
        public void Constant_Version4()
        {
            var v = new Version(1, 2, 3, 4);
            var t = BuildStatic("CV4", typeof(Version), me =>
            {
                me.Append(Expression.Constant(v));
            });
            Assert.Equal(v, Invoke(t));
        }

        [Fact]
        public void Constant_Decimal_Large()
        {
            var d = 12345678901234.56789m;
            var t = BuildStatic("CDL", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(d));
            });
            Assert.Equal(d, Invoke(t));
        }

        [Fact]
        public void Constant_Decimal_Long()
        {
            var d = 999999999999m;
            var t = BuildStatic("CDLg", typeof(decimal), me =>
            {
                me.Append(Expression.Constant(d));
            });
            Assert.Equal(d, Invoke(t));
        }

        // ===== FieldEmitter: used as expression =====

        [Fact]
        public void FieldEmitter_ReadWriteInMethod()
        {
            var cls = _mod.DefineType("FE_RW", TypeAttributes.Public);
            var ctor = cls.DefineConstructor(MethodAttributes.Public);
            ctor.InvokeBaseConstructor();

            var field = cls.DefineField("_count", typeof(int));

            var inc = cls.DefineMethod("Inc", MethodAttributes.Public, typeof(int));
            inc.Append(Expression.Assign(field, Expression.Add(field, Expression.Constant(1))));
            inc.Append(field);

            var t = cls.CreateType();
            var inst = Activator.CreateInstance(t);
            Assert.Equal(1, t.GetMethod("Inc").Invoke(inst, null));
            Assert.Equal(2, t.GetMethod("Inc").Invoke(inst, null));
        }

        // ===== MemberInitExpression: new with field bindings =====

        [Fact]
        public void MemberInit_FieldBinding()
        {
            // Use existing class with public field
            var t = BuildStatic("MIF", typeof(object), me =>
            {
                var type = typeof(TestFieldClass);
                var ctorInfo = type.GetConstructor(Type.EmptyTypes);
                var fieldInfo = type.GetField("Value");
                var binding = Expression.Bind(fieldInfo, Expression.Constant(42));
                var memberInit = Expression.MemberInit(Expression.New(ctorInfo), binding);
                me.Append(Expression.Convert(memberInit, typeof(object)));
            });
            var result = (TestFieldClass)Invoke(t);
            Assert.Equal(42, result.Value);
        }

        public class TestFieldClass
        {
            public int Value;
        }

        // ===== VariableExpression =====

        [Fact]
        public void Variable_MultipleTypes()
        {
            var t = BuildStatic("VarM", typeof(string), me =>
            {
                var vi = Expression.Variable(typeof(int));
                var vs = Expression.Variable(typeof(string));
                var vd = Expression.Variable(typeof(double));
                me.Append(Expression.Assign(vi, Expression.Constant(42)));
                me.Append(Expression.Assign(vd, Expression.Constant(3.14)));
                me.Append(Expression.Assign(vs, Expression.Constant("ok")));
                me.Append(vs);
            });
            Assert.Equal("ok", Invoke(t));
        }

        // ===== Expression.Default for various types =====

        [Fact]
        public void Default_String()
        {
            var t = BuildStatic("DefS", typeof(string), me =>
            {
                me.Append(Expression.Default(typeof(string)));
            });
            Assert.Null(Invoke(t));
        }

        [Fact]
        public void Default_Long()
        {
            var t = BuildStatic("DefL", typeof(long), me =>
            {
                me.Append(Expression.Default(typeof(long)));
            });
            Assert.Equal(0L, Invoke(t));
        }

        [Fact]
        public void Default_Double()
        {
            var t = BuildStatic("DefD", typeof(double), me =>
            {
                me.Append(Expression.Default(typeof(double)));
            });
            Assert.Equal(0.0, Invoke(t));
        }

        [Fact]
        public void Default_Float()
        {
            var t = BuildStatic("DefF", typeof(float), me =>
            {
                me.Append(Expression.Default(typeof(float)));
            });
            Assert.Equal(0.0f, Invoke(t));
        }

        [Fact]
        public void Default_Guid()
        {
            var t = BuildStatic("DefG", typeof(Guid), me =>
            {
                me.Append(Expression.Default(typeof(Guid)));
            });
            Assert.Equal(Guid.Empty, Invoke(t));
        }

        [Fact]
        public void Default_Nullable()
        {
            var t = BuildStatic("DefNI", typeof(int?), me =>
            {
                me.Append(Expression.Default(typeof(int?)));
            });
            Assert.Null(Invoke(t));
        }

        // ===== MethodCallExpression: void static method =====

        [Fact]
        public void MethodCallExpression_VoidStatic()
        {
            var mi = typeof(GC).GetMethod("Collect", Type.EmptyTypes);
            var t = BuildStatic("MCSVoid", typeof(void), me =>
            {
                me.Append(Expression.Call(mi));
            });
            Invoke(t); // should not throw
        }

        [Fact]
        public void MethodCallExpression_StaticWithArgs()
        {
            var mi = typeof(Math).GetMethod("Max", new[] { typeof(int), typeof(int) });
            var t = BuildStatic("MCSArgs", typeof(int), me =>
            {
                me.Append(Expression.Call(mi, Expression.Constant(3), Expression.Constant(7)));
            });
            Assert.Equal(7, Invoke(t));
        }

        [Fact]
        public void MethodCallExpression_InstanceMethod()
        {
            var mi = typeof(string).GetMethod("ToUpper", Type.EmptyTypes);
            var t = BuildStaticP("MCI", typeof(string), typeof(string), (me, a) =>
            {
                me.Append(Expression.Call(a, mi));
            });
            Assert.Equal("HELLO", Invoke(t, "hello"));
        }

        // ===== Constant types coverage =====

        [Fact]
        public void Constant_SByte()
        {
            var t = BuildStatic("CSB", typeof(sbyte), me =>
            {
                me.Append(Expression.Constant((sbyte)-5));
            });
            Assert.Equal((sbyte)-5, Invoke(t));
        }

        [Fact]
        public void Constant_UShort()
        {
            var t = BuildStatic("CUS", typeof(ushort), me =>
            {
                me.Append(Expression.Constant((ushort)1234));
            });
            Assert.Equal((ushort)1234, Invoke(t));
        }

        [Fact]
        public void Constant_UInt()
        {
            var t = BuildStatic("CUI", typeof(uint), me =>
            {
                me.Append(Expression.Constant(42u));
            });
            Assert.Equal(42u, Invoke(t));
        }

        [Fact]
        public void Constant_ULong()
        {
            var t = BuildStatic("CUL", typeof(ulong), me =>
            {
                me.Append(Expression.Constant(42UL));
            });
            Assert.Equal(42UL, Invoke(t));
        }

        [Fact]
        public void Constant_Char()
        {
            var t = BuildStatic("CC", typeof(char), me =>
            {
                me.Append(Expression.Constant('A'));
            });
            Assert.Equal('A', Invoke(t));
        }

        [Fact]
        public void Constant_Byte()
        {
            var t = BuildStatic("CBY", typeof(byte), me =>
            {
                me.Append(Expression.Constant((byte)255));
            });
            Assert.Equal((byte)255, Invoke(t));
        }

        [Fact]
        public void Constant_Short()
        {
            var t = BuildStatic("CSH", typeof(short), me =>
            {
                me.Append(Expression.Constant((short)-100));
            });
            Assert.Equal((short)-100, Invoke(t));
        }
    }
}
